using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChartUtil
{
    public class ChartDataInfo
    {
        public float min = 0.0f;
        public float max = 0.0f;
        public float minSum = 0.0f;
        public float maxSum = 0.0f;
        public float minX = 0.0f;
        public float maxX = 0.0f;
        public float spanX = 0.0f;
        public float[] posSum = null;
        public float[] negSum = null;

        public void ComputeValueFirstData(ChartData data)
        {
            min = minSum = float.MaxValue;
            max = maxSum = float.MinValue;

            float pSum = 0.0f, nSum = 0.0f;
            for (int j = 0; j < data.series.Count; ++j)
            {
                if (!data.series[j].show || data.series[j].data.Count == 0 || !data.series[j].data[0].show) continue;

                float value = data.series[j].data[0].value;
                if (value >= 0.0f) pSum += value;
                else nSum += value;
                if (value < min) min = value;
                if (value > max) max = value;
            }

            if (pSum > maxSum) maxSum = pSum;
            if (nSum < minSum) minSum = nSum;
        }

        public void ComputeValue(ChartGeneralInfo info, bool doNeg = true, bool doZoom = false)
        {
            if (info.data.categories == null || info.data.categories.Count == 0) return;

            if (info.IsZooming())
            {
                minX = Mathf.Clamp(info.chartZoom.zoomStart, 0.0f, info.data.categories.Count - 1);
                maxX = Mathf.Clamp(info.chartZoom.zoomEnd, info.chartZoom.zoomStart, info.data.categories.Count - 1);
            }
            else if (doZoom && !info.options.xAxis.autoAxisValues)
            {
                minX = Mathf.Clamp(info.options.xAxis.min, 0.0f, info.data.categories.Count - 1);
                maxX = Mathf.Clamp(info.options.xAxis.max, info.options.xAxis.min, info.data.categories.Count - 1);
            }
            else
            {
                minX = 0.0f;
                maxX = info.data.categories.Count - 1;
            }

            min = minSum = float.MaxValue;
            max = maxSum = float.MinValue;
            posSum = new float[info.data.categories.Count];
            if (doNeg) negSum = new float[info.data.categories.Count];
            for (int i = (int)minX; i <= maxX; ++i)
            {
                float pSum = 0.0f, nSum = 0.0f;
                for (int j = 0; j < info.data.series.Count; ++j)
                {
                    if (!info.data.series[j].show || info.data.series[j].data.Count <= i || !info.data.series[j].data[i].show) continue;

                    float value = info.data.series[j].data[i].value;
                    if (value >= 0.0f) pSum += value;
                    else nSum += value;
                    if (value < min) min = value;
                    if (value > max) max = value;
                }

                if (pSum > maxSum) maxSum = pSum;
                if (nSum < minSum) minSum = nSum;
                posSum[i] = pSum;
                if (negSum != null) negSum[i] = -nSum;
            }

            spanX = maxX - minX;
        }

        public void ComputeValueLinear(ChartGeneralInfo info)
        {
            bool isZooming = info.IsZooming();
            if (isZooming || !info.options.xAxis.autoAxisValues)
            {
                if (isZooming)
                {
                    minX = info.chartZoom.zoomStart;
                    maxX = info.chartZoom.zoomEnd > minX ? info.chartZoom.zoomEnd : minX;
                }
                else
                {
                    minX = info.options.xAxis.min;
                    maxX = info.options.xAxis.max > minX ? info.options.xAxis.max : minX;
                }
                min = float.MaxValue;
                max = float.MinValue;
                for (int i = 0; i < info.data.series.Count; ++i)
                {
                    if (!info.data.series[i].show) continue;
                    for (int j = 0; j < info.data.series[i].data.Count; ++j)
                    {
                        if (!info.data.series[i].data[j].show) continue;
                        float x = info.data.series[i].data[j].x;
                        if (x < minX || x > maxX) continue;
                        float value = info.data.series[i].data[j].value;
                        if (value < min) min = value;
                        if (value > max) max = value;
                    }
                }
            }
            else
            {
                min = minX = float.MaxValue;
                max = maxX = float.MinValue;
                for (int i = 0; i < info.data.series.Count; ++i)
                {
                    if (!info.data.series[i].show) continue;
                    for (int j = 0; j < info.data.series[i].data.Count; ++j)
                    {
                        if (!info.data.series[i].data[j].show) continue;
                        float value = info.data.series[i].data[j].value;
                        float x = info.data.series[i].data[j].x;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (value < min) min = value;
                        if (value > max) max = value;
                    }
                }
            }
            spanX = maxX - minX;
        }
    }

    public class ChartAxisInfo
    {
        public float min = 0.0f;
        public float max = 1.0f;
        public float count = 1;
        public float span = 1.0f;
        public float interval = 1.0f;
        public float baseLine = 0.0f;
        public float baseLineRatio = 0.0f;
        public string labelFormat = "N0";

        public float GetValue(float ratio)
        {
            return span * ratio + min;
        }

        public void Compute(float minValue, float maxValue, int division)
        {
            if (minValue >= maxValue || division <= 0) return;

            min = minValue;
            max = maxValue;
            count = division;
            span = max - min;
            interval = span / count;

            if (interval >= 1.0f) labelFormat = "N0";
            else labelFormat = "N" + ChartHelper.FindFloatDisplayPrecision(interval).ToString();

            baseLine = 0.0f;
            if (min >= 0.0f) baseLine = min;
            else if (max <= 0.0f) baseLine = max;
            baseLineRatio = (baseLine - min) / span;
        }

        public void Compute(float minValue, float maxValue, int division, bool zeroBased)
        {
            if (minValue > maxValue) return;

            if (zeroBased) { if (minValue > 0.0f) minValue = 0.0f; if (maxValue < 0.0f) maxValue = 0.0f; }

            count = division >= 1 ? division : 1;
            interval = (maxValue - minValue) / count;
            if (interval >= 1.0f)
            {
                int i = Mathf.CeilToInt(interval);
                int l = ChartHelper.FindIntegerLength(i);
                int unit = (int)Mathf.Pow(10, l - 1);
                int r = i % unit;
                i = i - r;
                if (r > (unit / 2)) i += unit;
                interval = i;
            }
            else
            {
                float l = Mathf.Pow(10, ChartHelper.FindFloatDisplayPrecision(interval));
                interval = Mathf.Floor(interval * l) / l;
            }

            int minStep = Mathf.FloorToInt(minValue / interval);
            int maxStep = Mathf.CeilToInt(maxValue / interval);
            min = minStep * interval;
            max = maxStep * interval;
            count = maxStep - minStep;
            span = max - min;

            if (interval >= 1.0f) labelFormat = "N0";
            else labelFormat = "N" + ChartHelper.FindFloatDisplayPrecision(interval).ToString();

            baseLine = 0.0f;
            if (min >= 0.0f) baseLine = min;
            else if (max <= 0.0f) baseLine = max;
            baseLineRatio = (baseLine - min) / span;
        }
    }

    public class ChartGeneralInfo
    {
        public ChartType type;
        public ChartOptions options;
        public ChartData data;
        public bool isLinear = false;
        public System.Globalization.CultureInfo cultureInfo;

        public ChartDataInfo chartDataInfo;
        public ChartAxisInfo xAxisInfo;
        public ChartAxisInfo yAxisInfo;

        public RectTransform contentRect;
        public RectTransform legendRect;
        public RectTransform dataRect;
        public RectTransform labelRect;
        public RectTransform gridRect;
        public RectTransform gridLabelRect;

        public ChartTooltip tooltip;
        public Image highlight;
        public Image background;

        public ChartEvents chartEvents;
        public ChartLinearAxisDateTimeMapper chartAxisMapper;
        public ChartMixer chartMixer;
        public ChartZoom chartZoom;

        public Vector2 chartSize;
        public Vector2 centerOffset;
        public Vector2 mousePos;
        public Vector2 gridMousePos;
        public float mouseAngle;
        public List<int> skipSeries;

        public bool SkipSeries(int index)
        {
            return skipSeries != null && skipSeries.Contains(index);
        }

        public Color GetSeriesColor(int seriesIndex)
        {
            int colorIndex = data.series[seriesIndex].colorIndex > 0 ? data.series[seriesIndex].colorIndex : seriesIndex;
            return options.plotOptions.dataColor[colorIndex % options.plotOptions.dataColor.Length];
        }

        public Vector2 GetPadding()
        {
            return
                new Vector2(options.xAxis.maxPadding, options.yAxis.maxPadding) -
                new Vector2(options.xAxis.minPadding, options.yAxis.minPadding);
        }

        public Vector2 GetPaddingChartSize()
        {
            return chartSize +
                new Vector2(options.xAxis.maxPadding, options.yAxis.maxPadding) -
                new Vector2(options.xAxis.minPadding, options.yAxis.minPadding);
        }

        public float GetXValue(float pos, bool inverted)
        {
            float length = inverted ? chartSize.y : chartSize.x;
            return pos / length * xAxisInfo.span + xAxisInfo.min;
        }

        public bool IsZooming()
        {
            return chartZoom != null && chartZoom.isZooming;
        }

        public string GetValueString(float value, string format)
        {
            if (format == "") format = ChartHelper.FindFloatDisplayFormat(value);
            if(format == "Time")//modified. Added ability to use time formatting
            {
                System.TimeSpan time = System.TimeSpan.FromSeconds(value);
                return time.ToString(@"hh\:mm\:ss");
            }
            return value.ToString(format, cultureInfo);
        }

        public string GetPercentageString(float value, string format)
        {
            if (format == "") format = "f0";
            return (value * 100).ToString(format, cultureInfo) + "%";
        }

        public string GetDateTimeString(float value)
        {
            return GetDateTimeString(chartAxisMapper.ValueToTicks(value));
        }

        public string GetDateTimeString(long ticks)
        {
            System.DateTime dt = new System.DateTime(ticks);
            return dt.ToString(chartAxisMapper.dateTimeFormat, cultureInfo);
        }
    }
}
