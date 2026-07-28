using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VehicleGaugeCluster : MonoBehaviour
{
    [SerializeField] SimpleVehicle vehicle;

    [SerializeField] Text gearText, speedText;

    private void Update()
    {
        speedText.text = Mathf.RoundToInt(Mathf.Abs(vehicle.speed * 3.6f)).ToString();
        switch (vehicle.gear)
        {
            case -1:
                gearText.text = "R";
                break;
            case 0:
                gearText.text = "";
                break;
            case 1:
                gearText.text = "D";
                break;
        }
    }
}
