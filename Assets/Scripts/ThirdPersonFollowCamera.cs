using UnityEngine;

// Smooth 3rd-person chase camera with free mouse-look. Attach to the Main Camera and
// set `target` to the player car. It rides behind/above the car, and the MOUSE orbits
// the view freely around the car while driving (yaw + pitch). When the mouse is idle
// it gently eases back to behind the car so you keep facing where you drive. Runs in
// LateUpdate (after the car moves) to avoid jitter; independent of SUMO traffic.
public class ThirdPersonFollowCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;            // assign the player car here

    [Header("Framing")]
    public float distance = 8f;         // how far behind the car
    public float height = 4f;           // how high above the car
    public float lookAtHeight = 1.5f;   // aim a bit above the car's pivot

    [Header("Smoothing")]
    public float followLerp = 5f;       // position easing (higher = tighter)
    public float rotationLerp = 5f;     // look easing

    [Header("Mouse look")]
    [Tooltip("Enable orbiting the camera with the mouse while driving.")]
    public bool enableMouseLook = true;
    [Tooltip("If true, only orbit while holding the right mouse button.")]
    public bool requireRightMouse = false;
    [Tooltip("Mouse sensitivity (deg per mouse unit).")]
    public float mouseSensitivity = 3f;
    [Tooltip("Resting tilt (deg) looking down at the car.")]
    public float defaultPitch = 12f;
    public float minPitch = -20f;
    public float maxPitch = 70f;
    [Tooltip("How fast the view eases toward center once recentering starts (deg/s). 0 = never recenter.")]
    public float recenterSpeed = 25f;
    [Tooltip("Seconds the mouse must stay idle before the view auto-recenters.")]
    public float recenterDelay = 3f;

    private float yawOffset = 0f;     // mouse yaw relative to the car's heading
    private float pitch;
    private float mouseIdleTimer = 0f; // seconds since the mouse last moved

    void Start()
    {
        pitch = defaultPitch;
    }

    void LateUpdate()
    {
        if (target == null) return;

        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");
        bool active = enableMouseLook && (!requireRightMouse || Input.GetMouseButton(1));

        if (active && (Mathf.Abs(mx) > 0.0001f || Mathf.Abs(my) > 0.0001f))
        {
            yawOffset += mx * mouseSensitivity;
            pitch -= my * mouseSensitivity;
            mouseIdleTimer = 0f;
        }
        else
        {
            mouseIdleTimer += Time.deltaTime;

            // Recenter only after the mouse has been idle for `recenterDelay`, and only
            // while the car is actively driven. Forward (W) -> ease behind the car;
            // reverse (S) -> ease to the FRONT (show the car's back). Idle car: hold.
            float drive = Input.GetAxisRaw("Vertical");
            bool driving = Mathf.Abs(drive) > 0.01f;
            if (recenterSpeed > 0f && mouseIdleTimer >= recenterDelay && driving)
            {
                float targetOffset = (drive < 0f) ? 180f : 0f;
                yawOffset = Mathf.MoveTowardsAngle(yawOffset, targetOffset, recenterSpeed * Time.deltaTime);
                pitch = Mathf.MoveTowards(pitch, defaultPitch, recenterSpeed * Time.deltaTime);
            }
        }
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        float carYaw = target.eulerAngles.y;
        Quaternion orbit = Quaternion.Euler(pitch, carYaw + yawOffset, 0f);
        Vector3 desiredPos = target.position + Vector3.up * height + orbit * (Vector3.back * distance);
        transform.position = Vector3.Lerp(transform.position, desiredPos, followLerp * Time.deltaTime);

        Vector3 lookAt = target.position + Vector3.up * lookAtHeight;
        Quaternion desiredRot = Quaternion.LookRotation(lookAt - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, rotationLerp * Time.deltaTime);
    }
}
