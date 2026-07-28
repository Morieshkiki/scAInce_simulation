using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TUD_UI_FooterRecInfo : MonoBehaviour
{
    [SerializeField] GameObject content;
    [SerializeField] Image recIcon;
    [SerializeField] Text recText, fileText;

    private void OnEnable()
    {
        Refresh();
        DC_Manager.recorder.onRecordStateChanged += OnRecordChanged;
    }

    private void Update()
    {
        if (DC_Manager.recorder.isRecording)
        {
            TimeSpan ts = TimeSpan.FromSeconds(Time.time - DC_Manager.recorder.startTime);
            recText.text = "REC\t" + ts.ToString("hh':'mm':'ss'.'fff") + "\t\tF: " + (Time.frameCount - DC_Manager.recorder.startFrame).ToString();
        }
    }

    private void OnDisable()
    {
        DC_Manager.recorder.onRecordStateChanged -= OnRecordChanged;
    }

    void Refresh()
    {
        bool isRecording = DC_Manager.recorder.isRecording;
        content.SetActive(isRecording);
        if (!isRecording)
            return;
        
        string fileName = DC_Manager.recorder.currentFile;
        fileText.text = fileName;
    }

    void OnRecordChanged()
    {
        Refresh();
    }
}
