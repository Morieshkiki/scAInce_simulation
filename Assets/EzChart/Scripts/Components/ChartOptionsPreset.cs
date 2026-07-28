using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartUtil
{
    [CreateAssetMenu(fileName = "ChartOptionsPreset", menuName = "EzChart/ChartOptionsPreset", order = 1)]
    public class ChartOptionsPreset : ScriptableObject
    {
        public ChartOptions.PlotOptions plotOptions = new ChartOptions.PlotOptions();
        public ChartOptions.Title title = new ChartOptions.Title();
        public ChartOptions.XAxis xAxis = new ChartOptions.XAxis();
        public ChartOptions.YAxis yAxis = new ChartOptions.YAxis();
        public ChartOptions.Pane pane = new ChartOptions.Pane();
        public ChartOptions.Tooltip tooltip = new ChartOptions.Tooltip();
        public ChartOptions.Legend legend = new ChartOptions.Legend();
        public ChartOptions.Label label = new ChartOptions.Label();
    }
}