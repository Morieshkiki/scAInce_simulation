using System.Collections.Generic;

public /*abstract*/ class DataStack
{
    protected string _id = "";
    public string id => _id;
    public float minBufferTime = 60;

    Stack<DataEntry> _entries = new Stack<DataEntry>();

    public DataStack(string id)
    {
        this._id = id;
    }

    public void AddEntry(float time, float data)
    {
        _entries.Push(new DataEntry(time, data));
        if (_entries.Count == 0)
            return;
        while (time - _entries.Peek().time > minBufferTime)
        {
            _entries.Pop();
        }
    }

    public bool TryGetLastEntry(out DataEntry entry)
    {
        return _entries.TryPeek(out entry);
    }
    public bool TryGetAllEntries(out DataEntry[] entries)
    {
        if (_entries.Count >= 1)
        {
            entries = _entries.ToArray();
            return true;
        }
        else
        {
            entries = null;
            return false;
        }
    }
}
/* Todo: generic
public class DataStack<T> : DataStack
{
    Stack<DataEntry<T>> _entries = new Stack<DataEntry<T>>();
    public void AddEntry(float time, T data)
    {
        _entries.Push(new DataEntry<T>(time, data));
        if (_entries.Count == 0)
            return;
        while (time - _entries.Peek().time > minBufferTime)
        {
            _entries.Pop();
        }
    }
    public DataStack(string id) : base(id) { }
}
*/
