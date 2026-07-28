using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TUD_UI_Console : MonoBehaviour
{
    [SerializeField] Text text;
    //[SerializeField] UIButton logBtn;

    private void OnEnable()
    {
        text.text = TUD_Logger.colorStringShort;
        TUD_Logger.onLog += OnLog;
        //logBtn.onClick += OnLogClicked;
    }

    private void OnDisable()
    {
        TUD_Logger.onLog -= OnLog;
        //logBtn.onClick -= OnLogClicked;
    }

    void OnLogClicked(UIButton btn)
    {
        //todo: open file location?
    }

    void OnLog(string condition, string stackTrace, UnityEngine.LogType type)
    {
        text.text = TUD_Logger.colorStringShort;
    }
}
