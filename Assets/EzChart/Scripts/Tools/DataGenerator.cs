//This data generator is tool to quickly generate random data for chart

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartUtil
{
    public class DataGenerator : MonoBehaviour
    {
        public enum CategoryType
        {
            Day, Day_short, Week, Week_short, Month, Time_15, Time_30, Time_60, Custom
        }

        public static string[] NamesOfWeekdays = new string[]
            { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
        public static string[] NamesOfWeekdays_Short = new string[]
            { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"};
        public static string[] NamesOfMonths = new string[]
            { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        public ChartData chartData;
        public float minValue = 0.0f, maxValue = 0.0f;
        public CategoryType cateType;
        public string[] customCategory;
        public int numberOfSeries = 1;
        public string seriesBaseName = "Series";

        private void Reset()
        {
            if (chartData == null)
            {
                chartData = GetComponent<ChartData>();
                if (chartData == null && GetComponent<Chart>() != null) chartData = GetComponent<Chart>().chartData;
            }
        }

        public void GenerateRandomData()
        {
            GenerateCategory();
            GenerateSeriesData();
        }

        public void GenerateCategory()
        {
            if (chartData == null) return;
            if (cateType == CategoryType.Custom)
            {
                chartData.categories = new List<string>();
                chartData.categories.AddRange(customCategory);
            }
            else
            {
                chartData.categories = GetCategory(cateType, System.DateTime.Now);
            }
        }

        public static List<string> GetCategory(CategoryType cateType, System.DateTime date)
        {
            List<string> categories = new List<string>();
            switch (cateType)
            {
                case CategoryType.Day:
                    {
                        System.DateTime d = new System.DateTime(date.Year, date.Month, 1);
                        int days = System.DateTime.DaysInMonth(date.Year, date.Month);
                        for (int i = 0; i < days; ++i)
                        {
                            categories.Add(d.AddDays(i).ToString("yyyy/MM/dd"));
                        }
                    }
                    break;
                case CategoryType.Day_short:
                    {
                        int days = System.DateTime.DaysInMonth(date.Year, date.Month);
                        for (int i = 1; i <= days; ++i)
                        {
                            categories.Add(i.ToString());
                        }
                    }
                    break;
                case CategoryType.Week:
                    categories.AddRange(NamesOfWeekdays);
                    break;
                case CategoryType.Week_short:
                    categories.AddRange(NamesOfWeekdays_Short);
                    break;
                case CategoryType.Month:
                    categories.AddRange(NamesOfMonths);
                    break;
                case CategoryType.Time_15:
                    for (int i = 0; i < 96; ++i)
                    {
                        categories.Add((i / 4).ToString("00") + ":" + (i % 4 * 15).ToString("00"));
                    }
                    break;
                case CategoryType.Time_30:
                    for (int i = 0; i < 48; ++i)
                    {
                        categories.Add((i / 2).ToString("00") + ":" + (i % 2 * 30).ToString("00"));
                    }
                    break;
                case CategoryType.Time_60:
                    for (int i = 0; i < 24; ++i)
                    {
                        categories.Add(i.ToString("00") + ":00");
                    }
                    break;
                default: break;
            }
            return categories;
        }

        public void GenerateSeriesData()
        {
            if (chartData == null) return;
            chartData.series = GetRandomSeriesData(chartData.categories.Count, minValue, maxValue, numberOfSeries, seriesBaseName);
        }

        public static List<Series> GetRandomSeriesData(int cateLength, float minValue, float maxValue, int numberOfSeries, string seriesBaseName)
        {
            List<Series> series = new List<Series>();
            for (int i = 0; i < numberOfSeries; ++i)
            {
                Series s = new Series();
                s.name = seriesBaseName + (i + 1).ToString("00");
                for (int j = 0; j < cateLength; ++j)
                {
                    Data d = new Data();
                    d.value = Random.Range(minValue, maxValue);
                    s.data.Add(d);
                }
                series.Add(s);
            }
            return series;
        }

        public void RegenerateSeriesData()
        {
            if (chartData == null) return;
            foreach (var series in chartData.series)
            {
                foreach (var data in series.data)
                {
                    data.value = Random.Range(minValue, maxValue);
                }
            }
        }

        public void RegenerateSeriesDataWithSign()
        {
            if (chartData == null) return;
            foreach (var series in chartData.series)
            {
                foreach (var data in series.data)
                {
                    data.value = Random.Range(minValue, maxValue) * Mathf.Sign(data.value);
                }
            }
        }
    }
}