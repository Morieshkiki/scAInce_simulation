using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Numena.SC;

public class DC_RigDataCollector : DC_DataCollector
{
    public enum UpdateMode {Update, LateUpdate, FixedUpdate, Interval}
    [Tooltip("Define when to collect the data.\r\n" +
        "Update: Each frame (undefined order)\r\n" +
        "LateUpdate: At end of each frame\r\nFixedUpdate: Each physics frame (undefined order)\r\n" +
        "Interval: every X seconds (at end of current frame)\r\n" +
        "\r\n" +
        "Maximum frequency is the applications current framerate.")]
    public UpdateMode updateMode = UpdateMode.LateUpdate;

    [Tooltip("Used only if updateMode is set to interval. Interval in seconds.")]
    public float interval = 1;

    float lastSampleTime = -1; //Time.time; -1 = no last sample

    public override string[] dataIDs {
        get {
            return new string[]{
            settings.headPosID + ".X",
            settings.headPosID + ".Y",
            settings.headPosID + ".Z",
            settings.headFwdID + ".X",
            settings.headFwdID + ".Y",
            settings.headFwdID + ".Z",
            settings.headUpID + ".X",
            settings.headUpID + ".Y",
            settings.headUpID + ".Z",

            settings.handLPosID + ".X",
            settings.handLPosID + ".Y",
            settings.handLPosID + ".Z",
            settings.handRPosID + ".X",
            settings.handRPosID + ".Y",
            settings.handRPosID + ".Z",
            settings.handLFwdId + ".X",
            settings.handLFwdId + ".Y",
            settings.handLFwdId + ".Z",
            settings.handRFwdID + ".X",
            settings.handRFwdID + ".Y",
            settings.handRFwdID + ".Z",
            settings.handLUpID + ".X",
            settings.handLUpID + ".Y",
            settings.handLUpID + ".Z",
            settings.handRUpID + ".X",
            settings.handRUpID + ".Y",
            settings.handRUpID + ".Z"
            };

        }
    }

    [System.Serializable] struct Settings
    {
        public bool headPosition, headForward, headUp;
        public bool handsPosition, handsForward, handsUp;
        public string headPosID, headFwdID, headUpID;
        public string handLPosID, handRPosID, handLFwdId, handRFwdID, handLUpID, handRUpID;
    }
    [SerializeField] Settings settings;

    private void Update()
    {
        if (updateMode != UpdateMode.Update)
            return;

        CollectData();
    }

    private void LateUpdate()
    {
        if (!(updateMode == UpdateMode.LateUpdate || updateMode == UpdateMode.Interval) )
            return;

        if (updateMode == UpdateMode.LateUpdate) {
            CollectData();
            lastSampleTime = Time.time;
            return;
        }
        else if (updateMode == UpdateMode.Interval)
        {
            if (interval <= 0)
            {
                Debug.LogError(nameof(DC_RigDataCollector) + ": Update interval cannot be <= 0");
                CollectData();
                return;
            }
            if (lastSampleTime == -1 || Time.time - lastSampleTime >= interval)
            {
                CollectData();
                while(lastSampleTime + interval < Time.time) //todo: check if safe
                    lastSampleTime += interval;//this makes sure timing errors (due to framerate) do not accumulate
                return;
            }
        }
    }

    private void FixedUpdate()
    {
        if (updateMode != UpdateMode.FixedUpdate)
            return;

        CollectData();
    }

    void CollectData()
    {
        if (settings.headPosition)
        {
            Vector3 headPos = SC_VRRig.mainCamera.transform.position;
            DC_Manager.AddData(settings.headPosID + ".X", headPos.x);
            DC_Manager.AddData(settings.headPosID + ".Y", headPos.y);
            DC_Manager.AddData(settings.headPosID + ".Z", headPos.z);
        }
        if (settings.headForward)
        {
            Vector3 headForward = SC_VRRig.mainCamera.transform.forward;
            DC_Manager.AddData(settings.headFwdID + ".X", headForward.x);
            DC_Manager.AddData(settings.headFwdID + ".Y", headForward.y);
            DC_Manager.AddData(settings.headFwdID + ".Z", headForward.z);
        }
        if (settings.headUp)
        {
            Vector3 headUp = SC_VRRig.mainCamera.transform.up;
            DC_Manager.AddData(settings.headUpID + ".X", headUp.x);
            DC_Manager.AddData(settings.headUpID + ".Y", headUp.y);
            DC_Manager.AddData(settings.headUpID + ".Z", headUp.z);
        }

        if (settings.handsPosition)
        {
            Vector3 handLPos = SC_VRRig.leftController.transform.position;
            DC_Manager.AddData(settings.handLPosID + ".X", handLPos.x);
            DC_Manager.AddData(settings.handLPosID + ".Y", handLPos.y);
            DC_Manager.AddData(settings.handLPosID + ".Z", handLPos.z);

            Vector3 handRPos = SC_VRRig.rightController.transform.position;
            DC_Manager.AddData(settings.handRPosID + ".X", handRPos.x);
            DC_Manager.AddData(settings.handRPosID + ".Y", handRPos.y);
            DC_Manager.AddData(settings.handRPosID + ".Z", handRPos.z);
        }
        if (settings.handsForward)
        {
            Vector3 handLFwd = SC_VRRig.leftController.transform.forward;
            DC_Manager.AddData(settings.handLFwdId + ".X", handLFwd.x);
            DC_Manager.AddData(settings.handLFwdId + ".Y", handLFwd.y);
            DC_Manager.AddData(settings.handLFwdId + ".Z", handLFwd.z);

            Vector3 handRFwd = SC_VRRig.rightController.transform.forward;
            DC_Manager.AddData(settings.handRFwdID + ".X", handRFwd.x);
            DC_Manager.AddData(settings.handRFwdID + ".Y", handRFwd.y);
            DC_Manager.AddData(settings.handRFwdID + ".Z", handRFwd.z);
        }
        if (settings.handsUp)
        {
            Vector3 handLUp = SC_VRRig.leftController.transform.up;
            DC_Manager.AddData(settings.handLUpID + ".X", handLUp.x);
            DC_Manager.AddData(settings.handLUpID + ".Y", handLUp.y);
            DC_Manager.AddData(settings.handLUpID + ".Z", handLUp.z);

            Vector3 handRUp = SC_VRRig.rightController.transform.up;
            DC_Manager.AddData(settings.handRUpID + ".X", handRUp.x);
            DC_Manager.AddData(settings.handRUpID + ".Y", handRUp.y);
            DC_Manager.AddData(settings.handRUpID + ".Z", handRUp.z);
        }
        //onCollect?.Invoke(this);
    }
}
