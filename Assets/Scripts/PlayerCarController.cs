using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// User-drivable car for free-roam in Play mode, independent of the SUMO traffic.
//
// DRIVING MODEL
//  * Motion uses a kinematic BICYCLE model pivoting about the rear axle (front and
//    rear axle points are advanced separately, then the body is rebuilt from them).
//    This is the textbook car model — see Rajamani, "Vehicle Dynamics and Control"
//    (Springer, 2012), ch. 2 — and is what makes a held-W + A/D turn sweep a natural
//    arc instead of spinning about the car's centre.
//  * A 5-speed MANUAL gearbox: E shifts up, Q shifts down. Each gear has its own top
//    speed and pulling power (both tunable in the Inspector). Higher gears pull more
//    weakly, and a per-gear "engage speed" makes the high gears bog (no pull) below a
//    threshold — so 3rd/4th/5th cannot launch a stopped car, but DO keep pulling
//    (more slowly) once the car is already rolling, exactly like a real clutch+engine.
//    This bog/engine model is a custom simplified approximation (not copied from a
//    third-party project), tuned to feel right rather than to be physically exact.
//  * Steering authority falls off with speed, so sharp turns are impossible at high
//    speed.
//  * Stays physics-driven (Rigidbody velocity) so it COLLIDES with SUMO traffic, and
//    stays upright via a yaw-only rotation (independent of the freeze checkboxes).
//
// VISUALS: all four wheels roll with speed; the two front wheels steer (A/D).
//
// Controls: W/S = accelerate / brake-reverse, A/D = steer, E/Q = gear up/down,
// Space = handbrake.
[RequireComponent(typeof(Rigidbody))]
public class PlayerCarController : MonoBehaviour
{
    [Header("Gearbox (index 0 = 1st gear ... index 4 = 5th gear)")]
    [Tooltip("Top speed of each gear in km/h. 5th gear is the car's top speed.")]
    public float[] gearMaxSpeedKmh = { 30f, 60f, 100f, 150f, 200f };
    [Tooltip("Pulling power (peak acceleration, m/s^2) of each gear. Lower for higher gears.")]
    public float[] gearAccel = { 5.0f, 3.6f, 2.6f, 1.9f, 1.3f };
    [Tooltip("Speed (km/h) below which a gear bogs and barely pulls. Keep 1st (and 2nd) low so they can launch; raise 3rd/4th/5th so they can't launch a stopped car.")]
    public float[] gearEngageSpeedKmh = { 0f, 0f, 30f, 55f, 85f };
    [Tooltip("Current gear (1..5). Visible/editable for debugging.")]
    public int currentGear = 1;
    [Tooltip("Brief power cut while shifting (s), like the clutch going in.")]
    public float shiftCutTime = 0.2f;

    [Header("Reverse / braking (m/s & m/s^2)")]
    public float maxReverseSpeed = 6f;
    public float reverseAccel = 3f;
    public float brakeDeceleration = 20f;
    public float coastDeceleration = 4f;
    public float handbrakeDeceleration = 30f;

    [Header("Steering")]
    [Tooltip("Maximum steering angle (deg) at low speed.")]
    public float maxSteerAngle = 32f;
    [Tooltip("How fast the steering angle moves toward the target (deg/s).")]
    public float steerSpeed = 110f;
    [Tooltip("Max lateral (cornering) acceleration, m/s^2 (~grip). This is what stops sharp turns at speed: the faster you go, the smaller the usable steering. ~8 = 0.8g. Lower = even gentler high-speed turns.")]
    public float maxLateralAccel = 8f;
    [Tooltip("Wheelbase (m): distance between front and rear axle. Bigger = wider turns.")]
    public float wheelBase = 2.6f;

    [Header("Body")]
    public float centerOfMassDrop = 0.6f;

