using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if CHART_TMPRO
using TMPro;
using EzChartText = TMPro.TextMeshProUGUI;
#else
using EzChartText = UnityEngine.UI.Text;
#endif

namespace ChartUtil
{
    [ExecuteInEditMode]
    public class LineChartLinear : LineChart
    {
        List<int> trackingList = new List<int>();
        SortedDictionary<int, int[]> cateDict = new SortedDictionary<int, int[]>(); //pos and series index

        Vector2 GetItemPos(int seriesIndex, int cateIndex, float anchPos = 1.0f, float offset = 0.0f)
        {
            float pos = posOffset + chartInfo.chartSize.x * pointList[seriesIndex].dataX[cateIndex] * posUnit;
            float h = chartInfo.chartSize.y * (pointList[seriesIndex].data[cateIndex].y + pointList[seriesIndex].data[cateIndex].x * anchPos);
            h = Mathf.Clamp(h, 0, chartInfo.chartSize.y);
            h += offset * Mathf.Sign(pointList[seriesIndex].data[cateIndex].x);
            return new Vector2(pos, h);
        }

        Vector2 GetItemPosInverted(int seriesIndex, int cateIndex, float anchPos = 1.0f, float offset = 0.0f)
        {
            float pos = posOffset + chartInfo.chartSize.y * pointList[seriesIndex].dataX[cateIndex] * posUnit;
            float h = chartInfo.chartSize.x * (pointList[seriesIndex].data[cateIndex].y + pointList[seriesIndex].data[cateIndex].x * anchPos);
            h = Mathf.Clamp(h, 0, chartInfo.chartSize.x);
            h += offset * Mathf.Sign(pointList[seriesIndex].data[cateIndex].x);
            return new Vector2(h, pos);
        }

        protected override void UpdateGrid()
        {
            if (chartGrid != null) return;
            if (chartInfo.options.plotOptions.inverted) chartGrid = new ChartGridRectLinearInverted();
            else chartGrid = new ChartGridRectLinear();
            chartGrid.chartInfo = chartInfo;
            chartGrid.midGrid = true;
            chartGrid.UpdateGrid();

            posOffset = 0.0f;
            posUnit = 1.0f;
            if (chartInfo.options.plotOptions.inverted)
            {
                if (chartInfo.options.xAxis.reversed) { posUnit *= -1; posOffset = chartInfo.chartSize.y; }
            }
            else
            {
                if (chartInfo.options.xAxis.reversed) { posUnit *= -1; posOffset = chartInfo.chartSize.x; }
            }
            //posOffset += posUnit * 0.5f;
        }

