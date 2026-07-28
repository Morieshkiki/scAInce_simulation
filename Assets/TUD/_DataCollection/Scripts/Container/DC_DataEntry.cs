public struct DataEntry //todo: generic?
{
    float _time;
    public float time => _time;
    float _data;
    public float data => _data;
    public DataEntry(float time, float data)
    {
        _time = time;
        _data = data;
    }
}