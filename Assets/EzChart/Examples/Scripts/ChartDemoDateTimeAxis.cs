using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChartUtil.Demo
{
    public class ChartDemoDateTimeAxis : MonoBehaviour
    {
        [SerializeField] Chart myChart;

        void Start()
        {
            ChartLinearAxisDateTimeMapper mapper = myChart.GetComponent<ChartLinearAxisDateTimeMapper>();

            //set mapper values, higher range for higher precision
            mapper.valueA = 0;
            mapper.valueB = 1000000;
            //set chart x-axis to match the range
            myChart.chartOptions.xAxis.type = AxisType.Linear;
            myChart.chartOptions.xAxis.autoAxisValues = false;
            myChart.chartOptions.xAxis.min = 0;
            myChart.chartOptions.xAxis.max = 1000000;
            myChart.chartOptions.xAxis.axisDivision = 12;

            //set corresponding start date and end date
            System.DateTime startDate = System.DateTime.Now.Date;
            System.DateTime endDate = startDate.AddHours(24);
            mapper.ticksA = startDate.Ticks;
            mapper.ticksB = endDate.Ticks;
            mapper.dateTimeFormat = "MM/dd/yyyy HH:mm";
            
            //create new series
            Series series1 = new Series();
            series1.name = "Consumption A";
            series1.data.Add(new Data(62.5f, mapper.TicksToValue(startDate.AddHours(1.2).Ticks)));
            series1.data.Add(new Data(95.8f, mapper.TicksToValue(startDate.AddHours(4.4).Ticks)));
            series1.data.Add(new Data(53.6f, mapper.TicksToValue(startDate.AddHours(9.6).Ticks)));
            series1.data.Add(new Data(36.4f, mapper.TicksToValue(startDate.AddHours(14.1).Ticks)));
            series1.data.Add(new Data(45.9f, mapper.TicksToValue(startDate.AddHours(18.2).Ticks)));
            series1.data.Add(new Data(87.4f, mapper.TicksToValue(startDate.AddHours(21.7).Ticks)));

            //create new series
            Series series2 = new Series();
            series2.name = "Consumption B";
            series2.data.Add(new Data(52.8f, mapper.TicksToValue(startDate.AddHours(2.3).Ticks)));
            series2.data.Add(new Data(36.5f, mapper.TicksToValue(startDate.AddHours(7.1).Ticks)));
            series2.data.Add(new Data(98.3f, mapper.TicksToValue(startDate.AddHours(10.3).Ticks)));
            series2.data.Add(new Data(25.7f, mapper.TicksToValue(startDate.AddHours(13.7).Ticks)));
            series2.data.Add(new Data(36.2f, mapper.TicksToValue(startDate.AddHours(17.5).Ticks)));
            series2.data.Add(new Data(78.9f, mapper.TicksToValue(startDate.AddHours(22.1).Ticks)));

            //add series into series list
            myChart.chartData.series.Clear();
            myChart.chartData.series.Add(series1);
            myChart.chartData.series.Add(series2);

            //update chart
            myChart.UpdateChart();
        }
    }
}