        protected override void UpdateItems()
        {
            pointList = new LineChartPoint[chartInfo.data.series.Count];
            if (chartInfo.options.plotOptions.lineChartOption.enableLine) lineList = chartInfo.options.plotOptions.lineChartOption.splineCurve ? new LineChartLineCurve[chartInfo.data.series.Count] : new LineChartLine[chartInfo.data.series.Count];
            if (chartInfo.options.plotOptions.lineChartOption.enableShade) shadeList = chartInfo.options.plotOptions.lineChartOption.splineCurve ? new LineChartShadeCurve[chartInfo.data.series.Count] : new LineChartShade[chartInfo.data.series.Count];
            for (int i = 0; i < chartInfo.data.series.Count; ++i)
            {
                if (!chartInfo.data.series[i].show) continue;
                RectTransform seriesRect = ChartHelper.CreateEmptyRect(chartInfo.data.series[i].name, chartInfo.dataRect, true);
                seriesRect.SetAsFirstSibling();
                if (chartInfo.options.plotOptions.inverted)
                {
                    seriesRect.offsetMin = new Vector2(chartInfo.options.yAxis.minPadding, chartInfo.options.xAxis.minPadding);
                    seriesRect.offsetMax = new Vector2(-chartInfo.options.yAxis.maxPadding, -chartInfo.options.xAxis.maxPadding);
                }
                else
                {
                    seriesRect.offsetMin = new Vector2(chartInfo.options.xAxis.minPadding, chartInfo.options.yAxis.minPadding);
                    seriesRect.offsetMax = new Vector2(-chartInfo.options.xAxis.maxPadding, -chartInfo.options.yAxis.maxPadding);
                }

                int batchCount = chartInfo.data.series[i].data.Count / (MAX_DATA_POINTS + 1);
                //point
                pointList[i] = ChartHelper.CreateEmptyRect("Point", seriesRect, true).gameObject.AddComponent<LineChartPoint>();
                pointList[i].material = itemMat[0];
                pointList[i].color = chartInfo.GetSeriesColor(i);
                pointList[i].diameter = chartInfo.options.plotOptions.lineChartOption.pointSize;
                pointList[i].inverted = chartInfo.options.plotOptions.inverted;
                pointList[i].reverse = chartInfo.options.xAxis.reversed;
                pointList[i].data = new Vector2[chartInfo.data.series[i].data.Count];
                pointList[i].dataX = new float[chartInfo.data.series[i].data.Count];
                pointList[i].show = new bool[chartInfo.data.series[i].data.Count];
                for (int j = 0; j < chartInfo.data.series[i].data.Count; ++j)
                {
                    pointList[i].show[j] = chartInfo.data.series[i].data[j].show;
                    if (!pointList[i].show[j]) continue;
                    pointList[i].data[j] = GetDataRatio(i, j);
                    pointList[i].dataX[j] = (chartInfo.data.series[i].data[j].x - chartInfo.xAxisInfo.min) / chartInfo.xAxisInfo.span;
                }
                if (batchCount > 0)
                {
                    pointList[i].startIndex = 0;
                    pointList[i].endIndex = MAX_DATA_POINTS - 1;
                    for (int n = 0; n < batchCount; ++n)
                    {
                        LineChartPoint batchItem = Instantiate(pointList[i], seriesRect);
                        batchItem.startIndex = MAX_DATA_POINTS * (n + 1);
                        batchItem.endIndex = batchItem.startIndex + MAX_DATA_POINTS < chartInfo.data.series[i].data.Count ? batchItem.startIndex + MAX_DATA_POINTS - 1 : chartInfo.data.series[i].data.Count - 1;
                    }
                }

                //line
                if (chartInfo.options.plotOptions.lineChartOption.enableLine)
                {
                    lineList[i] = chartInfo.options.plotOptions.lineChartOption.splineCurve ?
                        ChartHelper.CreateEmptyRect("Line", seriesRect, true).gameObject.AddComponent<LineChartLineCurve>() :
                        ChartHelper.CreateEmptyRect("Line", seriesRect, true).gameObject.AddComponent<LineChartLine>();
                    lineList[i].transform.SetAsFirstSibling();
                    lineList[i].material = itemMat[1];
                    lineList[i].color = pointList[i].color;
                    lineList[i].width = chartInfo.options.plotOptions.lineChartOption.lineWidth;
                    lineList[i].inverted = pointList[i].inverted;
                    lineList[i].reverse = pointList[i].reverse;
                    lineList[i].data = pointList[i].data;
                    lineList[i].dataX = pointList[i].dataX;
                    lineList[i].show = pointList[i].show;
                    if (batchCount > 0)
                    {
                        lineList[i].startIndex = 0;
                        lineList[i].endIndex = MAX_DATA_POINTS;
                        for (int n = 0; n < batchCount; ++n)
                        {
                            LineChartLine batchItem = Instantiate(lineList[i], seriesRect);
                            batchItem.startIndex = MAX_DATA_POINTS * (n + 1);
                            batchItem.endIndex = batchItem.startIndex + MAX_DATA_POINTS < chartInfo.data.series[i].data.Count ? batchItem.startIndex + MAX_DATA_POINTS : chartInfo.data.series[i].data.Count - 1;
                        }
                    }
                }

                //shade
                if (chartInfo.options.plotOptions.lineChartOption.enableShade)
                {
                    shadeList[i] = chartInfo.options.plotOptions.lineChartOption.splineCurve ?
                        ChartHelper.CreateEmptyRect("Shade", seriesRect, true).gameObject.AddComponent<LineChartShadeCurve>() :
                        ChartHelper.CreateEmptyRect("Shade", seriesRect, true).gameObject.AddComponent<LineChartShade>();
                    shadeList[i].transform.SetAsFirstSibling();
                    shadeList[i].color = new Color(pointList[i].color.r, pointList[i].color.g, pointList[i].color.b, chartInfo.options.plotOptions.lineChartOption.shadeOpacity);
                    shadeList[i].inverted = pointList[i].inverted;
                    shadeList[i].reverse = pointList[i].reverse;
                    shadeList[i].data = pointList[i].data;
                    shadeList[i].dataX = pointList[i].dataX;
                    shadeList[i].show = pointList[i].show;
                    if (batchCount > 0)
                    {
                        shadeList[i].startIndex = 0;
                        shadeList[i].endIndex = MAX_DATA_POINTS;
                        for (int n = 0; n < batchCount; ++n)
                        {
                            LineChartShade batchItem = Instantiate(shadeList[i], seriesRect);
                            batchItem.startIndex = MAX_DATA_POINTS * (n + 1);
                            batchItem.endIndex = batchItem.startIndex + MAX_DATA_POINTS < chartInfo.data.series[i].data.Count ? batchItem.startIndex + MAX_DATA_POINTS : chartInfo.data.series[i].data.Count - 1;
                        }
                    }
                }
            }
            if (chartInfo.options.plotOptions.mouseTracking) UpdateTrackingInfo();
        }