    [Header("Wheel visuals")]
    [Tooltip("Wheel radius (m). Smaller = the rolling spin looks faster for the same speed.")]
    public float wheelRadius = 0.35f;
    [Tooltip("Max visual angle (deg) the front wheels turn at LOW speed.")]
    public float visualSteerMax = 45f;
    [Tooltip("Speed (km/h) at which the front-wheel visual turn is reduced to its minimum fraction.")]
    public float visualSteerSpeedRefKmh = 80f;
    [Tooltip("Fraction of the visual turn still allowed at/above the reference speed (0..1).")]
    [Range(0f, 1f)] public float visualSteerMinFraction = 0.15f;
    [Tooltip("Flip to -1 if the front wheels turn the wrong way.")]
    public float steerVisualSign = 1f;
    public Transform[] spinWheels;
    public Transform steerJointFL;
    public Transform steerJointFR;

    [Header("HUD")]
    [Tooltip("Show a small on-screen gear + speed readout while driving.")]
    public bool showHud = true;

    [Header("Crash")]
    [Tooltip("Mass (kg) of the player car. Drives momentum transfer in crashes.")]
    public float bodyMass = 1300f;
    [Tooltip("Minimum relative speed (m/s) of a vehicle impact to trigger a crash.")]
    public float crashSpeedThreshold = 3f;
    [Tooltip("Seconds after a crash during which physics owns the car (it spins/skids) " +
             "before the driving model takes back control.")]
    public float recoverTime = 1.2f;
    [Tooltip("Minimum speed (m/s) hitting a STATIC obstacle (tree, wall, pole) that counts " +
             "as a crash (dents the body and briefly hands the car to physics).")]
    public float staticCrashSpeedThreshold = 2.5f;
    [Tooltip("Physics-owned recovery time (s) after hitting a static obstacle. Shorter than " +
             "a vehicle crash: the car mostly just stops dead against the obstacle.")]
    public float staticRecoverTime = 0.6f;

    // Raycast spring suspension (the technique GTA-class driving games use): the body
    // collider never touches the ground; four rays at the wheel positions hold the car
    // up with spring+damper forces. Steps like curbs compress the front springs first,
    // pitching the body up smoothly instead of slamming the collider into a wall, and
    // small pavement seams are swallowed entirely by the springs.
    [Header("Suspension (raycast springs at the wheels)")]
    [Tooltip("Distance (m) from the wheel axle to the ground the springs try to hold. " +
             "Roughly wheel radius + a little static sag headroom.")]
    public float suspensionRest = 0.50f;
    [Tooltip("Extra travel (m) beyond rest before a wheel fully unloads (droop).")]
    public float suspensionTravel = 0.30f;
    [Tooltip("Spring rate per wheel (N/m).")]
    public float springStrength = 42000f;
    [Tooltip("Damper rate per wheel (N·s/m).")]
    public float springDamper = 4600f;
    [Tooltip("Wheel radius (m) used to place the wheel VISUALS on the ground as the " +
             "suspension moves (measured from this SUV model's geometry).")]
    public float wheelVisualRadius = 0.42f;

    private Vector3[] suspAnchors;       // car-local spring anchor per corner (at axle height)
    private Transform[] suspWheelTr;     // wheel/joint transform that visually follows corner i
    private Vector3[] suspWheelRest;     // its rest localPosition
    private float[] suspVisualOffset;    // smoothed vertical wheel-visual offset per corner
    private bool grounded;
    private readonly RaycastHit[] suspHits = new RaycastHit[8];
    private const float SuspRayUp = 0.30f; // start rays this far above the anchor

    private float crashRecoveryTimer = 0f;
    private MeshDenter denter;

    // Impulse-based crash detection. OnCollisionEnter alone is unreliable against the
    // baked buildings' thin one-sided wall sheets: the Enter event often carries a
    // grazing relative velocity, and the real deceleration happens over the following
    // Stay frames — so an Enter speed threshold misses the crash entirely. Instead we
    // cache the most recent side-on contact (Enter OR Stay) and watch the rigidbody's
    // measured horizontal velocity loss; losing > crashVelocityLoss within ~0.12 s while
    // touching something is a crash, however PhysX chose to deliver the contacts.
    private readonly Vector3[] velHist = new Vector3[6];
    private int velHistIdx;
    private Vector3 lastContactPoint;
    private Vector3 lastContactNormal;
    private float lastContactTime = -999f;
    private bool lastContactVehicle;
    private const float crashVelocityLoss = 4f; // m/s lost over the history window

