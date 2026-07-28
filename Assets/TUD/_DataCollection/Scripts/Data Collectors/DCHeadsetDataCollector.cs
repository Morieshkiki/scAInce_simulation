// HP Omnicept SDK (for HP Reverb G2 eye tracking & biometrics) is not installed.
// This file is disabled until the SDK is added. Define HP_OMNICEPT_SDK to re-enable.
#if HP_OMNICEPT_SDK

using HP.Omnicept.Messaging.Messages;
using HP.Omnicept.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DCHeadsetDataCollector : DC_DataCollector
{
    [SerializeField] string heartRateID = "Headset.HeartRate";
    [SerializeField] string pupilDilationID = "Headset.PupilDilation";
    [SerializeField] string gazeID = "Headset.Gaze";

    [SerializeField] GliaBehaviour glia;

    public override string[] dataIDs
    {
        get
        {
            return new string[]{
            heartRateID,
            pupilDilationID + ".L",
            pupilDilationID + ".R",
            gazeID + ".X",
            gazeID + ".Y"
            };
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        glia.OnEyeTracking.AddListener(OnEyeTracking);
        glia.OnHeartRate.AddListener(OnHeartRate);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        glia.OnEyeTracking.RemoveListener(OnEyeTracking);
        glia.OnHeartRate.RemoveListener(OnHeartRate);
    }

    void OnEyeTracking(EyeTracking tracking)
    {
        EyeGaze gaze = tracking.CombinedGaze;
        Vector3 normalizedGaze = new Vector3(gaze.X, gaze.Y, gaze.Z).normalized;
        DC_Manager.AddData(gazeID + ".X", -normalizedGaze.x);
        DC_Manager.AddData(gazeID + ".Y", normalizedGaze.y);
        DC_Manager.AddData(pupilDilationID + ".L", tracking.LeftEye.PupilDilation);
        DC_Manager.AddData(pupilDilationID + ".R", tracking.RightEye.PupilDilation);
    }

    void OnHeartRate(HeartRate hr)
    {
        DC_Manager.AddData(heartRateID, (float)hr.Rate);
    }
}

#endif // HP_OMNICEPT_SDK
