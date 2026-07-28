using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartUtil
{
    public enum MixMethod
    {
        None, BarToLine, LineToBar
    }

    [RequireComponent(typeof(Chart))]
    public class ChartMixer : MonoBehaviour
    {
        public MixMethod mixMethod;
        public List<int> seriesToMix;

        public void UpdateContent(ChartBase chart)
        {
            if (seriesToMix == null || seriesToMix.Count == 0) { chart.UpdateContentItems(); return; }

            switch (mixMethod)
            {
                case MixMethod.BarToLine:
                    {
                        BarChart barChart = (BarChart)chart;
                        if (barChart != null)
                        {
                            LineChart lineChart = barChart.gameObject.AddComponent<LineChart>();
                            lineChart.chartInfo = barChart.chartInfo;
                            lineChart.chartGrid = barChart.chartGrid;
                            List<int> barSkip = new List<int>();
                            List<int> lineSkip = new List<int>();
                            for (int i = 0; i < barChart.chartInfo.data.series.Count; ++i)
                            {
                                if (seriesToMix.Contains(i)) barSkip.Add(i);
                                else lineSkip.Add(i);
                            }
                            lineChart.chartInfo.skipSeries = lineSkip;
                            lineChart.UpdateContentItems();
                            barChart.chartInfo.skipSeries = barSkip;
                            barChart.UpdateContentItems();
                            lineChart.chartInfo.skipSeries = null;
                        }
                        else
                        {
                            chart.UpdateContentItems();
                        }
                    }
                    break;
                case MixMethod.LineToBar:
                    {
                        LineChart lineChart = (LineChart)chart;
                        if (lineChart != null)
                        {
                            BarChart barChart = lineChart.gameObject.AddComponent<BarChart>();
                            barChart.chartInfo = lineChart.chartInfo;
                            barChart.chartGrid = lineChart.chartGrid;
                            List<int> barSkip = new List<int>();
                            List<int> lineSkip = new List<int>();
                            for (int i = 0; i < lineChart.chartInfo.data.series.Count; ++i)
                            {
                                if (seriesToMix.Contains(i)) lineSkip.Add(i);
                                else barSkip.Add(i);
                            }
                            barChart.chartInfo.skipSeries = barSkip;
                            barChart.UpdateContentItems();
                            lineChart.chartInfo.skipSeries = lineSkip;
                            lineChart.UpdateContentItems();
                            barChart.chartInfo.skipSeries = null;
                        }
                        else
                        {
                            chart.UpdateContentItems();
                        }
                    }
                    break;
                default:
                    chart.UpdateContentItems();
                    break;
            }
        }
    }
}