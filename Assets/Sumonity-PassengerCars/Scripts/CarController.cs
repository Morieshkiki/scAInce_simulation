/*
 * CarController.cs - Vehicle simulation controller
 * 
 * This script controls 3D car models from the "Generic passenger car pack" by Comrade1280
 * Source: https://sketchfab.com/3d-models/generic-passenger-car-pack-20f9af9b8a404d5cb022ac6fe87f21f5
 * License: Creative Commons Attribution 4.0 International (CC BY 4.0)
 * Attribution: "Generic passenger car pack" by Comrade1280 is licensed under CC BY 4.0
 */

using UnityEngine;
using static tumvt.sumounity.Vehicle;  
using tumvt.sumounity; 

namespace tum_car_controller
{

    // public class CarController : MonoBehaviour, IVehicleController
    public class CarController : MonoBehaviour, IVehicleController 
    {
        public string id { get; set; } // SUMO Identifier in Vehicle Dictionary
        private Rigidbody rb;

        [Header("Vehicle Control")]
        private bool inputAccelerate = false;
        private bool inputBrake = false;
        private float toruqeInputAccelerate = 0f;
        private float toruqeInputBrake = 0f;
        private bool inputLeft = false;
        private bool inputRight = false;
        // TELEPORT MODE (default ON): snap each car exactly to the SUMO position
        // (positionX/positionY + rotation) via SumoTaxiTeleport, kinematically.
        // With this OFF the car free-runs a physics bicycle model steered by the
        // lookahead point, which drifts off-road and lags behind SUMO when the
        // lookahead is approximate. For an exact, on-road, GUI-synced view we keep
        // this true. (Private/non-serialized, so this code default is authoritative.)
        private bool isTeleportOnlyMode = true;

        [Header("Manual Control")]
        [Tooltip("Enable to drive the car manually using WASD.")]
        public bool manualDriven = false; // default unchecked in inspector

        [Header("Vehicle Parameters")]
        public float currentSpeed = 0.0f;
        public float maxSpeed = 20f; // m/s
        public float acceleration = 10f;
        public float brakingForce = 20f;
        public float maxReverseSpeed = 10f;
        public float deceleration = 5f;
        public float maxSteeringAngle = 25f;
        public float wheelBase; // Distance between front and rear axle
        public float wheelRadius = 0.1f; // [m]
        private float steeringInput = 0f;

        [Header("Wheel References")]
        public GameObject wheelJointFL;
        public GameObject wheelFL;
        public GameObject wheelJointFR;
        public GameObject wheelFR;
        public GameObject wheelRL;
        public GameObject wheelRR;

        [Header("SUMO Integration")]
        private SumoSocketClient sock;  // Reference to SUMO socket client
        private PIDController pidControllerSpeed;
        private PIDController pidControllerDist;
        private bool bDrawGizmo;
        private Vector2 lookAheadMarker;
        public bool isSumoVehicle = true;
        private Vector2 rbMarker;
        private float stopState;

        // Wheel-spin support for teleport (SUMO) mode: the kinematic integrator is
        // skipped while teleporting, so currentSpeed would otherwise stay 0 and the
        // wheels wouldn't roll. We derive the real speed from the teleport position
        // delta each frame and feed it to RotateWheels().
        private Vector3 _lastTeleportPos;
        private bool _hasLastTeleportPos = false;

        [Header("Position Accuracy Logging")]
        [Tooltip("Enable position accuracy logging for this vehicle")]
        public bool enablePositionLogging = true;
        [Tooltip("Duration in seconds to ignore before starting to record (default: 20)")]
        public float recordingDelaySeconds = 20f;
        private Vector3 lastSumoGroundTruthPosition;
        private bool hasLoggedSuccessfully = false;  // Track if we've successfully logged at least once

        [Header("Dynamic Model")]
        [Tooltip("Distance from CG to front axle [m]")]
        public float lf = 1.5f;
        [Tooltip("Distance from CG to rear axle [m]")]
        public float lr = 1.5f;
        [Tooltip("Maximum steering angle at the wheels [deg]")]
        public float maxSteerAngleDeg = 30f;
        [Tooltip("How fast the steering can change [deg/s]")]
        public float steerRateDegPerSec = 120f;
        [Tooltip("Maximum lateral acceleration [m/s^2] to keep turning plausible")]
        public float maxLateralAccel = 8.0f;
        [Tooltip("Quadratic air drag coefficient (simplified)")]
        public float airDrag = 0.3f;
        [Tooltip("Rolling resistance (~g * crr)")]
        public float rollingResistance = 0.2f;

        // Internal state for the model
        private float steerAngleDeg = 0f;          // current wheel steer angle (deg)
        private float desiredSteerNormalized = 0f; // -1..1 desired steer
        private float driveCommand = 0f;           // -1..1 desired long accel (S=-1 brake, W=+1 accel)

