using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ChartUtil.Demo
{
    public class ChartDemoLineGraph : MonoBehaviour
    {
        public Chart chart;
        public int minCount = 4, maxCount = 10;
        public float minValue = 0.0f, maxValue = 0.0f;
        public float minX = 0.0f, maxX = 100.0f;
        
        public void GenerateData()
        {
            if (chart == null) return;
            
            for (int i = 0; i < chart.chartData.series.Count; ++i)
            {
                Series s = chart.chartData.series[i];
                s.data.Clear();
                int count = Random.Range(minCount, maxCount);
                float xUnit = (maxX - minX) / count;
                for (int j = 0; j < count; ++j)
                {
                    float x = Random.Range(xUnit * j, xUnit * (j + 0.6f));
                    float value = (1000 + i) * Random.Range(0.5f, 1.0f);
                    s.data.Add(new Data(value, x, true));
                }
            }
        }
    }
}