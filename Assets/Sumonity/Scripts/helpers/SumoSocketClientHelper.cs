using UnityEngine;
using System.Collections.Generic;
using tum_car_controller;

namespace tumvt.sumounity
{
    public static class SumoSocketClientHelper
    {
        public static void RemoveAllActorsIfSumoInBackground(
            bool runInBackground,
            VehicleSetup vehicleSetup,
            VehicleToggles vehicleToggles)
        {
            if(!runInBackground) return;

            if(vehicleSetup.vehDict.Count <= 0) return;

            List<string> keysToRemove = new List<string>();
            foreach (KeyValuePair<string, GameObject> kvp in vehicleSetup.vehDict)
            {  
                if(kvp.Value.gameObject.CompareTag("Bus") && !vehicleToggles.busEnable)
                {
                    Debug.LogWarning("Bus is not destroyed in background mode!");
                }
                else if(kvp.Value.gameObject.CompareTag("Ego"))
                {
                    Debug.LogWarning("Ego is not destroyed in background mode!");
                }
                else if(kvp.Value.gameObject.CompareTag("Person") && !vehicleToggles.pedestrianEnable)
                {
                    Debug.LogWarning("Person is not destroyed");
                }
                else
                {
                    Debug.Log("Destroyed Vehicle = " + kvp.Value.gameObject.name);   
                    Object.Destroy(kvp.Value.gameObject);
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (string key in keysToRemove)
            {
                vehicleSetup.vehDict.Remove(key);
            }
        }

        public static void CheckForNewVehiclesAndAdd(
            SumoSimulationStepInfo stepInfo,
            bool runInBackground,
            VehicleSetup vehicleSetup,
            SimulatorVehicleInfo simulatorVehicleInfo,
            OptimizationSettings optimizationSettings,
            VehicleToggles vehicleToggles,
            bool isTeleportOnlyMode)
        {
            if(runInBackground) return;

            foreach (SerializableVehicle serVehicle in stepInfo.vehicleList)
            {   
                string vehId = serVehicle.id;
                if (vehicleSetup.vehDict.ContainsKey(vehId)) continue;
                if (vehId == simulatorVehicleInfo.egoVehicleId) continue;
                // Crashed & detached: SUMO still streams this id, but the wreck lives on in the
                // scene independently. Do NOT respawn it (that loop multiplied the vehicle).
                if (VehicleCrashController.DetachedIds.Contains(vehId)) continue;
                // Durable guard: never respawn an id whose wreck is still in the scene, even
                // after DetachedIds was pruned (which happens once we remove the pulled-over
                // wreck's SUMO twin so traffic can flow). Without this the id could respawn
                // as a driven duplicate parked next to its own wreck.
                if (VehicleCrashController.ActiveWrecks.ContainsKey(vehId)) continue;

                bool isInRadius = true;
                if(optimizationSettings.useEgoRadius)
                {
                    isInRadius = Vector3.Distance(
                        new Vector3(serVehicle.positionX, 0, serVehicle.positionY), 
                        simulatorVehicleInfo.egoVehicle.position) <= optimizationSettings.egoRadius;
                }

                if (!isInRadius) continue;

                SpawnNewVehicle(serVehicle, vehicleSetup, vehicleToggles, isTeleportOnlyMode);
            }
        }

        private static void SpawnNewVehicle(
            SerializableVehicle serVehicle, 
            VehicleSetup vehicleSetup,
            VehicleToggles vehicleToggles,
            bool isTeleportOnlyMode)
        {
            GameObject vehObj;
            float specificHeightOfCoordianteFrame;

            (vehObj, specificHeightOfCoordianteFrame) = GetVehicleObjectAndHeight(
                serVehicle.vehicleType, 
                vehicleSetup, 
                vehicleToggles);

            if (vehObj == null) return;

            Vector3 pos = new Vector3(
                serVehicle.positionX,
                // In teleport mode the rigidbody is kinematic and never falls, so the
                // old "+2f drop onto the road" slack would leave cars floating 2 m up.
                // Spawn directly at the intended ground offset instead.
                specificHeightOfCoordianteFrame,
                serVehicle.positionY);
            Quaternion rot = Quaternion.Euler(0, serVehicle.rotation + 180f, 0);
            
            GameObject veh = Object.Instantiate(vehObj, pos, rot);
            veh.name = $"{serVehicle.id}-{serVehicle.vehicleType}-{vehObj.name}";

            // Set the tag to "Vehicle" for all vehicles
            veh.tag = "Vehicle";

            // TODO: fix implementation to work with general vehicle controllers
            // CarController carController = veh.GetComponent<CarController>();
            // if (carController != null)
            // {
            //     carController.SetTeleportOnlyMode(isTeleportOnlyMode);
            // }

            ApplyRandomColor(veh);
            SetupVehicleController(veh, serVehicle.id, specificHeightOfCoordianteFrame, GetYawOffset(serVehicle.vehicleType));

            vehicleSetup.vehDict.Add(serVehicle.id, veh);

            // Make the vehicle crashable: body collider, mass, denter + crash controller.
            // Done AFTER the dict Add so the crash controller can later unregister itself.
            SetupCrashSystem(veh, serVehicle.id, serVehicle.vehicleType, vehicleSetup);
        }

        private static (GameObject obj, float height) GetVehicleObjectAndHeight(
            string vehicleType, 
            VehicleSetup vehicleSetup,
            VehicleToggles vehicleToggles)
        {
            var vc = vehicleSetup.vehicleCompositions;
            switch (vehicleType)
            {
                case "passenger":
                    return PickFrom(vc.PassengerCars, 0f, vc);
                case "bicycle":
                    return PickFrom(vc.Bicycles, 0f, vc);
                case "pedestrian" when !vehicleToggles.pedestrianEnable:
                    return PickFrom(vc.Persons, 1.1f, vc);
                case "bus":
                    return PickFrom(vc.Busses, 0f, vc);
                case "tram":
                    // raised so the tall tram model sits ON the road (tune if needed)
                    return PickFrom(vc.Trams, 1.5f, vc);
                case "taxi":
                    return PickFrom(vc.Taxis, 1f, vc);
                default:
                    Debug.LogWarning($"Vehicle Type '{vehicleType}' not mapped; using a passenger car.");
                    return PickFrom(vc.PassengerCars, 0f, vc);
            }
        }

        // Pick a random prefab from `list`. If that list is empty/unassigned (e.g.
        // Trams not yet populated in the Inspector), fall back to a passenger car so
        // the vehicle is still VISIBLE rather than silently dropped (return null).
        private static (GameObject obj, float height) PickFrom(
            List<GameObject> list, float height, VehicleCompositionsScriptableObject vc)
        {
            if (list != null && list.Count > 0)
                return (list[Random.Range(0, list.Count)], height);
            if (vc.PassengerCars != null && vc.PassengerCars.Count > 0)
                return (vc.PassengerCars[Random.Range(0, vc.PassengerCars.Count)], 0f);
            return (null, 0f);
        }

        private static void ApplyRandomColor(GameObject vehicle)
        {
            Color vehcolor = Random.ColorHSV();
            foreach (Transform child in vehicle.transform)
            {
                GameObject bodyComponent = child.Find("Body")?.gameObject;
                if (bodyComponent == null) continue;

                foreach (Transform child2 in child)
                {
                    MeshRenderer meshRenderer = child2.GetComponent<MeshRenderer>();
                    if (meshRenderer == null || meshRenderer.materials.Length == 0) continue;

                    foreach (Material material in meshRenderer.materials)
                    {
                        if (material.name == child.name + "_Body (Instance)")
                        {
                            material.color = vehcolor;
                        }
                    }
                }
            }
        }

        private static void SetupVehicleController(GameObject vehicle, string vehicleId, float heightOffset, float yawOffset)
        {
            // Disable the heavy physics CarController if the prefab has one (the
            // passenger cars do) so it does not fight the smooth SUMO mover. We
            // drive EVERY vehicle uniformly with SumoTeleportController, which also
            // means tram/bus prefabs that have no controller now move correctly
            // (previously they logged "Vehicle Controller not found!" and sat still).
            // Grab the wheel references the prefab author wired into CarController so
            // the teleport mover can roll them, THEN disable CarController so it does
            // not fight the mover.
            CarController carCtrl = vehicle.GetComponent<CarController>();
            List<Transform> wheels = new List<Transform>();
            if (carCtrl != null)
            {
                if (carCtrl.wheelFL != null) wheels.Add(carCtrl.wheelFL.transform);
                if (carCtrl.wheelFR != null) wheels.Add(carCtrl.wheelFR.transform);
                if (carCtrl.wheelRL != null) wheels.Add(carCtrl.wheelRL.transform);
                if (carCtrl.wheelRR != null) wheels.Add(carCtrl.wheelRR.transform);
                carCtrl.enabled = false;
            }

            SumoTeleportController mover = vehicle.GetComponent<SumoTeleportController>();
            if (mover == null) mover = vehicle.AddComponent<SumoTeleportController>();
            mover.id = vehicleId;
            mover.heightOffset = heightOffset;
            mover.yawOffset = yawOffset;
            mover.wheels = wheels.ToArray();
        }

        // ----------------------------------------------------------------------------
        // Crash system: give every spawned vehicle a body collider + Rigidbody (kept
        // kinematic while SUMO drives it) and the MeshDenter/VehicleCrashController pair.
        // The body BoxCollider is essential: the prefabs ship with only wheel sphere
        // colliders, so without this a player would hit only the wheels, not the body.
        // ----------------------------------------------------------------------------
        private static void SetupCrashSystem(
            GameObject vehicle, string vehicleId, string vehicleType, VehicleSetup vehicleSetup)
        {
            Rigidbody rb = vehicle.GetComponent<Rigidbody>();
            if (rb == null) rb = vehicle.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            // Kinematic bodies need speculative CCD to be hit by the fast ContinuousDynamic
            // player car (otherwise the player can tunnel straight through).
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.mass = MassForType(vehicleType);

            AddBodyCollider(vehicle);

            MeshDenter denter = vehicle.GetComponent<MeshDenter>();
            if (denter == null) denter = vehicle.AddComponent<MeshDenter>();
            denter.Initialize();

            VehicleCrashController crash = vehicle.GetComponent<VehicleCrashController>();
            if (crash == null) crash = vehicle.AddComponent<VehicleCrashController>();
            crash.Configure(vehicleId, MassForType(vehicleType), vehicleSetup.vehDict, vehicleType);
        }

        private static float MassForType(string vehicleType)
        {
            switch (vehicleType)
            {
                case "tram":       return 40000f;
                case "bus":        return 12000f;
                case "taxi":       return 1300f;
                case "bicycle":    return 90f;
                case "pedestrian": return 80f;
                default:           return 1200f; // passenger car
            }
        }

        // Add a single BoxCollider on the vehicle root sized to enclose all of its meshes,
        // computed in the root's LOCAL space so it stays correct under any spawn rotation.
        private static void AddBodyCollider(GameObject vehicle)
        {
            MeshFilter[] mfs = vehicle.GetComponentsInChildren<MeshFilter>(true);
            bool has = false;
            Vector3 min = Vector3.zero, max = Vector3.zero;

            foreach (MeshFilter mf in mfs)
            {
                if (mf.sharedMesh == null) continue;
                Bounds lb = mf.sharedMesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 c = lb.center + Vector3.Scale(lb.extents, new Vector3(
                        (corner & 1) == 0 ? -1f : 1f,
                        (corner & 2) == 0 ? -1f : 1f,
                        (corner & 4) == 0 ? -1f : 1f));
                    Vector3 local = vehicle.transform.InverseTransformPoint(mf.transform.TransformPoint(c));
                    if (!has) { min = max = local; has = true; }
                    else { min = Vector3.Min(min, local); max = Vector3.Max(max, local); }
                }
            }
            if (!has) return;

            BoxCollider bc = vehicle.AddComponent<BoxCollider>();
            bc.center = (min + max) * 0.5f;
            bc.size = (max - min);
        }

        // Per-type yaw correction in case a model's forward axis is not +Z.
        // Adjust tram/bus here if they ever render sideways (try 90 or -90).
        private static float GetYawOffset(string vehicleType)
        {
            switch (vehicleType)
            {
                // Tram model's forward axis renders 90° off from SUMO's heading, so
                // it appeared perpendicular to the track. If it now faces backwards,
                // change this to -90 (equivalently 270).
                case "tram": return 90f;
                case "bus":  return 0f;
                default:     return 0f;
            }
        }

        public static void RemoveNonExistentActors(
            VehicleSetup vehicleSetup,
            SumoSimulationStepInfo stepInfo)
        {
            try
            {
                var vehiclesToRemove = new List<string>();
                foreach (var kvp in vehicleSetup.vehDict)
                {
                    if (!stepInfo.vehicleList.Exists(v => v.id == kvp.Key))
                    {
                        Object.Destroy(kvp.Value.gameObject);
                        vehiclesToRemove.Add(kvp.Key);
                    }
                }

                foreach (string key in vehiclesToRemove)
                {
                    vehicleSetup.vehDict.Remove(key);
                }

                // Prune detached-crash ids that SUMO no longer reports, so the set does not
                // grow unbounded and a recycled id could legitimately spawn again later.
                if (VehicleCrashController.DetachedIds.Count > 0)
                {
                    var detachedToRemove = new List<string>();
                    foreach (var id in VehicleCrashController.DetachedIds)
                    {
                        if (!stepInfo.vehicleList.Exists(v => v.id == id))
                            detachedToRemove.Add(id);
                    }
                    foreach (var id in detachedToRemove)
                        VehicleCrashController.DetachedIds.Remove(id);
                }

                // Same pruning for the crash-phase reports streamed to the Python host:
                // once SUMO stops reporting an id (because the host removed the twin after
                // "cleared"/tow-away) the phase entry has served its purpose.
                if (VehicleCrashController.CrashPhases.Count > 0)
                {
                    var phasesToRemove = new List<string>();
                    foreach (var id in VehicleCrashController.CrashPhases.Keys)
                    {
                        if (!stepInfo.vehicleList.Exists(v => v.id == id))
                            phasesToRemove.Add(id);
                    }
                    foreach (var id in phasesToRemove)
                        VehicleCrashController.CrashPhases.Remove(id);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"An exception occurred: {e.Message}");
            }
        }
    }
} 