        [Header("Coordinate Frame")]
        [Tooltip("Yaw offset (deg) to align dynamics forward (+Z) to the object's visual forward. Set 180 if your car appears to drive backwards.")]
        public float frameYawOffsetDeg = 0f;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            wheelBase = 3.0f; // Adjust based on your bus model

            // The prefabs serialize wheelRadius = 0.1 m, which makes the wheels spin
            // ~3-4x too fast (blurry). Use a realistic passenger-car radius unless an
            // intentionally larger value was set in the Inspector.
            if (wheelRadius < 0.2f) wheelRadius = 0.35f;

            // If lf/lr not set meaningfully, split the wheelbase
            if (lf <= 0f || lr <= 0f)
            {
                lf = wheelBase * 0.5f;
                lr = wheelBase * 0.5f;
            }

            // Initialize SUMO integration
            InitializeSumoIntegration();

            // Initialize internal state - no longer needed as we use rb.rotation directly
            // yawRad = Mathf.Deg2Rad * (rb.rotation.eulerAngles.y + frameYawOffsetDeg);
        }

        private void InitializeSumoIntegration()
        {
            // Get the socketclient with the step info
            sock = GameObject.FindObjectOfType<SumoSocketClient>();

            // Initialize controllers
            pidControllerDist = new PIDController(15.0f, 0.0f, 0.0f);
            pidControllerSpeed = new PIDController(1.0f, 0.0f, 0.0f);
            bDrawGizmo = true;
        }

        void Update()
        {
            // Prioritize manual driving when enabled
            if (manualDriven)
            {
                UpdateManualInput();
            }
            else if (isSumoVehicle)
            {
                UpdateSumoVehicle();
            }
            else
            {
                UpdateManualInput();
            }
        }

        private void UpdateSumoVehicle()
        {
            // feature not implemented yet. Will be used for performance optimization in user simulation studies.
            bool isInsidePhsyicsArea = SumoVehicleDetect(ref sock, id); // will always be true in the current state

            if (!isInsidePhsyicsArea || isTeleportOnlyMode)
            {
                HandleOutsidePhysicsArea();
            }
            else
            {
                HandleInsidePhysicsArea();
            }
        }

        private void HandleOutsidePhysicsArea()
        {
            rb = SumoTaxiTeleport(
                ref sock,
                id,
                rb,
                0.01f, // steeringGain
                ref pidControllerSpeed,
                ref pidControllerDist,
                ref lookAheadMarker
            );
            rb.isKinematic = true;

            // Derive the real speed from how far the car was teleported this frame,
            // signed along its forward axis, so the wheels roll at the correct rate.
            Vector3 here = rb.position;
            float dt = Time.deltaTime;
            if (_hasLastTeleportPos && dt > 0f)
            {
                float forwardDelta = Vector3.Dot(here - _lastTeleportPos, transform.forward);
                currentSpeed = forwardDelta / dt;
            }
            _lastTeleportPos = here;
            _hasLastTeleportPos = true;
        }

        private void HandleInsidePhysicsArea()
        {
            rb.isKinematic = false;
            rbMarker.x = rb.position.x;
            rbMarker.y = rb.position.z;

            var (steeringValue, torqueInput, desiredVelocity) =
                SumoVehicleControl(
                    ref sock,
                    id,
                    rb,
                    0.01f, // steeringGain
                    ref pidControllerSpeed,
                    ref pidControllerDist,
                    ref lookAheadMarker
                );

            UpdateVehicleControls(steeringValue, torqueInput, desiredVelocity);
            stopState = getVehicleStopState(ref sock, id);

            // Log position accuracy
            if (enablePositionLogging)
            {
                LogPositionAccuracy();
            }
        }

        private void UpdateVehicleControls(float steeringValue, float torqueInput, float desiredVelocity)
        {
            maxSpeed = desiredVelocity;

            if (torqueInput > 0)
            {
                toruqeInputAccelerate = torqueInput;
                toruqeInputBrake = 0f;
            }
            else
            {
                toruqeInputBrake = torqueInput;
                toruqeInputAccelerate = 0f;
            }

            // Steering value expected in [-1,1]
            desiredSteerNormalized = Mathf.Clamp(steeringValue, -1f, 1f);
            steeringInput = desiredSteerNormalized; // keep legacy field updated for visuals
        }

        void RotateWheels()
        {

            // Convert linear velocity to angular velocity (in radians per second)
            float angularVelocity = currentSpeed / wheelRadius;

            // Convert to degrees per second
            float degreesPerSecond = angularVelocity * Mathf.Rad2Deg;

            // Calculate rotation amount per frame
            float rotationAmount = degreesPerSecond * Time.deltaTime;

            if (wheelFL != null && wheelFR != null && wheelRL != null && wheelRR != null)
            {
                wheelFL.transform.Rotate(Vector3.left, -1 * rotationAmount);
                wheelFR.transform.Rotate(Vector3.left, -1 * rotationAmount);
                wheelRL.transform.Rotate(Vector3.left, -1 * rotationAmount);
                wheelRR.transform.Rotate(Vector3.left, -1 * rotationAmount);
            }
        }


