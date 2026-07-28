using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using ChartUtil;

/// <summary>
/// Singleton that manages overlay graphs for data ui.
/// Controls EZChart & adds data from cache.
/// Shows & hides overlay graphs.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class DUI_GraphManager : MonoBehaviour
{
    static DUI_GraphManager main;

    [SerializeField]  float _updateInterval = 1;
    public static float updateInterval { get => main._updateInterval; set { main._updateInterval = value; } }

    float _lastUpdate = 0;
    public static float lastUpdate => main._lastUpdate;

    public static Action onRefresh;

    public float graphRange = 30; // in seconds. Show the last x seconds

    [SerializeField] Chart mainChart;
    [SerializeField] ChartOptions mainChartOptions;
    [SerializeField] ChartData mainChartData;

    [SerializeField] GameObject graphPanel;

    List<DUI_Graph> _graphs = new List<DUI_Graph>();

    private void Awake()
    {
        main = this;
    }

    private void Update()
    {
        if(Time.time - _updateInterval >= _lastUpdate)
            RefreshChart();
    }

    void RefreshChart(bool refreshData = true)
    {
        _lastUpdate = Time.time;
        if(refreshData)
            UpdateAllGraphs();
        mainChartOptions.xAxis.autoAxisValues = false;
        mainChartOptions.xAxis.min = Time.time - graphRange;
        mainChartOptions.xAxis.max = Time.time;

        mainChart.UpdateChart();
        onRefresh?.Invoke();
    }

    string GetGraphTitle()
    {
        string titleString = "";
        for(int ct = 0; ct < _graphs.Count; ct++)
        {
            DUI_Graph graph = _graphs[ct];
            if (graph == null)
                continue;
            string color = "#" + ((int)(graph.color.r*255)).ToString("X2") + ((int)(graph.color.g * 255)).ToString("X2") + ((int)(graph.color.b * 255)).ToString("X2");
            if (ct != 0)
                titleString += ", ";
            titleString += "<color=" + color + ">" + graph.title + " (" + graph.shortTitle + ")" + "</color>";
        }
        return titleString;
    }

    string GetYAxisTitle()
    {
        string titleString = "";
        for (int ct = 0; ct < _graphs.Count; ct++)
        {
            DUI_Graph graph = _graphs[ct];
            if (graph == null)
                continue;
            string color = "#" + ((int)(graph.color.r * 255)).ToString("X2") + ((int)(graph.color.g * 255)).ToString("X2") + ((int)(graph.color.b * 255)).ToString("X2");
            if (ct != 0)
                titleString += ", ";
            titleString += "<color=" + color + ">" + graph.shortTitle + "(" + graph.units + ")" + "</color>";
        }
        return titleString;
    }

    void ToggleGraphPanel()
    {
        ToggleGraphPanel(!graphPanel.activeSelf);
    }

    void ToggleGraphPanel(bool on)
    {
        graphPanel.SetActive(on);
    }

    public static void AddGraph(DUI_Graph graph)
    {
        if (main._graphs.Contains(graph))
            return;//todo: log warning

        if(main._graphs.Count == 0)
        {
            main.ToggleGraphPanel(true);
        }

        main._graphs.Add(graph);
        List<Color> dataColors = new List<Color>(main.mainChartOptions.plotOptions.dataColor);
        dataColors.Add(graph.color);
        main.mainChartOptions.plotOptions.dataColor = dataColors.ToArray();

        Series graphSeries = new Series();
        graphSeries.name = graph.title;
        //main.UpdateGraphData(ref graphSeries, graph, Time.time - main.graphRange-updateInterval, Time.time); //todo: clamp time? Not clamping has benefit of consistent graph scale
        main.mainChartData.series.Add(graphSeries);

        main.mainChartOptions.title.mainTitle = main.GetGraphTitle();
        main.mainChartOptions.yAxis.title = main.GetYAxisTitle();

        main.RefreshChart();
    }

    public static void RemoveGraph(DUI_Graph graph)
    {
        if ( ! main._graphs.Contains(graph))
            return;//todo: log warning

        int index = main._graphs.IndexOf(graph);

        if (main._graphs.Count == 1)
        {
            main.ToggleGraphPanel(false);
        }

        main._graphs.Remove(graph);
        List<Color> dataColors = new List<Color>(main.mainChartOptions.plotOptions.dataColor);
        dataColors.RemoveAt(index);
        main.mainChartOptions.plotOptions.dataColor = dataColors.ToArray();
        main.mainChartData.series.RemoveAt(index);

        main.mainChartOptions.title.mainTitle = main.GetGraphTitle();
        main.mainChartOptions.yAxis.title = main.GetYAxisTitle();

        main.RefreshChart();
    }

    void UpdateAllGraphs()
    {
        foreach(DUI_Graph graph in _graphs)
        {
            if (graph == null)//todo: remove graph if null?
                continue;
            UpdateGraph(graph);
        }
    }

    void UpdateGraph(DUI_Graph graph)
    {
        int index = _graphs.IndexOf(graph);
        if (index == -1)//not found. Todo: warning or error.
            return;
        if (index >= mainChartData.series.Count)
            return;
        Series graphSeries = mainChartData.series[index];
        UpdateGraphData(ref graphSeries, graph, Time.time - graphRange-updateInterval, Time.time);
    }

    void UpdateGraphData(ref Series graphSeries, DUI_Graph graph, float fromTime, float toTime)
    {
        List<Data> data = graphSeries.data;
        int removeLeading = 0;
        for(int ct = 0; ct < data.Count; ct++)//remove elements before fromTime
        {
            removeLeading = ct;

            if (data[ct].x >= fromTime)
                break;
        }
        data.RemoveRange(0, removeLeading);

        int removeTrailing = data.Count-1;
        for (int ct = data.Count-1; ct >= 0 ; ct--)//remove elements after toTime
        {
            removeTrailing = ct;

            if (data[ct].x <= toTime)
                break;
        }
        if(removeTrailing >= 0)
            data.RemoveRange(removeTrailing, data.Count - removeTrailing - 1);

        if (data.Count == 0)
        {
            bool newDataFound = DC_Manager.TryGetCachedDataRange(graph.dataID, fromTime, toTime, out DataEntry[] newDataRaw); //todo: check data type & generic data?
            if (newDataFound)
            {
                Data[] newdata = new Data[newDataRaw.Length];
                for (int ct = 0; ct < newDataRaw.Length; ct++)
                    newdata[ct] = new Data(newDataRaw[ct].data * graph.factor, newDataRaw[ct].time);

                data.AddRange(newdata);// insert all data
            }
        }
        else
        {
            bool leadingDataFound = DC_Manager.TryGetCachedDataRange(graph.dataID, fromTime, data[0].x, out DataEntry[] leadingDataRaw); //todo: check data type & generic data?
            if (leadingDataFound)
            {
                Data[] leadingData = new Data[leadingDataRaw.Length];
                for (int ct = 0; ct < leadingDataRaw.Length; ct++)
                    leadingData[ct] = new Data(leadingDataRaw[ct].data * graph.factor, leadingDataRaw[ct].time);

                data.InsertRange(0, leadingData);// insert missing leading data
            }


            bool trailingDataFound = DC_Manager.TryGetCachedDataRange(graph.dataID, data[data.Count - 1].x, toTime, out DataEntry[] trailingDataRaw); //todo: check data type?
            if (trailingDataFound)
            {
                Data[] trailingData = new Data[trailingDataRaw.Length];
                for (int ct = 0; ct < trailingDataRaw.Length; ct++)
                    trailingData[ct] = new Data(trailingDataRaw[ct].data * graph.factor, trailingDataRaw[ct].time);

                data.InsertRange(data.Count, trailingData);// insert missing trailing data
            }
        }
    }
}
