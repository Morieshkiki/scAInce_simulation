using UnityEngine;

namespace BikeURP
{
    /// <summary>
    /// Applies a visual lean to a target transform based on the bicycle's current speed and steering angle.
    /// Intended to complement <see cref="BicyclePhysicsController"/> without requiring built-in frameRoot lean.
    /// </summary>
    public class BicycleLeanAnimator : MonoBehaviour
    {
        [Tooltip("Bicycle controller providing speed and steering data. Auto-assigned if left empty.")]
        public BicyclePhysicsController controller;
        [Tooltip("Transform that receives the lean roll (defaults to controller.frameRoot if available).")]
        public Transform leanRoot;
        [Tooltip("Maximum lean angle in degrees applied to the lean root.")]
        public float maxLeanDeg = 25f;
        [Tooltip("Sensitivity scaling for lean response (higher values lean more for the same curvature).")]
        public float leanSensitivity = 1f;
        [Tooltip("Smoothing factor (1/s) for reaching the target lean angle.")]
        public float leanSmoothing = 8f;
    [Tooltip("Invert lean direction for rigs that respond opposite to steering input.")]
    public bool invertLeanDirection;

        private Quaternion _baseLocalRotation;
        private float _currentLeanDeg;
        private bool _hasCachedBase;

        private void Awake()
        {
            if (!controller)
            {
                controller = GetComponent<BicyclePhysicsController>();
                if (!controller)
                    controller = GetComponentInParent<BicyclePhysicsController>();
            }

            if (!leanRoot && controller)
            {
                leanRoot = controller.frameRoot ? controller.frameRoot : controller.transform;
            }

            CacheBaseRotation();
        }

        private void OnEnable()
        {
            CacheBaseRotation();
        }

        private void CacheBaseRotation()
        {
            if (!leanRoot) return;
            _baseLocalRotation = leanRoot.localRotation;
            _hasCachedBase = true;
        }

        private void LateUpdate()
        {
            if (!controller || !leanRoot || !_hasCachedBase)
                return;

            float speed = controller.GetSpeedMps();
            float steerDeg = controller.GetSteerAngleDeg();
            float steerRad = steerDeg * Mathf.Deg2Rad;
            float curvature = Mathf.Abs(controller.wheelbase) > 1e-3f ? Mathf.Tan(steerRad) / controller.wheelbase : 0f;
            const float g = 9.81f;
            float targetLean = Mathf.Atan(leanSensitivity * speed * speed * curvature / g) * Mathf.Rad2Deg;
            targetLean = Mathf.Clamp(targetLean, -maxLeanDeg, maxLeanDeg);
            targetLean *= invertLeanDirection ? 1f : -1f;

            float alpha = 1f - Mathf.Exp(-leanSmoothing * Time.deltaTime);
            _currentLeanDeg = Mathf.Lerp(_currentLeanDeg, targetLean, alpha);

            leanRoot.localRotation = _baseLocalRotation * Quaternion.AngleAxis(_currentLeanDeg, Vector3.forward);
        }

        private void OnDisable()
        {
            if (leanRoot && _hasCachedBase)
            {
                leanRoot.localRotation = _baseLocalRotation;
            }
            _currentLeanDeg = 0f;
        }
    }
}
