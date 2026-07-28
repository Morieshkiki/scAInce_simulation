using UnityEngine;
using tumvt.sumounity; 
using static tumvt.sumounity.Vehicle;  

namespace tum_car_controller
{
    public class BusController : MonoBehaviour, IVehicleController 
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
        private bool isTeleportOnlyMode = false;

        [Header("Vehicle Parameters")]
        public float currentSpeed = 0.0f;
        public float maxSpeed = 20f; // m/s
        public float acceleration = 10f;
        public float brakingForce = 20f;
        public float maxReverseSpeed = 10f;
        public float deceleration = 5f;
        public float maxSteeringAngle = 25f;
        public float wheelBase; // Distance between front and rear axle
        public float wheelRadius = 0.25f; // [m]
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

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            wheelBase = 8.0f; // Adjust based on your bus model

            // Initialize SUMO integration
            InitializeSumoIntegration();
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
            if(isSumoVehicle)
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

            steeringInput = steeringValue;
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
                wheelFL.transform.Rotate(Vector3.up, -1 * rotationAmount);
                wheelFR.transform.Rotate(Vector3.up, -1 * rotationAmount);
                wheelRL.transform.Rotate(Vector3.up, -1 * rotationAmount);
                wheelRR.transform.Rotate(Vector3.up, -1 * rotationAmount);
            }
        }


        void ApplySteeringWheelRotation()
        {
            // Calculate the current steering angle based on the input 

            float visualSteeringMultiplicatorGain = 3;
            float visualSteeringAngle = maxSteeringAngle * steeringInput * visualSteeringMultiplicatorGain;

            Mathf.Clamp(visualSteeringAngle,-maxSteeringAngle,maxSteeringAngle);
            
            // Apply the steering angle to the front wheels
            Quaternion steeringRotation = Quaternion.Euler(0, 0, visualSteeringAngle);
            if (wheelJointFL != null && wheelJointFR != null)
            {
                wheelJointFL.transform.localRotation = steeringRotation;
                wheelJointFR.transform.localRotation = steeringRotation;
            }
        }

        void UpdateManualInput()
        {
            inputBrake = Input.GetKey(KeyCode.UpArrow);
            inputAccelerate = Input.GetKey(KeyCode.DownArrow);
            inputLeft = Input.GetKey(KeyCode.LeftArrow);
            inputRight = Input.GetKey(KeyCode.RightArrow);

            steeringInput = 0f;
            if (inputLeft)
            {
                steeringInput = -0.1f;
            }
            else if (inputRight)
            {
                steeringInput = 0.1f;
            }
        }

        void OnDrawGizmos(){
            if (bDrawGizmo){
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
            RotateWheels();
            ApplySteeringWheelRotation();
            
            HandleAcceleration();

            if (currentSpeed != 0)  // Only steer if vehicle is moving
            {
                HandleSteering();
            }

            if (!isTeleportOnlyMode)
            {
                ApplyPhysics();
            }

        }

        void HandleAcceleration()
        {
            //sumo
            if(isSumoVehicle){
                if (toruqeInputAccelerate > 0 && currentSpeed < maxSpeed)
                {
                    currentSpeed += acceleration * Time.fixedDeltaTime;
                }
                else if (toruqeInputBrake > 0 && currentSpeed > -maxReverseSpeed)
                {
                    currentSpeed -= brakingForce * Time.fixedDeltaTime;
                }
                else
                {
                    currentSpeed = Mathf.MoveTowards(currentSpeed, 0, deceleration * Time.fixedDeltaTime);
                }
            }
            // manual
            else{
                if (inputBrake && currentSpeed < maxSpeed)
                {
                    currentSpeed += acceleration * Time.fixedDeltaTime;
                }
                else if (inputAccelerate && currentSpeed > -maxReverseSpeed)
                {
                    currentSpeed -= brakingForce * Time.fixedDeltaTime;
                }
                else
                {
                    currentSpeed = Mathf.MoveTowards(currentSpeed, 0, deceleration * Time.fixedDeltaTime);
                }
            }

        }

        void HandleSteering()
        {
            float speedGain = 0.01f; // make steering at low speeds more realistic
            Mathf.Clamp(speedGain*currentSpeed,-1f,1f);

            float steeringAngleCurrent = maxSteeringAngle * steeringInput;
            // Adjust the pivot point to the rear axle
            Vector3 pivotPoint = transform.position - transform.forward * wheelBase;
            Quaternion rotation = Quaternion.AngleAxis(steeringAngleCurrent, Vector3.up);
            rb.MovePosition(pivotPoint + rotation * (transform.position - pivotPoint));
            rb.MoveRotation(rb.rotation * rotation);
        }

        void ApplyPhysics()
        {
            Vector3 velocity = transform.forward * currentSpeed;
            rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);
        }

        public void SetTeleportOnlyMode(bool value)
        {
            isTeleportOnlyMode = value;
        }

    }
}
