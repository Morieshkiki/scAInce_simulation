using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(getParkingSpots))]
public class getParkingSpotsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        getParkingSpots gps = (getParkingSpots)target;

        if(GUILayout.Button("Place Parked Objects"))
        {
            gps.Starter();
        }
    }
}