        protected override void UpdateLabels()
        {
            //template
            EzChartText labelTemp = ChartHelper.CreateText("Label", transform, chartInfo.options.label.textOption, chartInfo.options.plotOptions.generalFont);
            labelTemp.rectTransform.anchorMin = Vector2.zero;
            labelTemp.rectTransform.anchorMax = Vector2.zero;
            labelTemp.rectTransform.sizeDelta = Vector2.zero;
            labelTemp.rectTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, chartInfo.options.label.rotation);

            for (int i = 0; i < chartInfo.data.series.Count; ++i)
            {
                if (pointList[i] == null) continue;
                RectTransform seriesLabelRect = ChartHelper.CreateEmptyRect(chartInfo.data.series[i].name, chartInfo.labelRect, true);
                seriesLabelRect.SetAsFirstSibling();
                if (chartInfo.options.plotOptions.inverted)
                {
                    seriesLabelRect.offsetMin = new Vector2(chartInfo.options.yAxis.minPadding, chartInfo.options.xAxis.minPadding);
                    seriesLabelRect.offsetMax = new Vector2(-chartInfo.options.yAxis.maxPadding, -chartInfo.options.xAxis.maxPadding);
                }
                else
                {
                    seriesLabelRect.offsetMin = new Vector2(chartInfo.options.xAxis.minPadding, chartInfo.options.yAxis.minPadding);
                    seriesLabelRect.offsetMax = new Vector2(-chartInfo.options.xAxis.maxPadding, -chartInfo.options.yAxis.maxPadding);
                }

                if (chartInfo.options.plotOptions.inverted)
                {
                    for (int j = 0; j < chartInfo.data.series[i].data.Count; ++j)
                    {
                        if (!pointList[i].show[j]) continue;
                        Vector2 pos = GetItemPosInverted(i, j, chartInfo.options.label.anchoredPosition, chartInfo.options.label.offset);
                        if (pos.y < 0.0f || pos.y > chartInfo.chartSize.y) continue;
                        EzChartText label = Instantiate(labelTemp, seriesLabelRect);
                        label.text = GetFormattedLabelTextLinear(i, j);
                        label.rectTransform.anchoredPosition = pos;
                    }
                }
                else
                {
                    for (int j = 0; j < chartInfo.data.series[i].data.Count; ++j)
                    {
                        if (!pointList[i].show[j]) continue;
                        Vector2 pos = GetItemPos(i, j, chartInfo.options.label.anchoredPosition, chartInfo.options.label.offset);
                        if (pos.x < 0.0f || pos.x > chartInfo.chartSize.x) continue;
                        EzChartText label = Instantiate(labelTemp, seriesLabelRect);
                        label.text = GetFormattedLabelTextLinear(i, j);
                        label.rectTransform.anchoredPosition = pos;
                    }
                }
            }
            ChartHelper.Destroy(labelTemp.gameObject);
        }