        void ApplySteeringWheelRotation()
        {
            // Calculate the current steering angle based on the input 

            float visualSteeringMultiplicatorGain = 1f; // Use 1:1 ratio between visual and physics
            float visualSteeringAngle = steerAngleDeg * visualSteeringMultiplicatorGain;
            visualSteeringAngle = Mathf.Clamp(visualSteeringAngle, -maxSteeringAngle * visualSteeringMultiplicatorGain, maxSteeringAngle * visualSteeringMultiplicatorGain);

            // Apply the steering angle to the front wheels (Y axis assumed up)
            // Add 180° Y-axis offset to flip wheels so rims face outward
            Quaternion steeringRotation = Quaternion.Euler(0, visualSteeringAngle + 180f, 0);
            if (wheelJointFL != null && wheelJointFR != null)
            {
                wheelJointFL.transform.localRotation = steeringRotation;
                wheelJointFR.transform.localRotation = steeringRotation;
            }
        }

        void UpdateManualInput()
        {
            // WASD manual controls
            inputAccelerate = Input.GetKey(KeyCode.W);
            inputBrake = Input.GetKey(KeyCode.S);
            inputLeft = Input.GetKey(KeyCode.A);
            inputRight = Input.GetKey(KeyCode.D);

            // desired steer command in [-1,1]
            // Left = negative steering (turn left), Right = positive steering (turn right)
            desiredSteerNormalized = 0f;
            if (inputLeft) desiredSteerNormalized -= 1f;  // A key = turn left = negative
            if (inputRight) desiredSteerNormalized += 1f; // D key = turn right = positive
            desiredSteerNormalized = Mathf.Clamp(desiredSteerNormalized, -1f, 1f);
            steeringInput = desiredSteerNormalized; // keep legacy field updated for gizmos/visuals
        }

        void OnDrawGizmos()
        {
            if (bDrawGizmo)
            {
                Gizmos.color = Color.red;
                Vector3 LadPoint = new Vector3(lookAheadMarker.x, 0.1f, lookAheadMarker.y);
                Gizmos.DrawSphere(LadPoint, 1.0f);

                Gizmos.color = Color.blue;
                Vector3 rbMarkerPoint = new Vector3(rbMarker.x, 0.1f, rbMarker.y);
                Gizmos.DrawSphere(rbMarkerPoint, 1.0f);
            }
        }


        void FixedUpdate()
        {
            // Update kinematic model and then visuals
            UpdateVehicleKinematics();
            RotateWheels();
            ApplySteeringWheelRotation();

        }

