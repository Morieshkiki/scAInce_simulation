using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TUD_VehiclePlayer : TUD_Player
{
    [SerializeField] SimpleVehicle vehicle;
    [SerializeField] Transform _rig;
    [SerializeField] Camera _camera;

    [SerializeField] bool debugMode;
    [SerializeField] InputActionReference debugInput;
    [SerializeField] InputActionReference steerInput, gasInput, brakeInput, resetInput, recenterInput;

    [SerializeField] Transform steeringWheelModel;
    [SerializeField] float steeringWheelMaxAngle = 450;

    [SerializeField] Transform startPosition;

    [SerializeField] Transform headTargetPos;

    public override Vector3 position => transform.position;
    public override Transform rig => _rig;
    public override Camera camera => _camera;

    private void OnEnable()
    {
        if (debugMode)
            debugInput.action.Enable();
        steerInput.action.Enable();
        gasInput.action.Enable();
        brakeInput.action.Enable();
        resetInput.action.Enable();
        recenterInput.action.Enable();
    }

    private void OnDisable()
    {
        if (debugMode)
            debugInput.action.Disable();
        steerInput.action.Disable();
        gasInput.action.Disable();
        brakeInput.action.Disable();
        resetInput.action.Disable();
        recenterInput.action.Disable();
    }

    public void ResetVehicle()
    {
        if (startPosition != null)
        {
            transform.position = startPosition.position;
            transform.rotation = startPosition.rotation;
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void RecenterCamera()
    {
        Vector3 delta = headTargetPos.position - camera.transform.position;
        delta = transform.InverseTransformVector(delta);
        rig.transform.localPosition += delta;
    }

    private void Update()
    {
        if (debugMode)
        {
            Vector2 debugSteer = debugInput.action.ReadValue<Vector2>();
            vehicle.steer = debugSteer.x;
            vehicle.accelerate = debugSteer.y;
        }
        else
        {
            vehicle.steer = steerInput.action.ReadValue<float>();
            vehicle.accelerate = gasInput.action.ReadValue<float>() - brakeInput.action.ReadValue<float>();
        }

        steeringWheelModel.localEulerAngles = new Vector3(steeringWheelModel.localEulerAngles.x, steeringWheelModel.localEulerAngles.y, vehicle.steer * steeringWheelMaxAngle);

        if (resetInput.action.WasPressedThisFrame())
            ResetVehicle();

        if (recenterInput.action.WasPressedThisFrame())
            RecenterCamera();
    }
}
