using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class DC_DataCollector : MonoBehaviour
{
    //public bool collecting => _collecting;
    //bool _collecting = false;

    /// <summary>Event when the data collector started collecting data.</summary>
    //public Action<DC_DataCollector> onBeginCollecting;
    /// <summary>Event when the data collector stopped collecting data.</summary>
    //public Action<DC_DataCollector> onEndCollecting;
    /// <summary>Event after the data collector collected a datapoint/set.</summary>
    public Action<DC_DataCollector, DC_Data> onCollect;
    /// <summary>
    /// List of ids this collector is using.
    /// </summary>
    public abstract string[] dataIDs { get; }

    protected virtual void OnEnable()
    {
        DC_Manager.AddDataCollector(this);
    }

    protected virtual void OnDisable()
    {
        DC_Manager.RemoveDataCollector(this);
    }

    /*public void BeginCollecting()
    {
        if (_collecting)
            return;

        _collecting = true;
        OnBeginCollecting();
        onBeginCollecting?.Invoke(this);
    }

    public void EndCollecting()
    {
        if (!_collecting)
            return;

        _collecting = false;
        OnEndCollecting();
        onEndCollecting?.Invoke(this);
    }

    protected abstract void OnBeginCollecting();
    protected abstract void OnEndCollecting();
    */
}
