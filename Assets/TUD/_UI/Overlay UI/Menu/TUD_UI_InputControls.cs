using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TUD_UI_InputControls : MonoBehaviour
{
    [SerializeField] ControlBtnInfo[] controlButtons;

    [System.Serializable]
    struct ControlBtnInfo
    {
        public UIToggleButton btn;
        public TUD_InputManager.InputMode mode;
    }

    private void OnEnable()
    {
        foreach (ControlBtnInfo info in controlButtons)
        {
            if (info.btn == null)
                continue;
            info.btn.onClick += OnControlButtonClicked;
        }

        TUD_InputManager.onInputModeChanged += OnInputModeChanged;
        UpdateButtons();
    }

    private void OnDisable()
    {
        foreach (ControlBtnInfo info in controlButtons)
        {
            if (info.btn == null)
                continue;
            info.btn.onClick -= OnControlButtonClicked;
        }

        TUD_InputManager.onInputModeChanged -= OnInputModeChanged;
    }

    void OnControlButtonClicked(UIButton btn)
    {
        int index = -1;
        for (int ct = 0; ct < controlButtons.Length; ct++)
        {
            if (controlButtons[ct].btn == btn)
            {
                index = ct;
                break;
            }
        }
        TUD_InputManager.ChangeInputMode(controlButtons[index].mode);
    }

    void OnInputModeChanged(TUD_InputManager.InputMode lastMode)
    {
        UpdateButtons();
    }

    void UpdateButtons()
    {
        foreach(ControlBtnInfo info in controlButtons)
        {
            if (info.btn == null)
                continue;
            info.btn.state = info.mode == TUD_InputManager.currentInputMode;
        }
    }
}
