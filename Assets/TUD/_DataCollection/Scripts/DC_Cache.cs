using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// keeps latest data for the specified timespan (buffer length).
/// Todo: support generic data?
/// </summary>
public class DC_Cache
{
    float bufferTime = 60; //in seconds

    Dictionary<string, DataStack> dataLists = new Dictionary<string, DataStack>();

    public DC_Cache() { }
    public DC_Cache(float bufferTime)
    {
        this.bufferTime = bufferTime; 
    }
    /// <summary>
    /// Add entry at specified time. Time has no influence on order of elements.
    /// </summary>
    public void AddEntry(string id, float time, float value)
    {
        DataStack stack;
        bool exists = dataLists.TryGetValue(id, out stack);
        if (!exists)
        {
            stack = new DataStack(id);
            stack.minBufferTime = bufferTime;
            dataLists.Add(id, stack);
        }
        stack.AddEntry(time, value);
    }
    /// <summary>
    /// Adds an entry at current game time
    /// </summary>
    public void AddEntryNow(string id, float value)
    {
        AddEntry(id, Time.time, value);
    }

    public bool TryGetLatestEntry(string id, out DataEntry entry)
    {
        bool found = dataLists.TryGetValue(id, out DataStack stack);
        if (!found)
        {
            entry = new DataEntry();
            return false;
        }

        return stack.TryGetLastEntry(out entry);
    }

    public bool TryGetAllEntries(string id, out DataEntry[] entries)
    {
        bool found = dataLists.TryGetValue(id, out DataStack stack);
        if (!found)
        {
            entries = null;
            return false;
        }

        return stack.TryGetAllEntries(out entries);
    }
}

