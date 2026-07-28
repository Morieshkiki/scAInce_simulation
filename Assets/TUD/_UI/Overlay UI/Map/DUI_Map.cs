using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DUI_Map : MonoBehaviour
{
    public Vector3 worldMin, worldMax;
    public Vector3 mapMin, mapMax;

    [SerializeField] Text positionText;

    public Vector3 WorldToMapPoint(Vector3 worldPos)
    {
        Vector3 f = new Vector3(Mathf.InverseLerp(worldMin.x,worldMax.x,worldPos.x), Mathf.InverseLerp(worldMin.z, worldMax.z, worldPos.z), Mathf.InverseLerp(worldMin.y, worldMax.y, worldPos.y));
        Vector3 p = new Vector3(Mathf.Lerp(mapMin.x, mapMax.x, f.x), Mathf.Lerp(mapMin.y, mapMax.y, f.y), Mathf.Lerp(mapMin.z, mapMax.z, f.z));
        return p;
    }

    public Vector3 MapToWorldPoint(Vector3 mapPoint)
    {
        Vector3 f = new Vector3(Mathf.InverseLerp(mapMin.x, mapMax.x, mapPoint.x), Mathf.InverseLerp(mapMin.z, mapMax.z, mapPoint.z), Mathf.InverseLerp(mapMin.y, mapMax.y, mapPoint.y));
        Vector3 p = new Vector3(Mathf.Lerp(worldMin.x, worldMax.x, f.x), Mathf.Lerp(worldMin.y, worldMax.y, f.y), Mathf.Lerp(worldMin.z, worldMax.z, f.z));
        return p;
    }

    private void Update()
    {
        Vector3 position = Vector3.zero;
        if(TUD_PlayerManager.activePlayer != null)
            position = TUD_PlayerManager.activePlayer.position;
        positionText.text = "x: " + position.x.ToString("0.00") + "\r\n"+
            "y: " + position.x.ToString("0.00") + "\r\n"+
            "z: " + position.x.ToString("0.00");
    }
}
