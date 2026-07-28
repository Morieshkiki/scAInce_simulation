using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using tumvt.sumounity;

// Crash behaviour for a SUMO-streamed vehicle.
//
// While uncrashed the vehicle is driven kinematically by SumoTeleportController. When the
// PLAYER car hits it hard enough, this controller:
//   1. detaches the vehicle from SUMO control (removes it from the registry and disables
//      the mover) so SUMO no longer repositions it,
//   2. hands it to the physics engine (non-kinematic) seeded with the momentum it had at
//      impact, so it gets shoved/spun realistically, plus an extra impulse for a clear hit,
//   3. dents the body at the contact points,
//   4. once it settles, classifies the outcome:
//        - rolled over (not on its wheels): permanent wreck in place;
//        - on its wheels: the driver sits "collecting themselves" for a few seconds
//          (longer after a hard hit), then DRIVES a gentle curve to the kerb at walking
//          pace — steering out, straightening, wheels rolling — and parks, clearing the lane.
//
// Every crashed vehicle also publishes its phase in CrashPhases ("blocking" while it
// obstructs the lane, "cleared" once parked at the kerb, "wrecked" if permanent).
// SumoSocketClient streams that dictionary back to the Python SUMO host each tick, which
// freezes/removes the SUMO twin so following traffic queues behind the accident in SUMO
// (and on the 2D web map) exactly while the lane is blocked in Unity.
//
// Lives in Assembly-CSharp (no asmdef in this project) so it can freely reference both the
// Sumonity types and the global-namespace PlayerCarController / MeshDenter.
[RequireComponent(typeof(Rigidbody))]
public class VehicleCrashController : MonoBehaviour
{
    [Header("Crash thresholds (m/s relative speed)")]
    [Tooltip("Minimum relative speed of a player impact to count as a crash.")]
    public float crashSpeedThreshold = 3f;
    [Tooltip("Above this impact severity the driver is dazed longer before pulling over.")]
    public float heavyThreshold = 8f;

    [Header("Aftermath")]
    [Tooltip("Metres to move to the right (kerb side) when pulling over.")]
    public float pulloverOffset = 3.5f;
    [Tooltip("Speed (m/s) below which the wreck is considered to have settled.")]
    public float settleSpeed = 0.6f;
    [Tooltip("Max seconds to wait for the wreck to settle before classifying.")]
    public float maxSettleWait = 6f;
    [Tooltip("Driving speed (m/s) of the pull-over. ~2 m/s = a careful limp to the kerb.")]
    public float pulloverSpeed = 2.0f;
    [Tooltip("Seconds (min..max) the driver waits after a LIGHT hit before pulling over.")]
    public Vector2 reactionDelayLight = new Vector2(2.5f, 4.5f);
    [Tooltip("Seconds (min..max) of dazed delay after a HEAVY hit before pulling over.")]
    public Vector2 reactionDelayHeavy = new Vector2(5f, 8f);
    [Tooltip("Extra shove along the impact (fraction of relative speed) for a visible hit.")]
    public float extraImpulseScale = 0.6f;

    public bool IsCrashed { get; private set; }

    // Ids of vehicles that have crashed and been detached from SUMO control. SUMO keeps
    // streaming these ids every step (it does not know about the Unity-side crash), so the
    // spawner (SumoSocketClientHelper.CheckForNewVehiclesAndAdd) MUST skip them — otherwise
    // it re-creates a fresh copy of the wreck every step, which the player keeps hitting,
    // producing the "one vehicle multiplies into several" bug. Pruned in RemoveNonExistentActors
    // once SUMO stops reporting the id (so the set never grows unbounded / handles id reuse).
    public static readonly HashSet<string> DetachedIds = new HashSet<string>();

