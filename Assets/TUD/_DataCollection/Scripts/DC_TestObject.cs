using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DC_TestObject : DC_DataCollector
{
    public float dataCurrent;

    [SerializeField] string id = "Test.test1";
    [SerializeField, Range(0.01f,5)] float updateInterval = 0.1f;
    [SerializeField] float dataMin = 0, dataMax = 100;

    IEnumerator coroutine;

    public override string[] dataIDs => new string[] {id };

    private void Start()
    {
        coroutine = InitLoop();
        StartCoroutine(coroutine);
    }

    IEnumerator InitLoop()
    {
        while (true)
        {
            dataCurrent = UnityEngine.Random.Range(dataMin, dataMax);
            DC_Manager.AddData(id, dataCurrent);
            yield return new WaitForSeconds(updateInterval);
        }
    }
}
