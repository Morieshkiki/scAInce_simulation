using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public class DC_Recorder : MonoBehaviour
{
    [SerializeField] string relativePath = "SC_Recorded Data";
    [SerializeField] float writeInterval = 10f;

    //Stack<Task> queue = new Stack<Task>();

    Chart chart = null;
    float lastWrite = 0;

    string _currentFile;
    public string currentFile => _currentFile;

    public Action<DC_Recorder> onError; //todo: add error codes

    float _startTime = 0;
    public float startTime => _startTime;
    int _startFrame = 0;
    public int startFrame => _startFrame;

    bool _isRecording;
    public bool isRecording => _isRecording;

    public Action onRecordStateChanged;

    private void Update()
    {
        if(chart != null && Time.time - lastWrite >= writeInterval)
        {
            //todo: add latest data to file
            Debug.Log("Writing to file");
            string str = chart.ToString(false);
            chart.Clear(false);
            AddToFile(_currentFile, str);
        }
    }

    #region start & stop
    public void StartRecording()
    {
        if(_isRecording)
        {
            Debug.LogWarning("Unable to start record. Already recording.");
            return;
        }
        _isRecording = true;
        _startTime = Time.time;
        _startFrame = Time.frameCount;
        lastWrite = Time.time;
        chart = new Chart();
        StartNewFile();
        DC_Manager.onDataAdded += OnDataAdded;
        onRecordStateChanged?.Invoke();
    }

    public void StopRecording()
    {
        if (_isRecording == false)
        {
            StopWithError("Unable to stop recording. Already stopped.");
            return;
        }
        //write cached data
        Debug.Log("Finishing file: \r\n" + GetAbsolutePath(_currentFile));
        string str = chart.ToString(false);
        chart.Clear(false);
        AddToFile(_currentFile, str);

        _isRecording = false;
        DC_Manager.onDataAdded -= OnDataAdded;
        chart = null;
        _currentFile = null;
        onRecordStateChanged?.Invoke();
    }

    public void ToggleRecord()
    {
        if (_isRecording)
            StopRecording();
        else
            StartRecording();
    }
    #endregion

    void OnDataAdded(string id)
    {
        if (_isRecording == false)
            StopWithError("Unexpected event. Incoming data while not recording.");
        bool found = DC_Manager.TryGetLatestCachedData(id, out DataEntry entry);
        if (!found)
            return;
        chart.AddElement(id, entry.time, entry.data);
    }

    #region IO

    void StartNewFile()
    {
        _currentFile = "Record_" + DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss") + ".csv";
        string path = GetAbsolutePath(_currentFile);
        Debug.Log("Start recording to file: \r\n" + path);
        if (File.Exists(path))
        {
            StopWithError("File already exists: " + path);
            return;
        }

        List<DC_DataCollector> allCollectors = DC_Manager.allCollectors;
        List<string> dataIDs = new List<string>();
        foreach(DC_DataCollector collector in allCollectors)
        {
            if (collector == null)
                continue;
            dataIDs.AddRange(collector.dataIDs);
        }

        dataIDs.Sort();//sort alphabetically
        
        for(int ct = 0; ct < dataIDs.Count; ct++)
        {
            chart.AddColumn(dataIDs[ct]);
        }

        try
        {
            if(!Directory.Exists(GetAbsolutePath("")))
             Directory.CreateDirectory(GetAbsolutePath(""));
            using (FileStream fs = File.Create(path)) { }
            File.AppendAllText(path, chart.ToString(true), Encoding.UTF8);
        }
        catch (Exception e)
        {
            StopWithError(e.Message);
            return;
        }
    }

    void AddToFile(string fileName, string data)
    {
        string path = GetAbsolutePath(fileName);
        if(File.Exists(path) == false)
        {
            StopWithError("Current plot file does not exist: " + path);
            return;
        }
        try
        {
            File.AppendAllText(path, "\r\n" + data, Encoding.UTF8);
        }catch(Exception e)
        {
            StopWithError(e.Message);
            return;
        }
        lastWrite = Time.time;
    }

    void FinishFile(string fileName)
    {
        string path = GetAbsolutePath(fileName);
        if (File.Exists(path) == false)
        {
            StopWithError("Current plot file does not exist: " + path);
            return;
        }
        try
        {
        }
        catch (Exception e)
        {
            StopWithError(e.Message);
            return;
        }
    }

    #endregion

    void StopWithError(string errorMsg)
    {
        Debug.LogError(errorMsg);
        _isRecording = false;
        chart = null;
        _currentFile = null;
        DC_Manager.onDataAdded -= OnDataAdded;
        onError?.Invoke(this);
        onRecordStateChanged?.Invoke();
    }

    #region Utility
    public string GetAbsolutePath(string fileName)
    {
        string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        path = Path.Combine(path, relativePath);
        path = Path.Combine(path, fileName);

        return path;
    }
    #endregion

    class Chart//todo: optimize using dictionaries
    {
        List<string> columnIds = new List<string>();
        List<float> rowIds = new List<float>();
        List<List<float?>> rows = new List<List<float?>>();

        public void AddColumn(string id)
        {
            columnIds.Add(id);
        }

        public void AddElement(string columnID, float rowID, float value)
        {
            int columnIndex = columnIds.IndexOf(columnID);
            if (columnIndex < 0)
            {
                columnIds.Add(columnID);
                columnIndex = columnIds.Count-1;
            }
            int rowIndex = rowIds.IndexOf(rowID);

            if (rowIndex < 0)
            {
                rowIds.Add(rowID);
                rows.Add(new List<float?>());
                rowIndex = rows.Count - 1;
            }
            List<float?> row = rows[rowIndex];
            while (row.Count < columnIndex + 1)
                row.Add(null);
            row[columnIndex] = value;
        }

        public override string ToString()
        {
            return ToString(true);
        }
        public string ToString(bool includeChartIDs = false)
        {
            StringBuilder content = new StringBuilder();
            if (includeChartIDs)//add header
            {
                content.Append("Time");
                for(int ct = 0; ct < columnIds.Count; ct++)
                {
                    content.Append("," + FormatCsv(columnIds[ct]));
                }
                content.Append("\r\n");
            }
            for(int ct = 0; ct < rows.Count; ct++)
            {
                TimeSpan ts = TimeSpan.FromSeconds(rowIds[ct]);
                content.Append(ts.ToString(@"hh\:mm\:ss\.fff"));
                for(int ctr = 0; ctr < rows[ct].Count; ctr++)
                {
                    float? value = rows[ct][ctr];
                    content.Append("," + (value == null? "" : value.ToString()));
                }
                if(ct < rows.Count-1)
                    content.Append("\r\n");
            }
            return content.ToString();
        }

        string FormatCsv(string value)
        {
            string str;
            if(value.Contains('"') || value.Contains('\n') || value.Contains(','))
            {
                str = value.Replace("\"", "\"\"");
                str = "\"" + str + "\"";
                return str;
            }
            else
            {
                return value;
            }
        }
        public void Clear(bool clearColumnIds)
        {
            rowIds.Clear();
            rows.Clear();
            if (clearColumnIds)
            {
                columnIds.Clear();
            }
        }
    }
}
