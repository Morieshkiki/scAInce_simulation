using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class TUD_InputManager : MonoBehaviour
{
    static TUD_InputManager main;

    const string INPUT_MODE_ID = "TUD_InputMode";
    public enum InputMode {None = -1, Teleport = 0, Treadmill = 1, Vehicle = 2, Bike = 3}

    InputMode _currentMode = InputMode.Teleport;
    public static InputMode currentInputMode => main._currentMode;

    /// <summary> When the active input mode has changed. Sends the previous input mode.</summary>
    public static Action<InputMode> onInputModeChanged;

    private void Awake()
    {
        main = this;
        int loadedMode = PlayerPrefs.GetInt(INPUT_MODE_ID, 0);
        InputMode lastMode = _currentMode;
        _currentMode = (InputMode)loadedMode;
        if (_currentMode != lastMode)
            onInputModeChanged?.Invoke(lastMode);
    }

    public static void ChangeInputMode(InputMode mode)
    {
        if (main._currentMode == mode)
            return;

        InputMode lastMode = main._currentMode;
        main._currentMode = mode;
        PlayerPrefs.SetInt(INPUT_MODE_ID, (int)main._currentMode);
        PlayerPrefs.Save();
        onInputModeChanged?.Invoke(lastMode);
    }
}
