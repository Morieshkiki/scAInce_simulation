using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Numena.SC;

public class DC_DemoScript : MonoBehaviour
{
    [SerializeField] float maxRayLength = 15;

    Vector3 lastGazePoint = new Vector3();
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Ray camRay = new Ray(SC_VRRig.mainCamera.transform.position, SC_VRRig.mainCamera.transform.forward);

        Debug.DrawRay(camRay.origin, camRay.direction * 0.1f, Color.blue, 20, true);
        Debug.DrawRay(SC_VRRig.leftController.transform.position, SC_VRRig.leftController.transform.forward * 0.1f, Color.red, 20, true);
        Debug.DrawRay(SC_VRRig.rightController.transform.position, SC_VRRig.rightController.transform.forward * 0.1f, Color.green, 20, true);

        if(Physics.Raycast(camRay, out RaycastHit hit, maxRayLength))
        {
            if ((hit.point - lastGazePoint).magnitude >= 0.5f)
            {
                Debug.DrawRay(hit.point, (camRay.origin - hit.point).normalized * 0.2f, new Color(0.1f, 1f, 0.1f), 20, true);

                Debug.DrawLine(lastGazePoint, hit.point, new Color(0.1f, 1f, 0.1f), 20, true);
                DrawSquare(hit.point, camRay.direction, 0.25f);
                lastGazePoint = hit.point;
            }
        }
    }

    void DrawSquare(Vector3 position, Vector3 normal, float radius)
    {
        Color col = new Color(0.1f, Random.Range(0.5f,1f), 0.1f);

        Vector3 right = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) >= 0.99f? Vector3.right : Vector3.Cross(normal, Vector3.up).normalized;
        Vector3 up = Vector3.Cross(right, normal).normalized;

        Vector3 v0 = position + up * radius;
        Vector3 v1 = position + right * radius;
        Vector3 v2 = position - up * radius;
        Vector3 v3 = position - right * radius;

        Debug.DrawLine(v0, v1, col, 20, true);
        Debug.DrawLine(v1, v2, col, 20, true);
        Debug.DrawLine(v2, v3, col, 20, true);
        Debug.DrawLine(v3, v0, col, 20, true);
    }
}
