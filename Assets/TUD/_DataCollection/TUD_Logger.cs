using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class TUD_Logger : MonoBehaviour
{
    static TUD_Logger main;

    string _colorStringShort = "";
    [SerializeField] int characterLimit = 2000;
    //todo: list with archived messages

    public static string colorStringShort => main._colorStringShort;

    public static Action<string, string, UnityEngine.LogType> onLog;

    private void Awake()
    {
        main = this;
    }

    private void OnEnable()
    {
        Application.logMessageReceived += OnLog;

    }

    private void OnDisable()
    {
        Application.logMessageReceived -= OnLog;
    }

    void OnLog(string condition, string stackTrace, UnityEngine.LogType type)
    {
        Color color = Color.white;
        switch (type)
        {
            case LogType.Error:
                color = new Color(1, 0, 0);
                break;
            case LogType.Assert:
                color = new Color(1, 0, 0.3f);
                break;
            case LogType.Warning:
                color = new Color(1, 1, 0);
                break;
            case LogType.Log:
                color = new Color(1, 1, 1);
                break;
            case LogType.Exception:
                color = new Color(1, 0.3f, 0);
                break;
        }
        string colorHex = ColorUtility.ToHtmlStringRGB(color);
        _colorStringShort = _colorStringShort += "<color=#" + colorHex + ">" + System.DateTime.Now.ToString("HH:mm:ss") + "</color>" + "\t" + condition + "\r\n";
        if (_colorStringShort.Length > characterLimit)
        {
            _colorStringShort = _colorStringShort.Remove(0, _colorStringShort.Length - characterLimit);
        }
        onLog?.Invoke(condition, stackTrace, type);
    }
}
