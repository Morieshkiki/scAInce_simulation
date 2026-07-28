using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChartUtil.Demo
{
    public class ChartDemoEventsHandle : MonoBehaviour
    {
        [SerializeField] Chart chart;
        [SerializeField] Text chartEventText;

        public void OnItemClick(int cateIndex, int seriesIndex)
        {
            string cateStr = cateIndex >= 0 ?
                "Category (" + chart.chartData.categories[cateIndex] + ") is clicked" :
                "No category is clicked";
            string seriesStr = seriesIndex >= 0 ?
                "Series (" + chart.chartData.series[seriesIndex].name + ") is clicked" :
                "No series is clicked";
            chartEventText.text = "Item Click Event: " + cateStr + ", " + seriesStr;
        }

        public void OnItemClickLinear(int dataIndex, int seriesIndex)
        {
            string str = seriesIndex >= 0 ? 
                "Series (" + chart.chartData.series[seriesIndex].name + ") - " +
                "Data (" + dataIndex + ") is clicked" :
                "No series is clicked";
            chartEventText.text = "Item Click Event: " + str;
        }

        public void OnSeriesToggle(int seriesIndex, bool isOn)
        {
            string toggleStr = isOn ? "On" : "Off";
            chartEventText.text = "Series Toggle Event: Series \"" + chart.chartData.series[seriesIndex].name + "\" has been turned " + toggleStr;
        }

        public void OnValueClick(float x, float y)
        {
            string txt = "\nClicked Value: (" + x.ToString() + ", " + y.ToString() + ")";
            chartEventText.text += txt;
        }
    }
}