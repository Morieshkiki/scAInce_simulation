using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DC_DataVisualizer : MonoBehaviour
{
    bool _visualizeData = false;
    public bool visualizeData { get { return _visualizeData; } set { SetVisualizeData(value); } }

    void SetVisualizeData(bool on)
    {
        if (on == _visualizeData)
            return;

        _visualizeData = on;
        if (on)
        {
            OnBeginVisualization();   
        }
        else
        {
            OnEndVisualization();
        }
    }

    protected abstract void OnBeginVisualization();
    protected abstract void OnEndVisualization();
}

public abstract class DC_DataVisualizer<T> : DC_DataVisualizer where T : DC_DataCollector
{
    public T dataCollector;

    private void OnEnable()
    {
    }

    private void OnDisable()
    {
    }

    
}