        // Improved kinematic bicycle model with proper Unity coordinate system
        void UpdateVehicleKinematics()
        {
            if (rb == null) return;

            // In teleport mode the car's pose is driven entirely by SumoTaxiTeleport
            // (see HandleOutsidePhysicsArea). Running the bicycle integrator here would
            // (a) fight the teleport with MovePosition and (b) decay currentSpeed back
            // to 0, freezing the wheels. So skip it and keep the SUMO-derived speed.
            if (isSumoVehicle && !manualDriven && isTeleportOnlyMode)
                return;

            float dt = Time.fixedDeltaTime;

            // 1) Determine drive command (-1..+1)
            if (manualDriven || !isSumoVehicle)
            {
                driveCommand = 0f;
                if (inputAccelerate) driveCommand += 1f;
                if (inputBrake) driveCommand -= 1f;
                driveCommand = Mathf.Clamp(driveCommand, -1f, 1f);
            }
            else
            {
                // From SUMO torque inputs: sign is enough for a simple mapping
                if (toruqeInputAccelerate > 0f)
                    driveCommand = 1f;
                else if (toruqeInputBrake > 0f)
                    driveCommand = -1f;
                else
                    driveCommand = 0f;
            }

            // 2) Smooth steering towards target (deg)
            float targetSteerDeg = Mathf.Clamp(desiredSteerNormalized, -1f, 1f) * Mathf.Min(maxSteeringAngle, maxSteerAngleDeg);
            float steerStep = steerRateDegPerSec * dt;
            steerAngleDeg = Mathf.MoveTowards(steerAngleDeg, targetSteerDeg, steerStep);

            // 3) Longitudinal model with air drag and rolling resistance
            float aLong = 0f;
            if (driveCommand > 0f && currentSpeed < maxSpeed)
            {
                aLong = acceleration;
            }
            else if (driveCommand < 0f && currentSpeed > -maxReverseSpeed)
            {
                aLong = -brakingForce;
            }
            else if (Mathf.Approximately(driveCommand, 0f))
            {
                aLong = -Mathf.Sign(currentSpeed) * deceleration;
            }

            // Apply air drag and rolling resistance
            float dragForce = airDrag * currentSpeed * currentSpeed * Mathf.Sign(currentSpeed);
            float rollingForce = rollingResistance * Mathf.Sign(currentSpeed);
            aLong -= (dragForce + rollingForce);

            if (Mathf.Approximately(currentSpeed, 0f) && Mathf.Approximately(driveCommand, 0f))
            {
                aLong = 0f;
            }

            // 4) Integrate speed and clamp
            currentSpeed += aLong * dt;
            currentSpeed = Mathf.Clamp(currentSpeed, -maxReverseSpeed, maxSpeed);
            if (Mathf.Abs(currentSpeed) < 0.001f && Mathf.Approximately(driveCommand, 0f))
                currentSpeed = 0f;

            // 5) Bicycle kinematics with proper Unity coordinate system
            float L = Mathf.Max(lf + lr, 0.001f);
            float steerRad = steerAngleDeg * Mathf.Deg2Rad;

            // Calculate yaw rate using bicycle model
            float yawRate = (currentSpeed / L) * Mathf.Tan(steerRad);

            // Limit lateral acceleration to prevent unrealistic turning
            float maxYawRate = maxLateralAccel / Mathf.Max(Mathf.Abs(currentSpeed), 0.1f);
            yawRate = Mathf.Clamp(yawRate, -maxYawRate, maxYawRate);

            // Get current world heading from rigidbody rotation
            float currentYawWorld = rb.rotation.eulerAngles.y * Mathf.Deg2Rad;

            // Update heading
            currentYawWorld += yawRate * dt;

            // Apply frame offset correction
            float adjustedYaw = currentYawWorld + frameYawOffsetDeg * Mathf.Deg2Rad;

            // Calculate velocity in Unity's coordinate system (Z forward, X right)
            Vector3 pos = rb.position;
            // In Unity: Z is forward, X is right
            pos.x += currentSpeed * Mathf.Sin(adjustedYaw) * dt;  // Right/left movement
            pos.z += currentSpeed * Mathf.Cos(adjustedYaw) * dt;  // Forward/backward movement

            // Apply pose - convert back to degrees for Quaternion
            rb.MovePosition(pos);
            rb.MoveRotation(Quaternion.Euler(0f, currentYawWorld * Mathf.Rad2Deg, 0f));
        }

        public void SetTeleportOnlyMode(bool value)
        {
            isTeleportOnlyMode = value;
        }

        /// <summary>
        /// Log position accuracy by comparing Unity physics position with SUMO ground truth
        /// </summary>
        private void LogPositionAccuracy()
        {
            // Skip recording during the initial delay period
            if (Time.time < recordingDelaySeconds)
            {
                return;
            }

            // Add diagnostic logging to understand why data isn't being written
            if (sock == null)
            {
                Debug.LogWarning($"[CarController] {id}: Cannot log position - sock is null");
                return;
            }
            
            if (sock.StepInfo == null)
            {
                Debug.LogWarning($"[CarController] {id}: Cannot log position - sock.StepInfo is null");
                return;
            }

            // Get SUMO ground truth position for this vehicle
            SerializableVehicle sumoVehicle = GetSumoVehicleData();
            if (sumoVehicle == null)
            {
                Debug.LogWarning($"[CarController] {id}: Cannot log position - SUMO vehicle data not found");
                return;
            }

            Vector3 sumoGroundTruth = new Vector3(sumoVehicle.positionX, 0, sumoVehicle.positionY);
            Vector3 unityPosition = rb.position;

            // Store for reference
            lastSumoGroundTruthPosition = sumoGroundTruth;

            // Log to the global position accuracy logger
            PositionAccuracyLogger.Instance.LogPositionAccuracy(
                id,
                unityPosition,
                sumoGroundTruth,
                currentSpeed,
                steerAngleDeg
            );
            
            // One-time success message to confirm logging is working
            if (!hasLoggedSuccessfully)
            {
                Debug.Log($"[CarController] {id}: Successfully logged position data to PositionAccuracyLogger");
                hasLoggedSuccessfully = true;
            }
        }

        /// <summary>
        /// Get SUMO vehicle data from the step info
        /// </summary>
        private SerializableVehicle GetSumoVehicleData()
        {
            if (sock == null || sock.StepInfo == null || sock.StepInfo.vehicleList == null)
                return null;

            foreach (SerializableVehicle veh in sock.StepInfo.vehicleList)
            {
                if (id == veh.id)
                {
                    return veh;
                }
            }
            return null;
        }

    }
}
