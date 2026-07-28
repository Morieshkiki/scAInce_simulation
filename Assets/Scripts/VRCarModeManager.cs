using UnityEngine;
#if ENABLE_VR
using UnityEngine.XR.Management;
#endif

// --- This section was adjusted using AI assistance ---
/// <summary>
/// Car-only bootstrap.
///
/// The VR (Cyberith treadmill) path and the start-up mode-select menu have been
/// removed: on Play the scene goes STRAIGHT into keyboard-driven car mode. The
/// VR rig is deactivated and any XR subsystems are stopped so no headset view is
/// ever started.
///
/// The public fields (vrRig, carRoot, carCamera) are kept so the existing scene
/// wiring on the ModeManager object stays valid; only carRoot/carCamera are
/// required now. vrRig is optional and, if assigned, is simply switched off.
///
/// Inspector setup:
///   • carRoot    – the PlayerCar GameObject (has PlayerCarController)
///   • carCamera  – the Camera used while driving (PlayerCar's child camera)
///   • vrRig      – (optional) the old TreadmillPlayer rig; deactivated on start
/// </summary>
public class VRCarModeManager : MonoBehaviour
{
    [Header("Car")]
    [Tooltip("GameObject with PlayerCarController on it.")]
    public GameObject carRoot;
    [Tooltip("Camera used while driving the car (PlayerCar's child camera).")]
    public Camera carCamera;

    [Header("Legacy VR rig (deactivated on start)")]
    [Tooltip("Optional. The old TreadmillPlayer GameObject; switched off if assigned.")]
    public GameObject vrRig;

    void Start()
    {
        // Make sure no leftover VR rig or XR view is live.
        if (vrRig != null) vrRig.SetActive(false);
        StopXR();

        // Go straight to car mode: unfreeze, enable the car + its camera.
        Time.timeScale = 1f;

        if (carCamera != null) carCamera.enabled = true;
        SetCarActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("[VRCarModeManager] Car mode: W/S/A/D to drive.");
    }

    void SetCarActive(bool on)
    {
        if (carRoot == null) return;
        if (!carRoot.activeSelf) carRoot.SetActive(true);
        var controller = carRoot.GetComponent<PlayerCarController>();
        if (controller != null) controller.enabled = on;
    }

    // Stop any XR subsystems that XR Plug-in Management may have started on
    // startup, so the flat car camera owns the screen. Safe no-op if XR is
    // not initialized or the package is absent.
    void StopXR()
    {
#if ENABLE_VR
        var settings = XRGeneralSettings.Instance;
        var mgr = settings != null ? settings.Manager : null;
        if (mgr != null && mgr.isInitializationComplete && mgr.activeLoader != null)
            mgr.StopSubsystems();
#endif
    }
}
