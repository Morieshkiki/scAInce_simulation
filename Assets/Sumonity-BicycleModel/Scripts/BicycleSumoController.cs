using UnityEngine;
using tumvt.sumounity;
using static tumvt.sumounity.Vehicle;
using BikeURP;

namespace BikeURP
{
    /// <summary>
    /// SUMO integration wrapper for BicyclePhysicsController.
    /// Implements IVehicleController to connect with SUMO backend and control the bicycle.
    /// Disables manual input when SUMO control is active.
    /// </summary>
    public class BicycleSumoController : MonoBehaviour, IVehicleController
    {
        // SUMO vehicle identifier (from IVehicleController interface)
        public string id { get; set; }
        
        [Header("SUMO Integration")]
        public bool isSumoVehicle = true;
        public bool isTeleportOnlyMode = false;
        
        [Header("References")]
        private BicyclePhysicsController physicsController;
        private Rigidbody rb;
        private SumoSocketClient sock;
        
        [Header("SUMO Control")]
        private PIDController pidControllerSpeed;
        private PIDController pidControllerDist;
        private Vector2 lookAheadMarker;
        private Vector2 rbMarker;
        private float stopState;
        
        [Header("Debug Visualization")]
        private bool bDrawGizmo = true;

        void Start()
        {
            // Get required components
            physicsController = GetComponent<BicyclePhysicsController>();
            if (physicsController == null)
            {
                Debug.LogError("BicycleSumoController requires BicyclePhysicsController component!");
                enabled = false;
                return;
            }

            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError("BicycleSumoController requires Rigidbody component!");
                enabled = false;
                return;
            }

            // Initialize SUMO integration
            InitializeSumoIntegration();
        }

        private void InitializeSumoIntegration()
        {
            // Get the socketclient with the step info
            sock = GameObject.FindObjectOfType<SumoSocketClient>();
            
            if (sock == null)
            {
                Debug.LogWarning("SumoSocketClient not found. SUMO control will not be available.");
                isSumoVehicle = false;
                return;
            }

            // Initialize PID controllers
            // Distance controller: more aggressive to follow path accurately
            pidControllerDist = new PIDController(15.0f, 0.0f, 0.0f);
            // Speed controller: smoother for bicycle acceleration
            pidControllerSpeed = new PIDController(1.0f, 0.0f, 0.0f);
            
            bDrawGizmo = true;
        }

        void Update()
        {
            if (isSumoVehicle && sock != null)
            {
                UpdateSumoVehicle();
            }
            // If not SUMO vehicle, the BicyclePhysicsController handles manual input
        }

        private void UpdateSumoVehicle()
        {
            // Feature not implemented yet. Will be used for performance optimization in user simulation studies.
            bool isInsidePhysicsArea = SumoVehicleDetect(ref sock, id); // will always be true in the current state
            
            if (!isInsidePhysicsArea || isTeleportOnlyMode)
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
            // Use teleport mode - directly position the vehicle without physics simulation
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
            
            // Disable physics controller input when teleporting
            physicsController.SetThrottle(0f);
            physicsController.SetBrake(0f);
            physicsController.SetSteer(0f);
        }

        private void HandleInsidePhysicsArea()
        {
            rb.isKinematic = false;
            
            // Update marker position for debugging
            rbMarker.x = rb.position.x;
            rbMarker.y = rb.position.z;

            // Get SUMO control values
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

            // Apply controls to the physics controller
            UpdatePhysicsControllerInputs(steeringValue, torqueInput, desiredVelocity);
            
            // Get vehicle stop state from SUMO
            stopState = getVehicleStopState(ref sock, id);
        }

        private void UpdatePhysicsControllerInputs(float steeringValue, float torqueInput, float desiredVelocity)
        {
            // Update max speed based on SUMO desired velocity
            physicsController.maxSpeed = desiredVelocity;

            // Convert torque input to throttle/brake commands
            // Positive torque = throttle, negative = brake
            if (torqueInput > 0)
            {
                // Normalize torque to 0-1 range for throttle
                float throttle = Mathf.Clamp01(torqueInput);
                physicsController.SetThrottle(throttle);
                physicsController.SetBrake(0f);
            }
            else if (torqueInput < 0)
            {
                // Normalize negative torque to 0-1 range for brake
                float brake = Mathf.Clamp01(-torqueInput);
                physicsController.SetThrottle(0f);
                physicsController.SetBrake(brake);
            }
            else
            {
                // No input - coast
                physicsController.SetThrottle(0f);
                physicsController.SetBrake(0f);
            }

            // Apply steering using the analog method for precise control
            // steeringValue is typically in range [-1, 1]
            // Convert to angle in degrees based on max steering angle
            float steeringAngleDeg = steeringValue * physicsController.maxSteerDeg;
            physicsController.SetSteerAnalog(steeringAngleDeg);
        }

        void OnDrawGizmos()
        {
            if (bDrawGizmo && isSumoVehicle)
            {
                // Draw look-ahead marker in red
                Gizmos.color = Color.red;
                Vector3 ladPoint = new Vector3(lookAheadMarker.x, 0.1f, lookAheadMarker.y);
                Gizmos.DrawSphere(ladPoint, 1.0f);

                // Draw rigidbody marker in blue
                Gizmos.color = Color.blue;
                Vector3 rbMarkerPoint = new Vector3(rbMarker.x, 0.1f, rbMarker.y);
                Gizmos.DrawSphere(rbMarkerPoint, 1.0f);
            }
        }

        /// <summary>
        /// Set teleport-only mode. When true, vehicle will only use teleportation without physics.
        /// </summary>
        public void SetTeleportOnlyMode(bool value)
        {
            isTeleportOnlyMode = value;
        }

        /// <summary>
        /// Get current SUMO vehicle stop state
        /// </summary>
        public float GetStopState()
        {
            return stopState;
        }

        /// <summary>
        /// Check if vehicle is currently controlled by SUMO
        /// </summary>
        public bool IsSumoControlled()
        {
            return isSumoVehicle && sock != null;
        }
    }
}
