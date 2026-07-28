using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartUtil.Demo
{
    public class ChartDemoMassiveDataLinear : MonoBehaviour
    {
        [SerializeField] int length = 1000;
        [SerializeField] Chart chart;

        void Start()
        {
            GenerateData();
        }

        public void GenerateData()
        {
            //new series
            chart.chartData.series = new List<Series>();
            chart.chartData.series.Add(new Series());
            chart.chartData.series[0].name = "Massive Data";

            //generate data
            for (int i = 0; i < length; ++i)
            {
                float value = (10 + i) * Random.Range(0.9f, 1.0f);
                chart.chartData.series[0].data.Add(new Data(value, i, true));
            }

            //update chart
            chart.UpdateChart();
        }
    }
}