    private Rigidbody rb;
    private float currentSpeed = 0f;   // signed, along forward axis
    private float steerAngle = 0f;     // current driving steer angle (deg)
    private float visualSteer = 0f;    // smoothed front-wheel visual angle (deg)
    private float shiftCutTimer = 0f;
    private Quaternion flRest, frRest;
    private Vector3[] rimAxle;
    private float[] rimSign;
    private Text hudText;

    private float TopSpeedMs => (gearMaxSpeedKmh != null && gearMaxSpeedKmh.Length > 0)
        ? gearMaxSpeedKmh[gearMaxSpeedKmh.Length - 1] / 3.6f : 55f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.centerOfMass += Vector3.down * centerOfMassDrop;
        rb.mass = bodyMass;
        // Cap how violently PhysX may eject the body when something does overlap it —
        // the source of the old "car pops into the air" on curbs and seams.
        rb.maxDepenetrationVelocity = 3f;
        currentGear = Mathf.Clamp(currentGear, 1, GearCount);

        // Denting for the player car too (auto-added; instances its body meshes on first hit).
        denter = GetComponent<MeshDenter>();
        if (denter == null) denter = gameObject.AddComponent<MeshDenter>();
        denter.Initialize();

        if (showHud) CreateHud();

        DiscoverWheels();
        if (steerJointFL != null) flRest = steerJointFL.localRotation;
        if (steerJointFR != null) frRest = steerJointFR.localRotation;

        int n = (spinWheels != null) ? spinWheels.Length : 0;
        rimAxle = new Vector3[n];
        rimSign = new float[n];
        for (int i = 0; i < n; i++)
        {
            Transform w = spinWheels[i];
            rimAxle[i] = (w != null) ? MinExtentAxis(w) : Vector3.right;
            float s = (w != null) ? Mathf.Sign(Vector3.Dot(w.TransformDirection(rimAxle[i]), transform.right)) : 1f;
            rimSign[i] = (s == 0f) ? 1f : s;
        }