        protected override int FindCategory()
        {
            int index = -1;
            if (chartInfo.options.plotOptions.inverted)
            {
                if (chartInfo.gridMousePos.y < 0 || chartInfo.gridMousePos.y > chartInfo.chartSize.y) return -1;
                index = Mathf.RoundToInt(chartInfo.gridMousePos.y);
            }
            else
            {
                if (chartInfo.gridMousePos.x < 0 || chartInfo.gridMousePos.x > chartInfo.chartSize.x) return -1;
                index = Mathf.RoundToInt(chartInfo.gridMousePos.x);
            }
            return index;
        }

        protected override int FindSeries(int cate)
        {
            if (cate < 0 || trackingList.Count <= cate)
                return -1;

            int index = -1;
            float min = float.PositiveInfinity;
            if (chartInfo.options.plotOptions.inverted)
            {
                for (int i = 0; i < chartInfo.data.series.Count; ++i)
                {
                    int cateIndex = cateDict[trackingList[cate]][i];
                    if (cateIndex < 0) continue;
                    float h = chartInfo.chartSize.x * (pointList[i].data[cateIndex].y + pointList[i].data[cateIndex].x);
                    float dif = Mathf.Abs(chartInfo.gridMousePos.x - h);
                    if (dif < min) { min = dif; index = i; }
                }
            }
            else
            {
                for (int i = 0; i < chartInfo.data.series.Count; ++i)
                {
                    int cateIndex = cateDict[trackingList[cate]][i];
                    if (cateIndex < 0) continue;
                    float h = chartInfo.chartSize.y * (pointList[i].data[cateIndex].y + pointList[i].data[cateIndex].x);
                    float dif = Mathf.Abs(chartInfo.gridMousePos.y - h);
                    if (dif < min) { min = dif; index = i; }
                }
            }
            return index;
        }

        public int GetCategory(int cate, int series)
        {
            if (cate < 0) return -1;
            return cateDict[trackingList[cate]][series];
        }

        protected override void HighlightCategory(int cate)
        {
            if (cate < 0 || cate >= trackingList.Count)
                return;
            if (chartInfo.options.plotOptions.inverted)
            {
                Vector2 offset = -0.5f * (new Vector2(0.0f, chartInfo.chartSize.y) + chartInfo.GetPadding());
                chartInfo.highlight.transform.localPosition = new Vector2(offset.x, trackingList[cate] + offset.y);
            }
            else
            {
                Vector2 offset = -0.5f * (new Vector2(chartInfo.chartSize.x, 0.0f) + chartInfo.GetPadding());
                chartInfo.highlight.transform.localPosition = new Vector2(trackingList[cate] + offset.x, offset.y);
            }
            chartInfo.highlight.gameObject.SetActive(true);
        }

        protected override void UnhighlightCategory(int series)
        {
            chartInfo.highlight.gameObject.SetActive(false);
        }

