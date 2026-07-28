using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.Globalization;
using System.IO;
using System.Linq;

public class getParkingSpots : MonoBehaviour
{
    // Assign different prefab types in the Unity Editor
    public GameObject[] parkingSpacePrefabs;

    // Occupancy rate from 0 to 100%
    public float occupancyRate = 50f;
    public float offsetX = 342.25f;
    public float offsetZ = 273.25f;

    GameObject parkingParent;
    List<(Vector3 position, Quaternion rotation, string id)> parkingSpots = new List<(Vector3, Quaternion, string)>();

    
    void Start()
    {
        //parkingParent = GameObject.Find("Parking");
//
        //if (!parkingParent)
        //{
        //    Debug.LogError("Parking parent object not found in the scene. Make sure there's a GameObject named 'Parking'.");
        //    return;
        //}
//
        //string filePath = "Assets/3d_model/tum_main_groupA.xodr";
//
        //if (File.Exists(filePath))
        //{
        //    string xodrContent = File.ReadAllText(filePath);
        //    ParseXODR(xodrContent);
        //    SpawnParkingSpotsAndvehicles();
        //}
        //else
        //{
        //    Debug.LogError("XODR file not found at: " + filePath);
        //}
    }

    void ParseXODR(string xmlContent)
    {
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlContent);

        XmlNodeList roadNodes = xmlDoc.SelectNodes("//road");
        foreach (XmlNode roadNode in roadNodes)
        {
            XmlNode geometryNode = roadNode.SelectSingleNode("planView/geometry");
            float roadX = float.Parse(geometryNode.Attributes["x"].Value, CultureInfo.InvariantCulture);
            float roadY = float.Parse(geometryNode.Attributes["y"].Value, CultureInfo.InvariantCulture);
            float roadHdg = float.Parse(geometryNode.Attributes["hdg"].Value, CultureInfo.InvariantCulture);
            
            XmlNodeList parkingNodes = roadNode.SelectNodes(".//object[@type='parkingSpace']");
            foreach (XmlNode parkingNode in parkingNodes)
            {
                float s = float.Parse(parkingNode.Attributes["s"].Value, CultureInfo.InvariantCulture);
                float t = float.Parse(parkingNode.Attributes["t"].Value, CultureInfo.InvariantCulture);
                // float width = float.Parse(parkingNode.Attributes["width"].Value, CultureInfo.InvariantCulture);
                // float length = float.Parse(parkingNode.Attributes["length"].Value, CultureInfo.InvariantCulture);
                float parkHdg = float.Parse(parkingNode.Attributes["hdg"].Value, CultureInfo.InvariantCulture);
                string id = parkingNode.Attributes["id"].Value;

                Vector3 position = ConvertSTtoUnityCoordinates(s, t, roadX, roadY, roadHdg);
                Quaternion rotation = Quaternion.Euler(0, Mathf.Rad2Deg * (parkHdg - roadHdg), 0);
                parkingSpots.Add((position, rotation, id));
                // PlaceParkingSpace(position, size, rotation, parkingNode.Attributes["id"].Value);
            }
        }
    }
    void SpawnParkingSpotsAndvehicles()
    {
        Transform vehiclesSubParent = parkingParent.transform.Find("ParkingVehicles");
        // vehiclesSubParent.transform.position = new Vector3(342.25f, 0, 273.25f);
        // vehiclesSubParent.transform.rotation = Quaternion.Euler(0, 0f, 0);
        vehiclesSubParent.transform.parent = parkingParent.transform;

        int totalSpots = parkingSpots.Count;
        int vehiclesToSpawn = Mathf.RoundToInt(totalSpots * (occupancyRate / 100f));

        List<int> allSpotIndices = new List<int>(Enumerable.Range(0, totalSpots));
        allSpotIndices = allSpotIndices.OrderBy(x => Random.value).ToList();
        HashSet<int> vehicleSpotIndices = new HashSet<int>(allSpotIndices.Take(vehiclesToSpawn));

        foreach (int index in vehicleSpotIndices)
    {
        Vector3 localPosition = parkingSpots[index].position;
        Quaternion rotation = parkingSpots[index].rotation;

        int prefabIndex = Random.Range(0, parkingSpacePrefabs.Length);
        switch (prefabIndex)
    {
        case 1:
            localPosition.y = 0;
            break;
        case 2:
            localPosition.y = 0.15f;
            break;
        case 3:
            localPosition.y = 1.02f;
            break;
        case 6:
            localPosition.y = 0.97f;
            break;
        default:
            localPosition.y = 0.0f;
            break;
    }
        GameObject vehiclePrefab = parkingSpacePrefabs[prefabIndex];

        // Instantiate the vehicle at the origin relative to the vehiclesSubParent
        GameObject vehicleObject = Instantiate(vehiclePrefab, Vector3.zero,rotation, vehiclesSubParent.transform);


        // Set the local position to the calculated local position
        vehicleObject.transform.localPosition = localPosition;

        vehicleObject.name = $"Vehicles_{parkingSpots[index].id}";

        Transform colliderTransform = vehicleObject.transform.Find("Collider");

        if (colliderTransform != null)
        {
            colliderTransform.gameObject.SetActive(false);
        }
    }

}
    Vector3 ConvertSTtoUnityCoordinates(float s, float t, float roadX, float roadY, float hdg)
    {
        Vector3 position = new Vector3(0, 0, 0);
        position.x = roadX + s * Mathf.Cos(hdg) - t * Mathf.Sin(hdg) + offsetX;  // Use offsetX here
        position.z = roadY + s * Mathf.Sin(hdg) + t * Mathf.Cos(hdg) + offsetZ;
        position.y = 0.0f;
        return position;
    }

    // void PlaceParkingSpace(Vector3 position, Vector3 size, Quaternion rotation, string parkingID)
    // {
    //     if (parkingSpacePrefabs != null && parkingSpacePrefabs.Length > 0)
    //     {
    //         // Select a random prefab from the array
    //         GameObject selectedPrefab = parkingSpacePrefabs[Random.Range(0, parkingSpacePrefabs.Length)];
    //         GameObject parkingSpace = Instantiate(selectedPrefab, position, rotation);
    //         parkingSpace.transform.localScale = size;
    //         parkingSpace.name = "ParkingSpace_" + parkingID;
    //         parkingSpace.transform.SetParent(parkingParent.transform, false);
    //     }
    //     else
    //     {
    //         Debug.LogError("Parking space prefabs are not assigned or the array is empty.");
    //     }
    // }
    public void Starter()
    {
        parkingParent = GameObject.Find("Parking");

        if (!parkingParent)
        {
            Debug.LogError("Parking parent object not found in the scene. Make sure there's a GameObject named 'Parking'.");
            return;
        }

        string filePath = "Assets/3d_model/tum_main_groupA.xodr";

        if (File.Exists(filePath))
        {
            string xodrContent = File.ReadAllText(filePath);
            ParseXODR(xodrContent);
            SpawnParkingSpotsAndvehicles();
        }
        else
        {
            Debug.LogError("XODR file not found at: " + filePath);
        }
    }
}
