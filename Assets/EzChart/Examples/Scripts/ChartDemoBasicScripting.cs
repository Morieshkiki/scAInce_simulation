using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChartUtil.Demo
{
    public class ChartDemoBasicScripting : MonoBehaviour
    {
        [SerializeField] RectTransform chartHolder;

        Chart myChart = null;

        void Start()
        {
            //Add chart component
            myChart = chartHolder.gameObject.AddComponent<Chart>();
            myChart.chartType = ChartType.BarChart;

            //add chart options
            myChart.chartOptions = chartHolder.gameObject.AddComponent<ChartOptions>();
            myChart.chartOptions.title.mainTitle = "A chart about fruits!";
            myChart.chartOptions.title.enableSubTitle = false;
            myChart.chartOptions.label.enable = true;
            myChart.chartOptions.yAxis.title = "Weight";
            myChart.chartOptions.plotOptions.barChartOption.barWidth = 20.0f;
            myChart.chartOptions.tooltip.share = false;

            //add chart data
            myChart.chartData = chartHolder.gameObject.AddComponent<ChartData>();
            myChart.chartData.categories = new List<string> { "Apple", "Banana", "Cherries", "Durian", "Grapes", "Lemon" }; //set categories
            myChart.chartData.series = new List<Series>();  //new series list

            //create new series
            Series series1 = new Series();
            series1.name = "Sold";
            series1.data.Add(new Data(122.5f));
            series1.data.Add(new Data(95.8f));
            series1.data.Add(new Data(53.6f));
            series1.data.Add(new Data(36.4f));
            series1.data.Add(new Data(45.9f));
            series1.data.Add(new Data(87.4f));

            Series series2 = new Series();
            series2.name = "Storage";
            series2.data.Add(new Data(152.8f));
            series2.data.Add(new Data(36.5f));
            series2.data.Add(new Data(98.3f));
            series2.data.Add(new Data(125.7f));
            series2.data.Add(new Data(36.2f));
            series2.data.Add(new Data(78.9f));

            //add series into series list
            myChart.chartData.series.Add(series1);
            myChart.chartData.series.Add(series2);

            //update chart
            myChart.UpdateChart();
        }
    }
}