    // vehId -> "blocking" | "cleared" | "wrecked". Read by SumoSocketClient every frame and
    // sent to the Python host, which mirrors the state into SUMO via TraCI (stop the twin
    // while blocking, delete it once cleared/towed). Written and read on the main thread
    // only. Pruned in RemoveNonExistentActors together with DetachedIds.
    public static readonly Dictionary<string, string> CrashPhases = new Dictionary<string, string>();

    // vehId -> the live wreck GameObject. This is the DURABLE anti-respawn guard, and it
    // exists because DetachedIds alone is not enough: once a wreck PULLS OVER we tell the
    // Python host to remove its SUMO twin (so traffic flows again), which makes the id
    // leave the stream while the wreck GameObject lives on. RemoveNonExistentActors then
    // prunes the id out of DetachedIds — and if SUMO ever streams that id again the spawner
    // would create a driven DUPLICATE next to the parked wreck. Keying the guard to the
    // wreck object instead of the stream closes that window: an id cannot respawn while its
    // wreck is still in the scene. Self-cleaning via OnDestroy.
    public static readonly Dictionary<string, GameObject> ActiveWrecks = new Dictionary<string, GameObject>();

    private Rigidbody rb;
    private SumoTeleportController mover;
    private MeshDenter denter;
    private Dictionary<string, GameObject> vehDict; // SUMO registry, to unregister from
    private string vehId;
    private string vehType = "passenger";

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        mover = GetComponent<SumoTeleportController>();
        denter = GetComponent<MeshDenter>();
    }

    // Called by SumoSocketClientHelper right after the vehicle is spawned & registered.
    public void Configure(string id, float mass, Dictionary<string, GameObject> registry,
                          string vehicleType = "passenger")
    {
        vehId = id;
        vehDict = registry;
        vehType = vehicleType;
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb != null) rb.mass = mass;
    }

    void OnCollisionEnter(Collision c)
    {
        float relSpeed = c.relativeVelocity.magnitude;
        bool fromPlayer = c.collider.GetComponentInParent<PlayerCarController>() != null;

        if (!IsCrashed && fromPlayer && relSpeed >= crashSpeedThreshold)
            TriggerCrash(c, relSpeed);
        else if (IsCrashed)
            ApplyDents(c, relSpeed); // already a wreck: accumulate dents from further knocks
    }

    // Public so a test harness (MCP execute_code) can force a crash without a real impact.
    public void TriggerCrash(Collision c, float severity)
    {
        if (!IsCrashed)
        {
            IsCrashed = true;

            Vector3 vel = (mover != null) ? mover.CurrentVelocity : rb.linearVelocity;

            // 1. Detach from SUMO control. Remove from the live registry AND record the id as
            //    detached so the spawner will not respawn a fresh copy while SUMO keeps
            //    streaming this id (that respawn loop is what multiplied the vehicle).
            if (vehId != null) DetachedIds.Add(vehId);
            if (vehId != null) ActiveWrecks[vehId] = gameObject; // durable anti-respawn guard
            if (vehDict != null && vehId != null) vehDict.Remove(vehId);
            if (mover != null) mover.enabled = false;
            SetPhase("blocking");

            // 2. Hand to physics with the momentum it had at impact.
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.centerOfMass += Vector3.down * 0.4f; // keep it from flipping too eagerly
            rb.linearVelocity = vel;

            // Extra shove + a bit of spin along the impact so the hit clearly registers.
            // PhysX already transfers momentum mass-correctly when there is a real contact;
            // this artificial nudge is scaled DOWN for heavy vehicles (referenced to a
            // 1200 kg car) so a 40-ton tram barely budges while a car gets thrown.
            float massFactor = Mathf.Clamp01(1200f / Mathf.Max(rb.mass, 1f));
            Vector3 n = (c != null && c.contactCount > 0)
                ? c.GetContact(0).normal
                : (c != null ? (transform.position - c.collider.transform.position).normalized : transform.forward);
            rb.AddForce(-n * severity * extraImpulseScale * massFactor, ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * severity * 0.25f * massFactor, ForceMode.VelocityChange);

            StartCoroutine(SettleAndDecide(severity));
        }

        ApplyDents(c, severity);
    }

    private void SetPhase(string phase)
    {
        if (vehId != null) CrashPhases[vehId] = phase;
    }

    void OnDestroy()
    {
        // Release the anti-respawn guard only when THIS wreck is the one registered
        // (guard against a later same-id instance being unregistered by an old one).
        if (vehId != null && ActiveWrecks.TryGetValue(vehId, out var g) && g == gameObject)
            ActiveWrecks.Remove(vehId);
    }

    private void ApplyDents(Collision c, float severity)
    {
        if (denter == null || c == null) return;
        int n = c.contactCount;
        if (denter.TargetCount == 0)
            Debug.LogWarning($"[dent] {vehId}: MeshDenter has 0 dentable meshes " +
                             "(body meshes not Read/Write enabled?) — no dent will show.");
        if (n > 0)
        {
            for (int i = 0; i < n; i++)
            {
                ContactPoint cp = c.GetContact(i);
                denter.DentAt(cp.point, cp.normal, severity);
            }
            return;
        }

        // Zero contact points. This is common, not exceptional: the wreck is a
        // KINEMATIC body using ContinuousSpeculative detection, and a speculative
        // hit fires OnCollisionEnter from a PREDICTED contact that has not
        // penetrated yet, so PhysX populates no ContactPoints. Without a fallback
        // the crash leaves NO dent at all (the reported "accidents don't dent"
        // bug) even though the pull-over still runs. Derive a believable impact
        // point ourselves: the spot on this body's collider closest to the other
        // collider, dented inward along the line between them.
        Collider mine = GetComponent<BoxCollider>();
        if (mine == null) mine = GetComponent<Collider>();
        Collider other = c.collider;
        Vector3 otherPos = (other != null) ? other.bounds.center : transform.position;
        Vector3 hit = (mine != null) ? mine.ClosestPoint(otherPos) : transform.position;
        Vector3 normal = hit - otherPos; normal.y = 0f;
        if (normal.sqrMagnitude < 1e-6f) { normal = transform.position - otherPos; normal.y = 0f; }
        if (normal.sqrMagnitude < 1e-6f) normal = -transform.forward;
        denter.DentAt(hit, normal.normalized, severity);
    }

    private IEnumerator SettleAndDecide(float severity)
    {
        float t = 0f;
        while (t < maxSettleWait && rb.linearVelocity.magnitude > settleSpeed)
        {
            t += Time.deltaTime;
            yield return null;
        }

        bool upright = Vector3.Dot(transform.up, Vector3.up) > 0.7f;

        // Rolled over (not on its wheels): permanent wreck in place. The Python host
        // stops its SUMO twin so traffic queues, then "tows" it (removes the twin)
        // after a timeout so the network cannot stay gridlocked forever.
        if (!upright)
        {
            SetPhase("wrecked");
            yield break;
        }

        // On its wheels: the driver needs a moment before clearing the lane — longer when
        // the hit was hard. During this pause the lane stays blocked in SUMO too.
        bool heavy = severity > heavyThreshold;
        Vector2 delay = heavy ? reactionDelayHeavy : reactionDelayLight;
        yield return new WaitForSeconds(Random.Range(delay.x, delay.y));

        yield return StartCoroutine(PullOver());
        SetPhase("cleared");
    }

    // Drive to the kerb like a real driver: follow a gentle S-curve forward-and-right at
    // walking pace, yawing along the curve tangent (steer out, then straighten), wheels
    // rolling, hugging the road surface. Kinematic so nothing can knock it off course.
    private IEnumerator PullOver()
    {
        Vector3 fwd = transform.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward; else fwd.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, fwd); // vehicle right (right-hand traffic kerb)

        rb.isKinematic = true; // take physics back off so the manoeuvre is clean
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        // Cubic Bezier: start pose -> a point ahead-and-right, tangents along the heading so
        // the car eases out of the lane and eases back parallel to it (no sideways slide).
        Vector3 p0 = transform.position;
        float ahead = Mathf.Max(pulloverOffset * 2.5f, 6f);
        Vector3 p3 = p0 + fwd * ahead + right * pulloverOffset;
        Vector3 p1 = p0 + fwd * (ahead * 0.4f);
        Vector3 p2 = p3 - fwd * (ahead * 0.35f);

        // Approximate the curve length so duration comes from the driving speed.
        float len = 0f;
        Vector3 prev = p0;
        for (int i = 1; i <= 16; i++)
        {
            Vector3 pt = Bezier(p0, p1, p2, p3, i / 16f);
            len += Vector3.Distance(prev, pt);
            prev = pt;
        }
        float dur = Mathf.Max(len / Mathf.Max(pulloverSpeed, 0.1f), 0.5f);

        Transform[] wheels = (mover != null) ? mover.wheels : null;
        float wheelRadius = (mover != null) ? mover.wheelRadius : 0.35f;
        Vector3 lastWheelPos = p0;

        float e = 0f;
        while (e < dur)
        {
            e += Time.deltaTime;
            // Ease in/out: rolls off gently, brakes to a stop at the kerb.
            float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / dur));

            Vector3 pos = Bezier(p0, p1, p2, p3, u);
            pos.y = GroundHeight(pos, p0.y);
            transform.position = pos;

            Vector3 tan = BezierTangent(p0, p1, p2, p3, Mathf.Clamp(u, 0.02f, 0.98f));
            tan.y = 0f;
            if (tan.sqrMagnitude > 1e-6f)
            {
                Quaternion look = Quaternion.LookRotation(tan.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, 90f * Time.deltaTime);
            }

            RollWheels(wheels, wheelRadius, pos, ref lastWheelPos);
            yield return null;
        }
        // Parked permanently at the kerb (dented).
    }

    private static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
    {
        float s = 1f - t;
        return s * s * s * a + 3f * s * s * t * b + 3f * s * t * t * c + t * t * t * d;
    }

    private static Vector3 BezierTangent(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
    {
        float s = 1f - t;
        return 3f * s * s * (b - a) + 6f * s * t * (c - b) + 3f * t * t * (d - c);
    }

    // Snap to the road surface under `pos` (ignoring our own colliders) so the car neither
    // floats on its crash-pose height nor sinks on sloped roads. Falls back to the start
    // height, and rejects hits far above/below it (bridges, tunnels).
    private float GroundHeight(Vector3 pos, float fallback)
    {
        RaycastHit[] hits = Physics.RaycastAll(pos + Vector3.up * 3f, Vector3.down, 10f,
                                               ~0, QueryTriggerInteraction.Ignore);
        float best = float.NegativeInfinity;
        foreach (RaycastHit h in hits)
        {
            if (h.collider.transform.IsChildOf(transform)) continue;
            if (Mathf.Abs(h.point.y - fallback) > 2f) continue;
            if (h.point.y > best) best = h.point.y;
        }
        if (float.IsNegativeInfinity(best)) return fallback;
        float clearance = (mover != null) ? mover.heightOffset : 0f;
        return best + clearance;
    }

    private void RollWheels(Transform[] wheels, float radius, Vector3 cur, ref Vector3 last)
    {
        if (wheels == null || wheels.Length == 0) { last = cur; return; }
        float forwardDelta = Vector3.Dot(cur - last, transform.forward);
        last = cur;
        float rotationAmount = (forwardDelta / Mathf.Max(radius, 0.01f)) * Mathf.Rad2Deg;
        for (int i = 0; i < wheels.Length; i++)
            if (wheels[i] != null)
                wheels[i].Rotate(Vector3.left, -rotationAmount, Space.Self);
    }
}