        BuildSuspension();
    }

    // One spring anchor per wheel corner, taken from the model's own wheel transforms
    // (front: the steer joints; rear: the rim transforms). Duplicates within 20 cm are
    // merged so a joint and the rim parented under it count once.
    private void BuildSuspension()
    {
        // The SUV model ships with convex colliders on the wheel meshes. Those rigid
        // cylinders were what actually carried the car (and slammed into curb walls,
        // causing the jumping). The springs replace them: only the root box collider
        // stays live, for walls/trees/vehicles.
        foreach (var col in GetComponentsInChildren<Collider>())
            if (col.gameObject != gameObject) col.enabled = false;

        var anchors = new List<Vector3>();
        var visuals = new List<Transform>();
        void Add(Transform t)
        {
            if (t == null) return;
            Vector3 lp = transform.InverseTransformPoint(t.position);
            foreach (var e in anchors) if ((e - lp).sqrMagnitude < 0.04f) return;
            anchors.Add(lp);
            visuals.Add(t);
        }
        Add(steerJointFL);
        Add(steerJointFR);
        if (spinWheels != null) foreach (var w in spinWheels) Add(w);

        if (anchors.Count < 3)
        {
            // Fallback: hang springs off the collider footprint corners.
            var bc = GetComponent<BoxCollider>();
            Vector3 c = bc.center, ext = bc.size * 0.5f;
            anchors.Clear(); visuals.Clear();
            anchors.Add(new Vector3(c.x - ext.x * 0.9f, 0.42f, c.z + ext.z * 0.7f)); visuals.Add(null);
            anchors.Add(new Vector3(c.x + ext.x * 0.9f, 0.42f, c.z + ext.z * 0.7f)); visuals.Add(null);
            anchors.Add(new Vector3(c.x - ext.x * 0.9f, 0.42f, c.z - ext.z * 0.4f)); visuals.Add(null);
            anchors.Add(new Vector3(c.x + ext.x * 0.9f, 0.42f, c.z - ext.z * 0.4f)); visuals.Add(null);
        }

        suspAnchors = anchors.ToArray();
        suspWheelTr = visuals.ToArray();
        suspWheelRest = new Vector3[suspWheelTr.Length];
        suspVisualOffset = new float[suspWheelTr.Length];
        for (int i = 0; i < suspWheelTr.Length; i++)
            if (suspWheelTr[i] != null) suspWheelRest[i] = suspWheelTr[i].localPosition;
    }

    // Spring + damper per corner, forces applied at the anchor so the body naturally
    // pitches over curbs and rolls in corners. Rays ignore the car's own colliders.
    private void ApplySuspension(float dt)
    {
        grounded = false;
        if (suspAnchors == null) return;

        float rayLen = SuspRayUp + suspensionRest + suspensionTravel;
        for (int i = 0; i < suspAnchors.Length; i++)
        {
            Vector3 anchor = transform.TransformPoint(suspAnchors[i]);
            Vector3 up = transform.up;
            Vector3 origin = anchor + up * SuspRayUp;

            float dist; // anchor -> ground along -up
            bool hit = RaycastGround(origin, -up, rayLen, out dist);
            float targetVisual;
            if (hit)
            {
                dist -= SuspRayUp;
                grounded = true;

                float offset = suspensionRest - dist;              // >0 when compressed
                float vel = Vector3.Dot(up, rb.GetPointVelocity(anchor));
                float force = offset * springStrength - vel * springDamper;
                if (force > 0f) rb.AddForceAtPosition(up * force, anchor);

                targetVisual = Mathf.Clamp(wheelVisualRadius - dist, -suspensionTravel, 0.25f);
            }
            else
            {
                targetVisual = -suspensionTravel * 0.6f; // wheel dangles when airborne
            }

            // Wheel visuals ride the spring (smoothed so they never snap).
            if (suspWheelTr[i] != null)
            {
                suspVisualOffset[i] = Mathf.MoveTowards(suspVisualOffset[i], targetVisual, 2.5f * dt);
                suspWheelTr[i].localPosition = suspWheelRest[i] + Vector3.up * suspVisualOffset[i];
            }
        }
    }

    private bool RaycastGround(Vector3 origin, Vector3 dir, float maxDist, out float dist)
    {
        int count = Physics.RaycastNonAlloc(origin, dir, suspHits, maxDist, ~0, QueryTriggerInteraction.Ignore);
        dist = float.MaxValue;
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            if (suspHits[i].collider.attachedRigidbody == rb) continue; // our own colliders
            if (suspHits[i].distance < dist) { dist = suspHits[i].distance; found = true; }
        }
        return found;
    }

    // Build a screen-space overlay HUD at runtime (reliable across render pipelines,
    // unlike IMGUI/OnGUI which wasn't drawing here).
    private void CreateHud()
    {
        var canvasGo = new GameObject("PlayerCarHUD");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        // Dark rounded background panel so the text is readable over any scenery.
        var panelGo = new GameObject("HudPanel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        var panel = panelGo.AddComponent<Image>();
        panel.color = new Color(0f, 0f, 0f, 0.55f);
        var prt = panel.rectTransform;
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0f, 0f);
        prt.anchoredPosition = new Vector2(16f, 16f);
        prt.sizeDelta = new Vector2(300f, 56f);

        var textGo = new GameObject("HudText");
        textGo.transform.SetParent(canvasGo.transform, false);
        hudText = textGo.AddComponent<Text>();
        hudText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hudText.fontSize = 28;
        hudText.fontStyle = FontStyle.Bold;
        hudText.color = Color.white;
        hudText.alignment = TextAnchor.LowerLeft;
        hudText.horizontalOverflow = HorizontalWrapMode.Overflow;
        hudText.verticalOverflow = VerticalWrapMode.Overflow;
        var outline = textGo.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);

        var rt = hudText.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(24f, 20f);
        rt.sizeDelta = new Vector2(700f, 90f);
    }

    private int GearCount => (gearMaxSpeedKmh != null) ? gearMaxSpeedKmh.Length : 5;

    private static Vector3 MinExtentAxis(Transform t)
    {
        var mf = t.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return Vector3.right;
        Vector3 s = mf.sharedMesh.bounds.size;
        if (s.x <= s.y && s.x <= s.z) return Vector3.right;
        if (s.y <= s.x && s.y <= s.z) return Vector3.up;
        return Vector3.forward;
    }

    private void DiscoverWheels()
    {
        if ((spinWheels == null || spinWheels.Length == 0) || steerJointFL == null || steerJointFR == null)
        {
            var rims = new List<Transform>();
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                string nm = t.name.ToLowerInvariant();
                if (nm.Contains("joint"))
                {
                    if (steerJointFL == null && nm.Contains("fl")) steerJointFL = t;
                    else if (steerJointFR == null && nm.Contains("fr")) steerJointFR = t;
                }
                else if (nm.Contains("wheel")) rims.Add(t);
            }
            if (spinWheels == null || spinWheels.Length == 0) spinWheels = rims.ToArray();
        }
    }

    void Update()
    {
        // Gear shifts read in Update so a quick tap is never missed between physics steps.
        if (Input.GetKeyDown(KeyCode.E) && currentGear < GearCount) { currentGear++; shiftCutTimer = shiftCutTime; }
        if (Input.GetKeyDown(KeyCode.Q) && currentGear > 1) { currentGear--; shiftCutTimer = shiftCutTime; }

        if (hudText != null)
        {
            int kmh = Mathf.RoundToInt(Mathf.Abs(currentSpeed) * 3.6f);
            hudText.text = $"Gear {currentGear}     {kmh} km/h";
        }
    }

    // A crash hands the car to PhysX for a moment so the impulse actually spins/shoves it
    // (otherwise the bicycle model below overwrites velocity/rotation every step and erases
    // the impact). When the timer expires we resync the scripted speed to whatever physics
    // left us with, so the driver smoothly "regains control" and drives on — dented.
    void OnCollisionEnter(Collision c) { HandleCollision(c, true); }
    void OnCollisionStay(Collision c) { HandleCollision(c, false); }

    private void HandleCollision(Collision c, bool isEnter)
    {
        bool vehicle = c.collider.GetComponentInParent<VehicleCrashController>() != null;

        // Cache the freshest side-on (wall-like) contact for the impulse-based crash
        // detection in FixedUpdate. Ground/curb contacts (upward normals) never count.
        for (int i = 0; i < c.contactCount; i++)
        {
            ContactPoint cp = c.GetContact(i);
            if (Mathf.Abs(cp.normal.y) >= 0.5f) continue;
            lastContactPoint = cp.point;
            lastContactNormal = cp.normal;
            lastContactTime = Time.time;
            lastContactVehicle = vehicle;
            break;
        }

        // Clean frontal hits still crash immediately on Enter (nice and responsive);
        // anything this path misses is caught by the velocity-loss check.
        if (!isEnter) return;
        float relSpeed = c.relativeVelocity.magnitude;
        if (relSpeed < (vehicle ? crashSpeedThreshold : staticCrashSpeedThreshold)) return;

        bool sideHit = vehicle;
        if (!vehicle)
            for (int i = 0; i < c.contactCount; i++)
                if (Mathf.Abs(c.GetContact(i).normal.y) < 0.5f) { sideHit = true; break; }
        if (!sideHit) return;

        crashRecoveryTimer = vehicle ? recoverTime : staticRecoverTime;
        if (denter != null)
            for (int i = 0; i < c.contactCount; i++)
            {
                ContactPoint cp = c.GetContact(i);
                if (!vehicle && Mathf.Abs(cp.normal.y) >= 0.5f) continue; // don't dent on ground contacts
                denter.DentAt(cp.point, cp.normal, relSpeed);
            }
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // Springs run in EVERY state (including crash recovery) — they are what keeps the
        // body off the ground now that the collider bottom sits at bumper height.
        ApplySuspension(dt);

        // Impulse-based crash detection: horizontal velocity lost over the ~0.12 s
        // history window, while touching something side-on. Catches wall/structure hits
        // whose OnCollisionEnter came in below the speed threshold (thin mesh sheets).
        Vector3 velNow = rb.linearVelocity;
        Vector3 velOld = velHist[velHistIdx];
        velHist[velHistIdx] = velNow;
        velHistIdx = (velHistIdx + 1) % velHist.Length;
        Vector3 dvVec = velNow - velOld; dvVec.y = 0f;
        if (crashRecoveryTimer <= 0f && dvVec.magnitude > crashVelocityLoss
            && Time.time - lastContactTime < 0.12f)
        {
            crashRecoveryTimer = lastContactVehicle ? recoverTime : staticRecoverTime;
            if (denter != null) denter.DentAt(lastContactPoint, lastContactNormal, dvVec.magnitude);
        }

        if (crashRecoveryTimer > 0f)
        {
            crashRecoveryTimer -= dt;
            if (crashRecoveryTimer <= 0f)
            {
                // Resume driving from whatever motion the crash left us with.
                currentSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
                steerAngle = 0f;
            }
            return; // let physics own the body (spin/skid) during recovery
        }

        // If something solid soaked up our momentum (sub-threshold scrape, shoved by a
        // car, ...), adopt the real speed instead of grinding against the obstacle.
        float actualFwd = Vector3.Dot(rb.linearVelocity, transform.forward);
        if (grounded && Mathf.Abs(actualFwd - currentSpeed) > 3f) currentSpeed = actualFwd;

        float throttle = Input.GetAxis("Vertical");
        float steerInput = Input.GetAxis("Horizontal");
        bool handbrake = Input.GetKey(KeyCode.Space);
        if (shiftCutTimer > 0f) shiftCutTimer -= dt;

        UpdateLongitudinal(throttle, handbrake, dt);
        UpdateSteering(steerInput, dt);
        if (grounded) ApplyBicycleMotion(dt); // airborne: ballistic, no tyre authority
        UpdateWheelVisuals(dt, steerInput);
    }

    private void UpdateLongitudinal(float throttle, bool handbrake, float dt)
    {
        if (handbrake)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, handbrakeDeceleration * dt);
            return;
        }

        if (throttle > 0.01f)
        {
            // Forward: gear-limited engine pull.
            int g = Mathf.Clamp(currentGear - 1, 0, GearCount - 1);
            float gearMax = gearMaxSpeedKmh[g] / 3.6f;
            float engage = gearEngageSpeedKmh[g] / 3.6f;

            // Bog factor: a high gear barely pulls below its engage speed (can't launch).
            float bog = (engage <= 0.01f) ? 1f : Mathf.Clamp01(currentSpeed / engage);
            bog *= bog; // sharpen, so it's clearly weak when below the engage speed

            // Power cut during a shift.
            if (shiftCutTimer > 0f) bog = 0f;

            // Headroom: pull eases off as the gear nears its top speed; if we're ABOVE
            // this gear's top (e.g. just downshifted), it engine-brakes us back down.
            float headroom = 1f - currentSpeed / Mathf.Max(gearMax, 0.1f);
            float aLong = gearAccel[g] * throttle * bog * headroom;

            currentSpeed += aLong * dt;
            // Don't let pull push past the active gear's top speed.
            if (headroom > 0f) currentSpeed = Mathf.Min(currentSpeed, gearMax);
        }
        else if (throttle < -0.01f)
        {
            // S: brake while rolling forward, then reverse (reverse ignores the gearbox).
            float target = (currentSpeed > 0.1f) ? 0f : -maxReverseSpeed;
            float rate = (currentSpeed > 0.1f) ? brakeDeceleration : reverseAccel;
            currentSpeed = Mathf.MoveTowards(currentSpeed, target, rate * dt);
        }
        else
        {
            // Coast down.
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, coastDeceleration * dt);
        }

        if (Mathf.Abs(currentSpeed) < 0.02f) currentSpeed = 0f;
    }

    private void UpdateSteering(float steerInput, float dt)
    {
        // Driver's requested steering angle; the speed-based cornering limit is applied
        // in ApplyBicycleMotion via the grip clamp.
        float targetSteer = steerInput * maxSteerAngle;
        steerAngle = Mathf.MoveTowards(steerAngle, targetSteer, steerSpeed * dt);
    }

    private void ApplyBicycleMotion(float dt)
    {
        float L = Mathf.Max(wheelBase, 0.1f);
        float yaw = rb.rotation.eulerAngles.y * Mathf.Deg2Rad;
        Vector3 fwd = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));

        Vector3 pos = rb.position;
        Vector3 rear = pos - fwd * (L * 0.5f);
        Vector3 front = pos + fwd * (L * 0.5f);

        // Grip limit: the fastest you go, the smaller the steering angle the tyres can
        // actually hold without exceeding maxLateralAccel. This makes sharp turns
        // physically impossible at speed (tan(steer) <= a_lat * L / v^2).
        float vAbs = Mathf.Abs(currentSpeed);
        float gripSteer = maxSteerAngle;
        if (vAbs > 0.5f)
            gripSteer = Mathf.Atan(maxLateralAccel * L / (vAbs * vAbs)) * Mathf.Rad2Deg;
        float effSteer = Mathf.Clamp(steerAngle, -gripSteer, gripSteer);

        Vector3 steerDir = Quaternion.Euler(0f, effSteer, 0f) * fwd;
        rear += fwd * currentSpeed * dt;
        front += steerDir * currentSpeed * dt;

        Vector3 newFwd = front - rear;
        if (newFwd.sqrMagnitude < 1e-6f) newFwd = fwd;
        newFwd.y = 0f; newFwd.Normalize();
        Vector3 newPos = (front + rear) * 0.5f;

        // Yaw is scripted as an angular VELOCITY (not MoveRotation with pitch/roll zeroed,
        // which rigidly locked the body flat): the suspension is free to pitch the car up
        // a curb ramp and roll it in corners, which is exactly the "one firm bump"
        // real-world feel that was missing.
        float newYawDeg = Mathf.Atan2(newFwd.x, newFwd.z) * Mathf.Rad2Deg;
        Vector3 av = rb.angularVelocity;
        av.y = Mathf.DeltaAngle(rb.rotation.eulerAngles.y, newYawDeg) * Mathf.Deg2Rad / dt;
        rb.angularVelocity = av;

        Vector3 vel = (newPos - pos) / dt;
        vel.y = rb.linearVelocity.y;
        rb.linearVelocity = vel;
    }

    private void UpdateWheelVisuals(float dt, float steerInput)
    {
        if (spinWheels != null)
        {
            float spinDeg = (currentSpeed / Mathf.Max(wheelRadius, 0.01f)) * Mathf.Rad2Deg * dt;
            for (int i = 0; i < spinWheels.Length; i++)
                if (spinWheels[i] != null)
                    spinWheels[i].Rotate(rimAxle[i], spinDeg * rimSign[i], Space.Self);
        }

        // Front-wheel visual angle also shrinks with speed, so the wheels visibly turn
        // less when fast (matching the reduced cornering). Tunable via the ref speed and
        // min fraction.
        float kmh = Mathf.Abs(currentSpeed) * 3.6f;
        float visFactor = Mathf.Lerp(1f, visualSteerMinFraction,
            Mathf.Clamp01(kmh / Mathf.Max(visualSteerSpeedRefKmh, 1f)));
        float targetVisual = Mathf.Clamp(steerInput, -1f, 1f) * visualSteerMax * visFactor;
        visualSteer = Mathf.MoveTowards(visualSteer, targetVisual, steerSpeed * dt);
        Quaternion steerRot = Quaternion.Euler(0f, visualSteer * steerVisualSign, 0f);
        if (steerJointFL != null) steerJointFL.localRotation = flRest * steerRot;
        if (steerJointFR != null) steerJointFR.localRotation = frRest * steerRot;
    }

}
