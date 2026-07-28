using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SimpleVehicle : MonoBehaviour
{
    [SerializeField] SimpleWheel[] wheels;
    [SerializeField] SimpleWheel[] frontWheels;

    [SerializeField] Transform centerOfGravity;

    [SerializeField] float maxSteeringAngle = 50;
    public float maxSpeed = 50;
    public float maxSpeedReverse = 10;
    public float maxAcceleration = 350;
    public float maxBrake = 600;
    public float accellerationFalloff = 3; //1 = linear, >1 = curved
    public bool steeringFalloff = false;

    public float accelerate = 0;
    public float steer = 0;

    Rigidbody _rigidbody;
    Vector3 localVelocity = new Vector3();

    int _gear = 0;

    public float speed => localVelocity.z;
    public int gear => _gear;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (centerOfGravity != null)
            _rigidbody.centerOfMass = transform.InverseTransformPoint(centerOfGravity.position);
    }

    private void FixedUpdate()
    {
        localVelocity = transform.InverseTransformDirection(_rigidbody.linearVelocity);
        Vector3 localKPH = localVelocity * 3.6f;
        float accelFactor = 0;
        if(accelerate > 0)
        {
            if (localVelocity.z < 0)
                accelFactor = 1;
            else
                accelFactor = 1 - Mathf.Pow(Mathf.Clamp01(localKPH.z / maxSpeed),accellerationFalloff);
        }
        else
        {
            if (localVelocity.z > 0)
                accelFactor = 1;
            else
                accelFactor = 1 - Mathf.Pow(Mathf.Clamp01(-localKPH.z / maxSpeedReverse), accellerationFalloff);
        }

        float maxAcc;

        if (Mathf.Sign(accelerate) == Mathf.Sign(localVelocity.z))
            maxAcc = maxAcceleration * accelFactor;
        else
            maxAcc = maxBrake;

        //acceleration force is added after checking ground

        for (int ct = 0; ct < frontWheels.Length; ct++)
        {
            frontWheels[ct].transform.localRotation = Quaternion.Euler(0, Mathf.Clamp(steer,-1,1) * maxSteeringAngle * getSteerFactor(Mathf.Abs(localKPH.z)), 0);
        }

        Vector3[] forces = new Vector3[wheels.Length];

        float ground = 0;

        for(int ct = 0; ct < wheels.Length; ct++)
        {
            forces[ct] = wheels[ct].GetImpulseForce();
            ground += wheels[ct].contact;
        }

        for (int ct = 0; ct < wheels.Length; ct++)
        {
            _rigidbody.AddForceAtPosition(forces[ct], wheels[ct].transform.position, ForceMode.Impulse);
        }

        //acceleration force
        _rigidbody.AddForce(Mathf.Clamp01(ground/4) * transform.forward * maxAcc * accelerate * Time.fixedDeltaTime);
    }

    float getSteerFactor(float localKPH)
    {
        if (steeringFalloff == false)
            return 1;
        float percentMaxSpeed = Mathf.Abs(localKPH)/ maxSpeed;
        return -0.5f * Mathf.Cos(1 / (Mathf.Pow(percentMaxSpeed,2) + (1 / Mathf.PI))) + 0.5f; //power controls falloff
    }

}
