using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Threading;

namespace tumvt.sumounity
{
    public interface IVehicleController
    {
        string id { get; set; }
    }

    public class SumoSocketClient : MonoBehaviour
    {

        [System.Serializable]
        private class DebugData 
        {
            [Tooltip("Message received from/to SUMO")]
            public string messageReceived;
            public string messageToSend;
            public bool showDebugGizmoSimulatorEgo = false;
        }
        [Header("Socket Communication")]
        [SerializeField] private DebugData debugData;


        [Header("Vehicle Setup")]
        [SerializeField] private VehicleSetup vehicleSetup;
        
        [Header("Vehicle Toggles")]
        [SerializeField] private VehicleToggles vehicleToggles;
        
        [Header("Optimization Settings")]
        [SerializeField] private OptimizationSettings optimizationSettings;
        
        [Header("Simulator Vehicle Info")]
        [SerializeField] private SimulatorVehicleInfo simulatorVehicleInfo;

        [Header("Simulation Step Information")]
        [SerializeField] private SumoSimulationStepInfo _stepInfo;
        public SumoSimulationStepInfo StepInfo => _stepInfo;

        public bool SendData => simulatorVehicleInfo._sendData;
        public Transform EgoVehicle => simulatorVehicleInfo.egoVehicle;

        private SocketConnector SocketConnector;
        private float simulationStartTime;


        void Start()
        {
            SocketConnector = new SocketConnector();
            Debug.Log("Starting Client with " + SocketConnector.connectionIP + " on port " + SocketConnector.connectionPort);
            SocketConnector.Start();

            simulationStartTime = Time.time; // Record the start time of the simulation
        }

        void Update()
        {
            // Self-heal: a mid-play domain reload (script recompile while playing) wipes
            // this non-serialized field, which used to leave the client dead with a
            // NullReferenceException every frame. Recreate the connection instead.
            if (SocketConnector == null)
            {
                SocketConnector = new SocketConnector();
                SocketConnector.Start();
                return;
            }

            // ======================
            //      Receive Data
            // ======================
            debugData.messageReceived = SocketConnector.messageReceived;

            if (debugData.messageReceived != null)
            {
                DeserializeStepInfo(debugData.messageReceived);
                UpdateVehiclesDictionary();
            }        

            // ======================
            //      Send Data
            // ======================

            // Always reply with a newline-framed JSON report: the ego pose (when a driven
            // ego exists) plus the live crash list from VehicleCrashController. The Python
            // SUMO host parses the "crashes" field to stop/remove the SUMO twins of crashed
            // vehicles, so traffic queues behind accidents in SUMO exactly while the lane
            // is blocked here in Unity.
            var report = new Dictionary<string, object>();

            if (simulatorVehicleInfo._sendData)
            {
                SerializableVehicle ego = new SerializableVehicle();
                ego.id = simulatorVehicleInfo.egoVehicleId;

                Vector3 egoPos = simulatorVehicleInfo.egoVehicle.position;
                float egoRot = simulatorVehicleInfo.egoVehicle.rotation.eulerAngles.y;

                ego.positionX = egoPos.x;
                ego.positionY = egoPos.z;
                ego.rotation = egoRot;

                report["ego"] = ego;
            }

            var crashes = new List<Dictionary<string, string>>();
            foreach (var kvp in VehicleCrashController.CrashPhases)
                crashes.Add(new Dictionary<string, string> { { "id", kvp.Key }, { "phase", kvp.Value } });
            report["crashes"] = crashes;

            debugData.messageToSend = JsonConvert.SerializeObject(report) + "\n";
            SocketConnector.messageToSend = debugData.messageToSend;
        }

        public void DeserializeStepInfo(string message)
        {
            try
            {
                _stepInfo = JsonConvert.DeserializeObject<SumoSimulationStepInfo>(message);
            }
            catch (JsonException ex)
            {
                if (Time.time - simulationStartTime > 5) // Check if more than 5 seconds have passed
                {
                    Debug.LogWarning($"Json Deserialization of SumoStep failed! Exception: {ex.Message}");
                }
                _stepInfo = new SumoSimulationStepInfo();
            }
        }

        private void OnApplicationQuit()
        {
           SocketConnector.Close();
        }

        void UpdateVehiclesDictionary()
        {
            SumoSocketClientHelper.RemoveAllActorsIfSumoInBackground(
                optimizationSettings.RunSumoInBackground,
                vehicleSetup, 
                vehicleToggles);

            SumoSocketClientHelper.CheckForNewVehiclesAndAdd(
                _stepInfo,
                optimizationSettings.RunSumoInBackground,
                vehicleSetup,
                simulatorVehicleInfo,
                optimizationSettings,
                vehicleToggles,
                optimizationSettings.isTeleportOnlyMode);

            SumoSocketClientHelper.RemoveNonExistentActors(
                vehicleSetup,
                _stepInfo);
        }

        public void SetRunSumoInBackground(bool value)
        {
            optimizationSettings.RunSumoInBackground = value;
        }

        public void SetBusEnable(bool value)
        {
            vehicleToggles.busEnable = value;
        }

        public void SetPedestrianEnable(bool value)
        {
            vehicleToggles.pedestrianEnable = value;
        }

        private void OnDrawGizmos()
        {
            if (debugData.showDebugGizmoSimulatorEgo)
            {
                Gizmos.color = new Color(1, 0, 0, 0.5f);
                Gizmos.DrawSphere(simulatorVehicleInfo.egoVehicle.position, 2);
                Gizmos.DrawSphere(simulatorVehicleInfo.egoVehicle.position, optimizationSettings.egoRadius);
            }
        }
    }
}
