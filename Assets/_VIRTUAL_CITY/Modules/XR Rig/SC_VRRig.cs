using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Numena.SC
{
    /// <summary>
    /// Provides simplified access to VR Rig components & features across multiple platforms.
    /// </summary>
    public class SC_VRRig : MonoBehaviour
    {
        static SC_VRRig main;

        [SerializeField] Camera _mainCamera;
        [SerializeField] GameObject _leftController, _rightController;

        public static Transform rigTransform => main.transform;
        public static Camera mainCamera => main._mainCamera;
        public static GameObject leftController => main._leftController;
        public static GameObject rightController => main._rightController;

        private static bool rot_set = false;

        private void Awake()
        {
            if (main != null && main.isActiveAndEnabled)
                Debug.LogError(nameof(SC_VRRig) + " is a singleton. Please make sure there is only one instance in the scene.");
            main = this;

            
        }

        private void Update()
        {
            if (!rot_set)
            {
                this.transform.eulerAngles = new Vector3(0, 0, 0);
                rot_set = true;
            }
        }

        private void OnDestroy()
        {
            main = null;
        }
    }
}
