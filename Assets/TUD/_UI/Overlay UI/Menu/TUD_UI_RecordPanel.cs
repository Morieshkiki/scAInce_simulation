using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class TUD_UI_RecordPanel : MonoBehaviour
{
    [SerializeField] UIToggleButton recButton;
    [SerializeField] Text infoText;

    string currentText;

    private void OnEnable()
    {
        Refresh();
        DC_Manager.recorder.onRecordStateChanged += OnRecordChanged;
        recButton.onClick += OnRecordClicked;
    }

    private void Update()
    {
        if (DC_Manager.recorder.isRecording)
        {
            TimeSpan ts = TimeSpan.FromSeconds(Time.time - DC_Manager.recorder.startTime);
            infoText.text = currentText + "<color=#ff0000>" + ts.ToString("hh':'mm':'ss") + "\t\tFrame: " + (Time.frameCount - DC_Manager.recorder.startFrame).ToString() + "</color>";
        }
    }

    private void OnDisable()
    {
        DC_Manager.recorder.onRecordStateChanged -= OnRecordChanged;
        recButton.onClick -= OnRecordClicked;
    }

    void Refresh()
    {
        bool isRecording = DC_Manager.recorder.isRecording;
        recButton.state = isRecording;
        if (isRecording)
        {
            string str = "";
            str += "Recording to file:\r\n";
            string fileName = DC_Manager.recorder.currentFile;
            string path = DC_Manager.recorder.GetAbsolutePath(DC_Manager.recorder.currentFile);
            str += path + "\r\n";
            currentText = str;
            TimeSpan ts = TimeSpan.FromSeconds(Time.time - DC_Manager.recorder.startTime);
            str += "<color=#ff0000>" + ts.ToString("hh':'mm':'ss") + "\t\tFrame: " + (Time.frameCount -  DC_Manager.recorder.startFrame).ToString() + "</color>"; 
            infoText.text = str;
        }
        else
        {
            infoText.text = "Files will be saved at:\r\n" + DC_Manager.recorder.GetAbsolutePath("");
        }
    }

    void OnRecordChanged()
    {
        Refresh();
    }

    void OnRecordClicked(UIButton btn)
    {
        DC_Manager.recorder.ToggleRecord();
    }
}
