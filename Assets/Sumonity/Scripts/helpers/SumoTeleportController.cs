using UnityEngine;

namespace tumvt.sumounity
{
    // Lightweight SUMO-driven mover used for EVERY vehicle (cars, buses, trams).
    //
    // Each frame it finds this vehicle's latest SUMO state by id and INTERPOLATES
    // the transform from where it currently is toward the new SUMO position/heading
    // over the measured update interval. Because SUMO updates arrive only a few
    // times per second (and slower when the sim delay is high), interpolation is
    // what makes motion and especially TURNING look continuous instead of snapping
    // ("looking forward" mid-turn). It replaces the heavier physics CarController
    // for SUMO playback.
    public class SumoTeleportController : MonoBehaviour, IVehicleController
    {
        public string id { get; set; }

        // Estimated world velocity of this vehicle from its interpolated motion. Read by
        // VehicleCrashController at the instant of a crash so the handoff to physics keeps
        // the momentum the car actually had (smooth, believable shove). Zero when stopped.
        public Vector3 CurrentVelocity { get; private set; }
        private Vector3 _velPrevPos;
        private bool _hasVelPrev;

        [Tooltip("Vertical offset so the model sits ON the road (raise if it sinks below the surface).")]
        public float heightOffset = 0f;
        [Tooltip("Extra yaw in degrees if the model's forward axis is not +Z (e.g. set 90 if it renders sideways).")]
        public float yawOffset = 0f;

        [Tooltip("Wheel mesh transforms that should roll with speed. Auto-filled on spawn from the prefab's CarController wheel references.")]
        public Transform[] wheels;
        [Tooltip("Wheel radius (m) used to convert travel distance into spin.")]
        public float wheelRadius = 0.35f;
        private Vector3 lastWheelPos;
        private bool hasLastWheelPos = false;

        private SumoSocketClient sock;

        [Tooltip("Interpolation window in seconds. Leave at 0 to adapt automatically to the observed SUMO update rate (recommended). Set e.g. 0.1 to pin it to the bridge's --dt.")]
        public float fixedInterpolationWindow = 0f;
        [Tooltip("Widens the interpolation window slightly so a packet arriving a little late does not leave the vehicle standing still before it lands. 1.0 = no slack.")]
        [Range(1f, 1.5f)] public float intervalSlack = 1.15f;

        private bool initialized = false;
        private float lastStepTime = -1f;   // SUMO sim time of the last consumed update
        private float stepWallTime = 0f;    // Time.time when that update was received
        private float stepInterval = 0.1f;  // SMOOTHED real seconds between updates
        private bool hasIntervalEstimate = false;
        private Vector3 prevPos, targetPos;
        private Quaternion prevRot, targetRot;

        void Start()
        {
            sock = Object.FindObjectOfType<SumoSocketClient>();
            // We move the transform directly; make sure physics never fights us.
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
        }

        void Update()
        {
            if (sock == null || sock.StepInfo == null || sock.StepInfo.vehicleList == null) return;

            SerializableVehicle v = null;
            var list = sock.StepInfo.vehicleList;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].id == id) { v = list[i]; break; }
            }
            if (v == null) return;

            Vector3 sumoPos = new Vector3(v.positionX, heightOffset, v.positionY);
            // Stream sends rotation = SUMO angle + 180; the original teleport used
            // (rotation - 180) to face the SUMO heading, so we mirror that exactly.
            Quaternion sumoRot = Quaternion.Euler(0f, v.rotation - 180f + yawOffset, 0f);

            float stepTime = sock.StepInfo.time;

            if (!initialized)
            {
                transform.position = sumoPos;
                transform.rotation = sumoRot;
                prevPos = targetPos = sumoPos;
                prevRot = targetRot = sumoRot;
                lastStepTime = stepTime;
                stepWallTime = Time.time;
                initialized = true;
                return;
            }

            if (!Mathf.Approximately(stepTime, lastStepTime))
            {
                // A new SUMO step arrived: ease from where we ARE to the new target.
                //
                // The window is a SMOOTHED average of the observed packet gaps, not the
                // single previous gap. Trusting one measurement made the window track
                // network jitter: whenever the next gap ran longer than the last, u
                // saturated early, the vehicle arrived and stood still until the next
                // packet, then snapped - the visible stutter. An exponential average
                // converges on the real cadence (0.1 s at the bridge's default --dt)
                // and stops the jitter feeding back into the motion.
                float now = Time.time;
                float gap = Mathf.Clamp(now - stepWallTime, 0.02f, 3f);
                if (!hasIntervalEstimate) { stepInterval = gap; hasIntervalEstimate = true; }
                else stepInterval = Mathf.Lerp(stepInterval, gap, 0.15f);
                stepWallTime = now;
                lastStepTime = stepTime;
                prevPos = transform.position;
                prevRot = transform.rotation;
                targetPos = sumoPos;
                targetRot = sumoRot;
            }
            else
            {
                // Same step still streaming: keep the target current.
                targetPos = sumoPos;
                targetRot = sumoRot;
            }

            // Clamp01 is deliberate: never extrapolate past the last known SUMO pose,
            // so a late packet can never push a vehicle through a wall or another car.
            float window = (fixedInterpolationWindow > 0f ? fixedInterpolationWindow : stepInterval) * intervalSlack;
            float u = Mathf.Clamp01((Time.time - stepWallTime) / Mathf.Max(window, 0.0001f));
            transform.position = Vector3.Lerp(prevPos, targetPos, u);
            transform.rotation = Quaternion.Slerp(prevRot, targetRot, u);

            // Estimate world velocity from the actual frame-to-frame motion (used by the
            // crash handoff to seed physics with real momentum).
            if (_hasVelPrev && Time.deltaTime > 1e-5f)
                CurrentVelocity = (transform.position - _velPrevPos) / Time.deltaTime;
            _velPrevPos = transform.position;
            _hasVelPrev = true;

            RotateWheels();
        }

        // Roll the wheel meshes based on how far the body actually moved this frame
        // (signed along the heading). Uses the interpolated transform so the spin
        // matches the visible motion, and naturally stops at red lights (delta = 0).
        void RotateWheels()
        {
            if (wheels == null || wheels.Length == 0) return;

            Vector3 cur = transform.position;
            if (!hasLastWheelPos) { lastWheelPos = cur; hasLastWheelPos = true; return; }

            float forwardDelta = Vector3.Dot(cur - lastWheelPos, transform.forward);
            lastWheelPos = cur;

            float rotationAmount = (forwardDelta / Mathf.Max(wheelRadius, 0.01f)) * Mathf.Rad2Deg;
            for (int i = 0; i < wheels.Length; i++)
                if (wheels[i] != null)
                    wheels[i].Rotate(Vector3.left, -rotationAmount, Space.Self);
        }
    }
}
