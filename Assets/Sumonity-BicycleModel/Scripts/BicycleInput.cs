using UnityEngine;

namespace BikeURP
{
    /// <summary>
    /// Minimal keyboard input for quick testing:
    /// - W/S: throttle forward/back
    /// - A/D: steer left/right
    /// - Space: brake
    /// Displays simple HUD with current speed.
    /// </summary>
    [RequireComponent(typeof(BicyclePhysicsController))]
    public class BicycleInput : MonoBehaviour
    {
        public BicyclePhysicsController controller;
    public float steerSensitivity = 2.0f; // scales horizontal input (analog)
    public bool useDigitalSteer = true;   // if true, A/D map to fixed angle via SetSteerDigital
    public float throttleAccel = 5.0f; // smoothing toward desired throttle

        private float _steer;
    private float _throttle;
    private BicycleSumoController sumoController; // Reference to SUMO controller if present

        void Reset()
        {
            controller = GetComponent<BicyclePhysicsController>();
        }

        void Awake()
        {
            if (!controller) controller = GetComponent<BicyclePhysicsController>();
            
            // Check if SUMO controller is present
            sumoController = GetComponent<BicycleSumoController>();
        }

        void Update()
        {
            if (!controller) return;
            
            // Disable manual input if SUMO controller is active
            if (sumoController != null && sumoController.IsSumoControlled())
            {
                return; // SUMO is controlling the vehicle, ignore manual input
            }

            // Steering smoother
            if (useDigitalSteer)
            {
                bool left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
                bool right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
                controller.SetSteerDigital(left, right);
            }
            else
            {
                float steerInput = Input.GetAxis("Horizontal");
                if (Mathf.Approximately(steerInput, 0f))
                {
                    if (Input.GetKey(KeyCode.A)) steerInput -= 1f;
                    if (Input.GetKey(KeyCode.D)) steerInput += 1f;
                }
                float steerCmd = Mathf.Clamp(steerInput * steerSensitivity, -1f, 1f);
                _steer = Mathf.Lerp(_steer, steerCmd, Time.deltaTime * 10f);
                controller.SetSteer(_steer);
            }

            // Brake
            bool braking = Input.GetKey(KeyCode.Space);
            controller.SetBrake(braking ? 1f : 0f);

            float vert = Input.GetAxis("Vertical");
            _throttle = Mathf.MoveTowards(_throttle, vert, throttleAccel * Time.deltaTime);
            controller.SetThrottle(_throttle);
        }

#if UNITY_EDITOR
        void OnGUI()
        {
            if (!controller) return;
            var speed = controller.GetSpeedKph();
            GUI.Label(new Rect(10, 10, 400, 20), $"Speed: {speed:F1} kph");
            
            // Show control mode
            string controlMode = "Manual";
            if (sumoController != null && sumoController.IsSumoControlled())
            {
                controlMode = "SUMO";
            }
            GUI.Label(new Rect(10, 30, 500, 20), $"Control Mode: {controlMode}");
            GUI.Label(new Rect(10, 50, 500, 20), "Controls: A/D steer, W/S throttle, Space brake");
        }
#endif
    }
}
