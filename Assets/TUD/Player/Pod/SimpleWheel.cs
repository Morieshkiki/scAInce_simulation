using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleWheel : MonoBehaviour
{
    public float grip = 1;

    [Header("Wheel")]
    public float radius = 0.25f;
    public float width = 0.18f;
    public float sideFriction = 0.25f;
    [Header("Suspension")]
    public float travel = 0.1f;
    public float spring = 1;
    public float damper = 1;

    Vector3 localVelocity;
    float lastHitDistance;

    Rigidbody _parentRigidbody;
    Rigidbody parentRigidbody { get { if (_parentRigidbody == null) _parentRigidbody = GetComponentInParent<Rigidbody>(); return _parentRigidbody; } }

    float _contact = 0;
    public float contact => _contact;


    public Vector3 GetImpulseForce()
    {
        Vector3 finalImpulseForce = new Vector3();

        float rayLength = radius * 2 + travel;
        bool impact = Physics.BoxCast(transform.position + Vector3.up * (travel + radius), new Vector3(width*0.5f, 0.01f, radius), -transform.up, out RaycastHit hit, transform.rotation, rayLength);
        if (impact)
        {
            Vector3 hitPoint = transform.position - transform.up * hit.distance;
            Vector3 localVel = parentRigidbody.GetPointVelocity(hitPoint);
            localVel = transform.InverseTransformDirection(localVel);

            float diff = hit.distance - lastHitDistance;

            float springForce = Mathf.Clamp(spring * (rayLength - hit.distance), 0, Mathf.Infinity);
            float damperForce = -Mathf.Clamp(diff / Time.fixedDeltaTime * damper, 0, Mathf.Infinity);
            float sideForce = -localVel.x * sideFriction * (rayLength - hit.distance);

            finalImpulseForce = Time.fixedDeltaTime * parentRigidbody.mass * (hit.normal * (springForce + damperForce) + transform.right * sideForce);

            lastHitDistance = hit.distance;
        }
        else
        {
            lastHitDistance = rayLength;//max distance
        }

        Debug.DrawLine(transform.position + transform.up * (radius + travel), transform.position + transform.up * (radius + travel) - transform.up * lastHitDistance);

        _contact = Mathf.Clamp01((rayLength - lastHitDistance) / travel * 2);

        return finalImpulseForce;
    }
}
