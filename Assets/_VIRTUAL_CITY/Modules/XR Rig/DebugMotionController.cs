using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Numena.Test
{
    public class DebugMotionController : MonoBehaviour
    {
        Vector2 motion;
        [SerializeField] float speed = 5;
        [SerializeField] Transform pointer;

        [SerializeField] InputActionReference moveInput, snapRotateLeftInput, snapRotateRightInput;

        private void OnEnable()
        {
            snapRotateLeftInput.action.performed += OnSnapRotateLeft;
            snapRotateRightInput.action.performed += OnSnapRotateRight;
        }

        private void OnDisable()
        {
            snapRotateLeftInput.action.performed -= OnSnapRotateLeft;
            snapRotateRightInput.action.performed -= OnSnapRotateRight;
        }

        // Update is called once per frame
        void Update()
        {
            motion = moveInput.action.ReadValue<Vector2>();
            motion *= speed;

            transform.position += pointer.TransformDirection(new Vector3(motion.x, 0, motion.y)) * Time.deltaTime;
        }

        void OnSnapRotateLeft(InputAction.CallbackContext context)
        {
            SnapRotate(-30);
        }

        void OnSnapRotateRight(InputAction.CallbackContext context)
        {
            SnapRotate(30);
        }

        void SnapRotate(float angle)
        {
            Vector3 camPos = pointer.position;
            transform.Rotate(Vector3.up, angle, Space.Self);
            transform.position += camPos - pointer.position;
        }
    }
}
