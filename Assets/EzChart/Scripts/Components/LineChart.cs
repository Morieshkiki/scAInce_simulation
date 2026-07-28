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
    public class LineChart : ChartBase
    {
        protected const int MAX_DATA_POINTS = 14000;

        protected float posOffset, posUnit;
        protected LineChartPoint[] pointList;
        protected LineChartLine[] lineList;
        protected LineChartShade[] shadeList;

        Vector2 GetItemPos(int seriesIndex, int cateIndex, float anchPos = 1.0f, float offset = 0.0f)
        {
            float pos = posOffset + posUnit * cateIndex;
            float h = chartInfo.chartSize.y * (pointList[seriesIndex].data[cateIndex].y + pointList[seriesIndex].data[cateIndex].x * anchPos);
            h = Mathf.Clamp(h, 0, chartInfo.chartSize.y);
            h += offset * Mathf.Sign(pointList[seriesIndex].data[cateIndex].x);
            return new Vector2(pos, h);
        }

        Vector2 GetItemPosInverted(int seriesIndex, int cateIndex, float anchPos = 1.0f, float offset = 0.0f)
        {
            float pos = posOffset + posUnit * cateIndex;
            float h = chartInfo.chartSize.x * (pointList[seriesIndex].data[cateIndex].y + pointList[seriesIndex].data[cateIndex].x * anchPos);
            h = Mathf.Clamp(h, 0, chartInfo.chartSize.x);
            h += offset * Mathf.Sign(pointList[seriesIndex].data[cateIndex].x);
            return new Vector2(h, pos);
        }
        
        protected override void UpdateGrid()
        {
            if (chartGrid != null) return;
            if (chartInfo.options.plotOptions.inverted) chartGrid = new ChartGridRectInverted();
            else chartGrid = new ChartGridRect();
            chartGrid.chartInfo = chartInfo;
            chartGrid.midGrid = true;
            chartGrid.UpdateGrid();

            posOffset = 0.0f;
            posUnit = chartGrid.unitWidth;
            if (chartInfo.options.plotOptions.inverted)
            {
                if (!chartInfo.options.xAxis.reversed) { posUnit *= -1; posOffset = chartInfo.chartSize.y; }
            }
            else
            {
                if (chartInfo.options.xAxis.reversed) { posUnit *= -1; posOffset = chartInfo.chartSize.x; }
            }
            posOffset += posUnit * (0.5f - chartInfo.xAxisInfo.min);
        }

        protected override void UpdateItemMaterial()
        {
            itemMat = new Material[2];
            itemMatFade = new Material[3];

            if (chartInfo.options.plotOptions.lineChartOption.enablePointOutline)
            {
                itemMat[0] = new Material(Resources.Load<Material>("Materials/Chart_OutlineCircle"));
                itemMat[0].SetFloat("_Smoothness", Mathf.Clamp01(2.0f / chartInfo.options.plotOptions.lineChartOption.pointSize));
                itemMat[0].SetFloat("_OutlineWidth", Mathf.Clamp01(chartInfo.options.plotOptions.lineChartOption.pointOutlineWidth * 2.0f / chartInfo.options.plotOptions.lineChartOption.pointSize));
                itemMat[0].SetColor("_OutlineColor", chartInfo.options.plotOptions.lineChartOption.pointOutlineColor);
                itemMatFade[0] = new Material(itemMat[0]);
                itemMatFade[0].color = new Color(1.0f, 1.0f, 1.0f, fadeValue);
                itemMatFade[0].SetColor("_OutlineColor", chartInfo.options.plotOptions.lineChartOption.pointOutlineColor * fadeValue);
            }
            else
            {
                itemMat[0] = new Material(Resources.Load<Material>("Materials/Chart_Circle"));
                itemMat[0].SetFloat("_Smoothness", Mathf.Clamp01(3.0f / chartInfo.options.plotOptions.lineChartOption.pointSize));
                itemMatFade[0] = new Material(itemMat[0]);
                itemMatFade[0].color = new Color(1.0f, 1.0f, 1.0f, fadeValue);
            }
            if (chartInfo.options.plotOptions.lineChartOption.enableLine)
            {
                itemMat[1] = new Material(Resources.Load<Material>("Materials/Chart_UBlur"));
                itemMat[1].SetFloat("_Smoothness", Mathf.Clamp01(3.0f / chartInfo.options.plotOptions.lineChartOption.lineWidth));
                itemMatFade[1] = new Material(itemMat[1]);
                itemMatFade[1].color = new Color(1.0f, 1.0f, 1.0f, fadeValue);
            }
            if (chartInfo.options.plotOptions.lineChartOption.enableShade)
            {
                itemMatFade[2] = new Material(Resources.Load<Material>("Materials/Chart_UI"));
                itemMatFade[2].color = new Color(1.0f, 1.0f, 1.0f, fadeValue);
            }
        }

        protected override void UpdateItems()
        {
            pointList = new LineChartPoint[chartInfo.data.series.Count];
            if (chartInfo.options.plotOptions.lineChartOption.enableLine) lineList = chartInfo.options.plotOptions.lineChartOption.splineCurve ? new LineChartLineCurve[chartInfo.data.series.Count] : new LineChartLine[chartInfo.data.series.Count];
            if (chartInfo.options.plotOptions.lineChartOption.enableShade) shadeList = chartInfo.options.plotOptions.lineChartOption.splineCurve ? new LineChartShadeCurve[chartInfo.data.series.Count] : new LineChartShade[chartInfo.data.series.Count];
            float[] stackValueList = chartInfo.options.plotOptions.columnStacking == ColumnStacking.None ? null : new float[chartInfo.data.categories.Count];
            float[] stackValueListNeg = chartInfo.options.plotOptions.columnStacking == ColumnStacking.None ? null : new float[chartInfo.data.categories.Count];
            for (int i = 0; i < chartInfo.data.series.Count; ++i)
            {
                if (chartInfo.SkipSeries(i) || !chartInfo.data.series[i].show) continue;
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
                pointList[i].reverse = chartInfo.options.xAxis.reversed ^ chartInfo.options.plotOptions.inverted;
                pointList[i].start = chartInfo.xAxisInfo.min;
                pointList[i].end = chartInfo.xAxisInfo.max;
                pointList[i].data = new Vector2[chartInfo.data.categories.Count];
                pointList[i].show = new bool[chartInfo.data.categories.Count];
                for (int j = (int)chartInfo.xAxisInfo.min; j <= (int)chartInfo.xAxisInfo.max; ++j)
                {
                    pointList[i].show[j] = j < chartInfo.data.series[i].data.Count && chartInfo.data.series[i].data[j].show;
                    if (pointList[i].show[j]) pointList[i].data[j] = GetDataRatio(i, j, stackValueList, stackValueListNeg);
                }
                if (batchCount > 0)
                {
                    pointList[i].startIndex = 0;
                    pointList[i].endIndex = MAX_DATA_POINTS - 1;
                    for (int n = 0; n < batchCount; ++n)
                    {
                        LineChartPoint batchItem = Instantiate(pointList[i], seriesRect);
                        batchItem.dataX = null;
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
                    lineList[i].show = pointList[i].show;
                    lineList[i].start = pointList[i].start;
                    lineList[i].end = pointList[i].end;
                    if (batchCount > 0)
                    {
                        lineList[i].startIndex = 0;
                        lineList[i].endIndex = MAX_DATA_POINTS;
                        for (int n = 0; n < batchCount; ++n)
                        {
                            LineChartLine batchItem = Instantiate(lineList[i], seriesRect);
                            batchItem.dataX = null;
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
                    shadeList[i].show = pointList[i].show;
                    shadeList[i].start = pointList[i].start;
                    shadeList[i].end = pointList[i].end;
                    if (batchCount > 0)
                    {
                        shadeList[i].startIndex = 0;
                        shadeList[i].endIndex = MAX_DATA_POINTS;
                        for (int n = 0; n < batchCount; ++n)
                        {
                            LineChartShade batchItem = Instantiate(shadeList[i], seriesRect);
                            batchItem.dataX = null;
                            batchItem.startIndex = MAX_DATA_POINTS * (n + 1);
                            batchItem.endIndex = batchItem.startIndex + MAX_DATA_POINTS < chartInfo.data.series[i].data.Count ? batchItem.startIndex + MAX_DATA_POINTS : chartInfo.data.series[i].data.Count - 1;
                        }
                    }
                }
            }
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
                    for (int j = 0; j < chartInfo.data.categories.Count; ++j)
                    {
                        if (!pointList[i].show[j]) continue;
                        EzChartText label = Instantiate(labelTemp, seriesLabelRect);
                        label.text = GetFormattedLabelText(i, j);
                        label.rectTransform.anchoredPosition = GetItemPosInverted(i, j, chartInfo.options.label.anchoredPosition, chartInfo.options.label.offset);
                    }
                }
                else
                {
                    for (int j = 0; j < chartInfo.data.categories.Count; ++j)
                    {
                        if (!pointList[i].show[j]) continue;
                        EzChartText label = Instantiate(labelTemp, seriesLabelRect);
                        label.text = GetFormattedLabelText(i, j);
                        label.rectTransform.anchoredPosition = GetItemPos(i, j, chartInfo.options.label.anchoredPosition, chartInfo.options.label.offset);
                    }
                }
            }

            ChartHelper.Destroy(labelTemp.gameObject);
        }

        protected override int FindSeries(int cate)
        {
            int index = -1;
            float min = float.PositiveInfinity;
            if (chartInfo.options.plotOptions.inverted)
            {
                for (int i = 0; i < chartInfo.data.series.Count; ++i)
                {
                    if (pointList[i] == null || !pointList[i].show[cate]) continue;
                    Vector2 currPos = GetItemPosInverted(i, cate);
                    int dir = chartInfo.gridMousePos.y > currPos.y ? 1 : -1;
                    int nextCate = cate + dir;
                    if (nextCate < 0 || nextCate >= pointList[i].data.Length || !pointList[i].show[nextCate]) continue;
                    Vector2 nextPos = GetItemPosInverted(i, nextCate);

                    float t = Mathf.InverseLerp(currPos.y, nextPos.y, chartInfo.gridMousePos.y);
                    float h = Mathf.Lerp(currPos.x, nextPos.x, t);
                    float mouseDir = chartInfo.gridMousePos.x / chartInfo.chartSize.x - chartInfo.yAxisInfo.baseLineRatio;
                    if (mouseDir * pointList[i].data[cate].x < 0.0f) continue;
                    float dist = Mathf.Abs(chartInfo.gridMousePos.x - h);
                    if (dist < min) { index = i; min = dist; }
                }
            }
            else
            {
                for (int i = 0; i < chartInfo.data.series.Count; ++i)
                {
                    if (pointList[i] == null || !pointList[i].show[cate]) continue;
                    Vector2 currPos = GetItemPos(i, cate);
                    int dir = chartInfo.gridMousePos.x > currPos.x ? 1 : -1;
                    int nextCate = cate + dir;
                    if (nextCate < 0 || nextCate >= pointList[i].data.Length || !pointList[i].show[nextCate]) continue;
                    Vector2 nextPos = GetItemPos(i, nextCate);

                    float t = Mathf.InverseLerp(currPos.x, nextPos.x, chartInfo.gridMousePos.x);
                    float h = Mathf.Lerp(currPos.y, nextPos.y, t);
                    float mouseDir = chartInfo.gridMousePos.y / chartInfo.chartSize.y - chartInfo.yAxisInfo.baseLineRatio;
                    if (mouseDir * pointList[i].data[cate].x < 0.0f) continue;
                    float dist = Mathf.Abs(chartInfo.gridMousePos.y - h);
                    if (dist < min) { index = i; min = dist; }
                }
            }
            return index;
        }

        protected override void HighlightSeries(int series)
        {
            for (int i = 0; i < pointList.Length; ++i)
            {
                if (pointList[i] == null || i == series) continue;
                pointList[i].material = itemMatFade[0];
                if (chartInfo.options.plotOptions.lineChartOption.enableLine) lineList[i].material = itemMatFade[1];
                if (chartInfo.options.plotOptions.lineChartOption.enableShade) shadeList[i].material = itemMatFade[2];
            }
        }

        protected override void UnhighlightSeries(int series)
        {
            for (int i = 0; i < pointList.Length; ++i)
            {
                if (pointList[i] == null || i == series) continue;
                pointList[i].material = itemMat[0];
                if (chartInfo.options.plotOptions.lineChartOption.enableLine) lineList[i].material = itemMat[1];
                if (chartInfo.options.plotOptions.lineChartOption.enableShade) shadeList[i].material = null;
            }
        }

        protected override void UpdateTooltip(int cate, int series)
        {
            string tooltipText = GetFormattedHeaderText(cate);
            if (chartInfo.options.tooltip.share)
            {
                for (int i = 0; i < chartInfo.data.series.Count; ++i)
                {
                    if (pointList[i] == null || !pointList[i].show[cate]) continue;
                    tooltipText += "\n" + GetFormattedPointText(i, cate);
                }
                chartInfo.tooltip.SetText(tooltipText);
            }
            else
            {
                if (pointList[series] != null && pointList[series].show[cate])
                {
                    tooltipText += "\n" + GetFormattedPointText(series, cate);
                }
                chartInfo.tooltip.SetText(tooltipText);
                
                tooltipFollowMouse = false;
                if (chartInfo.options.plotOptions.inverted) chartInfo.tooltip.SetPosition(GetItemPosInverted(series, cate) - chartInfo.GetPaddingChartSize() * 0.5f, Mathf.Sign(pointList[series].data[cate].x));
                else chartInfo.tooltip.SetPosition(GetItemPos(series, cate) - chartInfo.GetPaddingChartSize() * 0.5f, Mathf.Sign(pointList[series].data[cate].x));
            }
        }
    }
}
