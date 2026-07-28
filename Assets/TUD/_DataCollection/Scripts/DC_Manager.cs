using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

[DefaultExecutionOrder(-10000)]//initialize before data collectors
public class DC_Manager : MonoBehaviour
{
    static DC_Manager main;

    [SerializeField] DC_Recorder _recorder;
    public static DC_Recorder recorder => main._recorder;

    HashSet<DC_DataCollector> _dataCollectors = new HashSet<DC_DataCollector>();

    public static List<DC_DataCollector> allCollectors { get { if (main == null) return new List<DC_DataCollector>(); return main._dataCollectors.ToList(); } } //todo: could be optimized

    DC_Cache cache;

    public static Action<string> onDataAdded;

    private void Awake()
    {
        if (main != null)
            Debug.LogError(nameof(DC_Manager) + ": Error. Multiple instances found. This might cause unexpected behaviour. Make sure there is only one instance of " + nameof(DC_Manager) + " in the scene.");
        main = this;
        cache = new DC_Cache();
    }

    private void OnDestroy()
    {
        main = null;
    }

    #region manage collector list
    public static void AddDataCollector(DC_DataCollector collector)
    {
        if(main == null)
        {
            Debug.LogError(nameof(DC_Manager) + ", add collector: Manager not initialized. Check execution order.");
            return;
        }
        if(collector == null)
        {
            Debug.LogError(nameof(DC_Manager) + ", add collector: Collector cannot be null. Skipping.");
            return;
        }
        if (main._dataCollectors.Contains(collector))
        {
            Debug.Log(nameof(DC_Manager) + ", add collector: Collector has already been added. Skipping.");
            return;
        }
        main._dataCollectors.Add(collector);
    }

    public static void RemoveDataCollector(DC_DataCollector collector)
    {
        if (main == null)
        {
            Debug.LogError(nameof(DC_Manager) + ", remove collector: Manager not initialized. Check execution order.");
            return;
        }
        if (collector == null)
        {
            Debug.LogError(nameof(DC_Manager) + ", remove collector: Collector cannot be null. Skipping.");
            return;
        }
        if (main._dataCollectors.Contains(collector) == false)
        {
            Debug.Log(nameof(DC_Manager) + ", remove collector: Collector has not been added to the list. Skipping.");
            return;
        }
        main._dataCollectors.Remove(collector);
    }
    #endregion

    #region Add & Get Data
    public static void AddData(string id, float value)
    {
        main.cache.AddEntryNow(id, value);
        onDataAdded?.Invoke(id);
    }

    public static bool TryGetLatestCachedData(string id, out DataEntry entry)
    {
        return main.cache.TryGetLatestEntry(id, out entry);
    }
    public static bool TryGetAllCachedData(string id, out DataEntry[] entries)
    {
        return main.cache.TryGetAllEntries(id, out entries);
    }
    public static bool TryGetCachedDataRange(string id, float fromTime, float toTime, out DataEntry[] entries, bool includeFrom = true, bool includeTo = true)
    {
        bool found = main.cache.TryGetAllEntries(id, out DataEntry[] entriesTemp);
        if (!found)
        {
            entries = null;
            return false;
        }

        Stack<DataEntry> entriesInRange = new Stack<DataEntry>();

        for (int ct = 0; ct < entriesTemp.Length; ct++)
        {
            DataEntry entry = entriesTemp[ct];
            if (entry.time < fromTime && includeFrom || entry.time <= fromTime && !includeFrom)
                continue;
            if (entry.time > toTime && includeTo || entry.time >= toTime && !includeTo)
                break;

            entriesInRange.Push(entry);
        }

        if(entriesInRange.Count == 0)
        {
            entries = null;
            return false;
        }

        entries = entriesInRange.ToArray();
        return true;
    }
    #endregion
}