        protected override void UpdateTooltip(int cate, int series)
        {
            int cateIndex = cateDict[trackingList[cate]][series];
            if (cateIndex < 0) return;
            float x = chartInfo.data.series[series].data[cateIndex].x;
            string tooltipText = chartInfo.options.tooltip.headerFormat.Replace("{category}", 
                chartInfo.chartAxisMapper == null ? chartInfo.GetValueString(x, chartInfo.xAxisInfo.labelFormat) : chartInfo.GetDateTimeString(x));
            tooltipText += "\n" + GetFormattedPointTextLinear(series, cateIndex);
            chartInfo.tooltip.SetText(tooltipText);

            tooltipFollowMouse = false;
            if (chartInfo.options.plotOptions.inverted) chartInfo.tooltip.SetPosition(GetItemPosInverted(series, cateIndex) - chartInfo.GetPaddingChartSize() * 0.5f, Mathf.Sign(pointList[series].data[cateIndex].x));
            else chartInfo.tooltip.SetPosition(GetItemPos(series, cateIndex) - chartInfo.GetPaddingChartSize() * 0.5f, Mathf.Sign(pointList[series].data[cateIndex].x));
        }

        void UpdateTrackingInfo()
        {
            if (chartInfo.options.plotOptions.inverted)
            {
                //caculate cate dictionary
                for (int i = 0; i < chartInfo.data.series.Count; ++i)
                {
                    if (!chartInfo.data.series[i].show) continue;

                    for (int j = 0; j < chartInfo.data.series[i].data.Count; ++j)
                    {
                        if (!pointList[i].show[j]) continue;
                        int pos = (int)(posOffset + chartInfo.chartSize.y * pointList[i].dataX[j] * posUnit);
                        if (!cateDict.ContainsKey(pos))
                        {
                            int[] info = new int[chartInfo.data.series.Count];
                            for (int k = 0; k < info.Length; ++k) info[k] = -1;
                            cateDict.Add(pos, info);
                        }
                        cateDict[pos][i] = j;
                    }
                }

                //add tracking list
                int counter = 0;
                List<int> cateKeys = new List<int>(cateDict.Keys);
                for (int i = 0; i < cateKeys.Count - 1; ++i)
                {
                    float mid = (cateKeys[i] + cateKeys[i + 1]) * 0.5f;
                    while (counter < cateKeys[i + 1])
                    {
                        if (counter < mid) trackingList.Add(cateKeys[i]);
                        else trackingList.Add(cateKeys[i + 1]);
                        counter++;
                    }
                }
                if (cateKeys.Count > 0)
                {
                    while (counter < chartInfo.chartSize.y)
                    {
                        trackingList.Add(cateKeys[cateKeys.Count - 1]);
                        counter++;
                    }
                }
            }
            else
            {
                //caculate cate dictionary
                for (int i = 0; i < chartInfo.data.series.Count; ++i)
                {
                    if (!chartInfo.data.series[i].show) continue;

                    for (int j = 0; j < chartInfo.data.series[i].data.Count; ++j)
                    {
                        if (!pointList[i].show[j]) continue;
                        int pos = (int)(posOffset + chartInfo.chartSize.x * pointList[i].dataX[j] * posUnit);
                        if (!cateDict.ContainsKey(pos))
                        {
                            int[] info = new int[chartInfo.data.series.Count];
                            for (int k = 0; k < info.Length; ++k) info[k] = -1;
                            cateDict.Add(pos, info);
                        }
                        cateDict[pos][i] = j;
                    }
                }

                //add tracking list
                int counter = 0;
                List<int> cateKeys = new List<int>(cateDict.Keys);
                for (int i = 0; i < cateKeys.Count - 1; ++i)
                {
                    float mid = (cateKeys[i] + cateKeys[i + 1]) * 0.5f;
                    while (counter < cateKeys[i + 1])
                    {
                        if (counter < mid) trackingList.Add(cateKeys[i]);
                        else trackingList.Add(cateKeys[i + 1]);
                        counter++;
                    }
                }
                if (cateKeys.Count > 0)
                {
                    while (counter < chartInfo.chartSize.x)
                    {
                        trackingList.Add(cateKeys[cateKeys.Count - 1]);
                        counter++;
                    }
                }
            }
        }
    }
}

