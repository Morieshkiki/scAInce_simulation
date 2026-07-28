using Numena.SC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DUI_GazeOverlay : MonoBehaviour
{
    [SerializeField] GameObject gazeCircle;
    [SerializeField] Text debugText;
    [SerializeField] string dataIDX = "Headset.GazeDir.X";
    [SerializeField] string dataIDY = "Headset.GazeDir.Y";
    [SerializeField] string dataIDDilation = "Headset.PupilDilation";
    //[SerializeField] Camera cam;
    //[SerializeField] Transform headCenter;
    //[SerializeField] Transform stickTest;
    [SerializeField] RectTransform screenRect;//used as workaround to access screen dimensions

    private void OnEnable()
    {
    }

    /// <summary>
    /// Todo: wait for bug fix: https://issuetracker.unity3d.com/issues/xr-sdk-cameraworldtoscreenpoint-returns-offset-coordinates
    /// This is a temporary workaround using hardcoded values for HP Reverb G2 & QHD 16x9 Display.
    /// Currently it is not possible to convert a direction into screen coordinates on the main display when a XR camera is active.
    /// </summary>
    Vector2 DirectionToScreenPoint(Vector3 dir)
    {
        if (dir.z == 0)
            return new Vector2(0, 0);
        Vector3 d = dir;
        d.x /= d.z;
        d.y /= d.z;
        d.z = 1;
        Vector2 output = new Vector2(Mathf.InverseLerp(-1.03856f, 0.60251f, d.x), Mathf.InverseLerp(-0.528f,0.4f,d.y));
        return output;
    }

    private void Update()
    {
        bool xAvailable = DC_Manager.TryGetLatestCachedData(dataIDX, out DataEntry entryX);
        bool yAvailable = DC_Manager.TryGetLatestCachedData(dataIDY, out DataEntry entryY);
        bool dilationAvailable = DC_Manager.TryGetLatestCachedData(dataIDDilation, out DataEntry entryD);

        if (xAvailable && yAvailable/* && cam != null*/)
        {
            if(gazeCircle.activeSelf == false)
            gazeCircle.SetActive(true);
            Vector3 reconstructedDirection = new Vector3(entryX.data, entryY.data, 1);
            reconstructedDirection.z = Mathf.Sqrt(1-Mathf.Pow(entryX.data,2) + Mathf.Pow(entryY.data,2));
            //stickTest.rotation = Quaternion.LookRotation(headCenter.TransformDirection(reconstructedDirection));
            //Debug.Log(reconstructedDirection);

            Vector2 screenVector = Vector2.Scale(DirectionToScreenPoint(reconstructedDirection), screenRect.sizeDelta) ;// cam.WorldToScreenPoint(cam.transform.TransformPoint(reconstructedDirection));
            RectTransform rt = gazeCircle.transform as RectTransform;
            rt.anchoredPosition = screenVector;

            string debugString = "Eye Tracking:\r\n" +
                "Vector:\t\t\t\t({0}|{1}|{2})\r\n" +
                "Pupil Dilation (Left):\t\t{3} mm";
            debugString = string.Format(debugString, entryX.data, entryY.data, 0, entryD.data);
            debugText.text = debugString;

        }
        else
        {
            if (gazeCircle.activeSelf)
                gazeCircle.SetActive(false);
            debugText.text = "Eye tracking data unavailable";
        }
    }

    private void OnDisable()
    {
    }
}
