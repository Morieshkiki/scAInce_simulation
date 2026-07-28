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
    public class BarChart : ChartBase
    {
        const int MAX_DATA_POINTS = 14000;

        float posOffset, posUnit;
        BarChartBar[] barList;

        Vector2 GetItemPos(int seriesIndex, int cateIndex, float anchPos = 1.0f, float offset = 0.0f)
        {
            float pos = posOffset + barList[seriesIndex].offset + posUnit * cateIndex;
            float h = chartInfo.chartSize.y * (barList[seriesIndex].data[cateIndex].y + barList[seriesIndex].data[cateIndex].x * anchPos);
            h = Mathf.Clamp(h, 0, chartInfo.chartSize.y);
            h += offset * Mathf.Sign(barList[seriesIndex].data[cateIndex].x);
            return new Vector2(pos, h);
        }

        Vector2 GetItemPosInverted(int seriesIndex, int cateIndex, float anchPos = 1.0f, float offset = 0.0f)
        {
            float pos = posOffset + barList[seriesIndex].offset + posUnit * cateIndex;
            float h = chartInfo.chartSize.x * (barList[seriesIndex].data[cateIndex].y + barList[seriesIndex].data[cateIndex].x * anchPos);
            h = Mathf.Clamp(h, 0, chartInfo.chartSize.x);
            h += offset * Mathf.Sign(barList[seriesIndex].data[cateIndex].x);
            return new Vector2(h, pos);
        }

        protected override void UpdateItemMaterial()
        {
            itemMat = new Material[1];
            if (chartInfo.options.plotOptions.barChartOption.barGradientStartIntensity > 0 || chartInfo.options.plotOptions.barChartOption.barGradientEndIntensity > 0)
            {
                itemMat[0] = new Material(Resources.Load<Material>("Materials/Chart_VGradient"));
                itemMat[0].SetColor("_StartColor", chartInfo.options.plotOptions.barChartOption.barGradientStartColor);
                itemMat[0].SetColor("_EndColor", chartInfo.options.plotOptions.barChartOption.barGradientEndColor);
                itemMat[0].SetFloat("_StartIntensity", chartInfo.options.plotOptions.barChartOption.barGradientStartIntensity);
                itemMat[0].SetFloat("_EndIntensity", chartInfo.options.plotOptions.barChartOption.barGradientEndIntensity);
            }
            else
            {
                itemMat[0] = new Material(Resources.Load<Material>("Materials/Chart_UI"));
            }
            itemMatFade = new Material[1];
            itemMatFade[0] = new Material(itemMat[0]);
            itemMatFade[0].color = new Color(1.0f, 1.0f, 1.0f, fadeValue);
        }

        protected override void UpdateGrid()
        {
            if (chartGrid != null) return;
            if (chartInfo.options.plotOptions.inverted) chartGrid = new ChartGridRectInverted();
            else chartGrid = new ChartGridRect();
            chartGrid.chartInfo = chartInfo;
            chartGrid.midGrid = false;
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

        protected override void UpdateItems()
        {
            float maxBarWidth = chartInfo.options.plotOptions.columnStacking == ColumnStacking.None ? chartGrid.unitWidth / chartInfo.data.series.Count : chartGrid.unitWidth;
            float barWidth = Mathf.Clamp(chartInfo.options.plotOptions.barChartOption.barWidth, 0.0f, maxBarWidth);
            float barSpace = Mathf.Clamp(chartInfo.options.plotOptions.barChartOption.itemSeparation, -barWidth * 0.5f, maxBarWidth - barWidth);
            float barUnit = barWidth + barSpace;

            float[] barOffset = new float[chartInfo.data.series.Count];
            if (chartInfo.options.plotOptions.columnStacking == ColumnStacking.None)
            {
                float offsetMin = 0.0f;
                for (int i = 0; i < chartInfo.data.series.Count; ++i)
                {
                    if (chartInfo.SkipSeries(i) || !chartInfo.data.series[i].show) continue;
                    offsetMin += barUnit;
                }
                offsetMin = -(offsetMin - barUnit) * 0.5f;
                int activeCount = 0;
                for (int i = 0; i < chartInfo.data.series.Count; ++i)
                {
                    if (chartInfo.SkipSeries(i) || !chartInfo.data.series[i].show) continue;
                    barOffset[i] = offsetMin + barUnit * activeCount;
                    activeCount++;
                }
            }
            else
            {
                for (int i = 0; i < chartInfo.data.series.Count; ++i) barOffset[i] = 0.0f;
            }

            BarChartBar background = null;
            barList = new BarChartBar[chartInfo.data.series.Count];
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
                //bar
                barList[i] = ChartHelper.CreateEmptyRect("Bar", seriesRect, true).gameObject.AddComponent<BarChartBar>();
                barList[i].material = itemMat[0];
                barList[i].color = chartInfo.GetSeriesColor(i);
                barList[i].width = barWidth;
                barList[i].offset = barOffset[i];
                barList[i].inverted = chartInfo.options.plotOptions.inverted;
                barList[i].reverse = chartInfo.options.xAxis.reversed ^ chartInfo.options.plotOptions.inverted;
                barList[i].start = chartInfo.xAxisInfo.min;
                barList[i].end = chartInfo.xAxisInfo.max;
                if (chartInfo.options.plotOptions.barChartOption.colorByCategories) barList[i].barColors = chartInfo.options.plotOptions.dataColor;
                barList[i].data = new Vector2[chartInfo.data.categories.Count];
                barList[i].show = new bool[chartInfo.data.categories.Count];
                for (int j = (int)chartInfo.xAxisInfo.min; j <= (int)chartInfo.xAxisInfo.max; ++j)
                {
                    barList[i].show[j] = j < chartInfo.data.series[i].data.Count && chartInfo.data.series[i].data[j].show;
                    if (barList[i].show[j]) barList[i].data[j] = GetDataRatio(i, j, stackValueList, stackValueListNeg);
                }
                if (batchCount > 0)
                {
                    barList[i].startIndex = 0;
                    barList[i].endIndex = MAX_DATA_POINTS - 1;
                    for (int n = 0; n < batchCount; ++n)
                    {
                        BarChartBar batchItem = Instantiate(barList[i], seriesRect);
                        batchItem.startIndex = MAX_DATA_POINTS * (n + 1);
                        batchItem.endIndex = batchItem.startIndex + MAX_DATA_POINTS < chartInfo.data.series[i].data.Count ? batchItem.startIndex + MAX_DATA_POINTS - 1 : chartInfo.data.series[i].data.Count - 1;
                    }
                }

                //background
                if (chartInfo.options.plotOptions.barChartOption.enableBarBackground &&
                    (chartInfo.options.plotOptions.columnStacking == ColumnStacking.None ||
                    (chartInfo.options.plotOptions.columnStacking != ColumnStacking.None && background == null)))
                {
                    background = ChartHelper.CreateEmptyRect("Background", seriesRect, true).gameObject.AddComponent<BarChartBar>();
                    background.transform.SetAsFirstSibling();
                    background.color = chartInfo.options.plotOptions.barChartOption.barBackgroundColor;
                    background.width = Mathf.Clamp(chartInfo.options.plotOptions.barChartOption.barBackgroundWidth, 0.0f, maxBarWidth);
                    background.offset = barList[i].offset;
                    background.inverted = barList[i].inverted;
                    background.reverse = barList[i].reverse;
                    background.data = new Vector2[chartInfo.data.categories.Count];
                    background.show = new bool[chartInfo.data.categories.Count];
                    for (int j = (int)chartInfo.xAxisInfo.min; j <= (int)chartInfo.xAxisInfo.max; ++j)
                    {
                        background.show[j] = true;
                        background.data[j] = Vector2.right;
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
                if (barList[i] == null) continue;
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
                        if (!barList[i].show[j]) continue;
                        EzChartText label = Instantiate(labelTemp, seriesLabelRect);
                        label.text = GetFormattedLabelText(i, j);
                        label.rectTransform.anchoredPosition = GetItemPosInverted(i, j, chartInfo.options.label.anchoredPosition, chartInfo.options.label.offset);
                    }
                }
                else
                {
                    for (int j = 0; j < chartInfo.data.categories.Count; ++j)
                    {
                        if (!barList[i].show[j]) continue;
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
            if (chartInfo.options.plotOptions.inverted)
            {
                for (int i = 0; i < chartInfo.data.series.Count; ++i)
                {
                    if (barList[i] == null || !chartInfo.data.series[i].show) continue;
                    Vector2 size = new Vector2(Mathf.Abs(barList[i].data[cate].x) * chartInfo.chartSize.x, barList[i].width);
                    Vector2 pos = new Vector2((barList[i].data[cate].y + barList[i].data[cate].x * 0.5f) * chartInfo.chartSize.x, posOffset + posUnit * cate);
                    pos.y += barList[i].offset;
                    Vector2 dir = chartInfo.gridMousePos - pos;
                    if (Mathf.Abs(dir.x) < size.x * 0.5f && Mathf.Abs(dir.y) < size.y * 0.5f) { index = i; break; }
                }
            }
            else
            {
                for (int i = 0; i < chartInfo.data.series.Count; ++i)
                {
                    if (barList[i] == null || !chartInfo.data.series[i].show) continue;
                    Vector2 size = new Vector2(barList[i].width, Mathf.Abs(barList[i].data[cate].x) * chartInfo.chartSize.y);
                    Vector2 pos = new Vector2(posOffset + posUnit * cate, (barList[i].data[cate].y + barList[i].data[cate].x * 0.5f) * chartInfo.chartSize.y);
                    pos.x += barList[i].offset;
                    Vector2 dir = chartInfo.gridMousePos - pos;
                    if (Mathf.Abs(dir.x) < size.x * 0.5f && Mathf.Abs(dir.y) < size.y * 0.5f) { index = i; break; }
                }
            }
            return index;
        }
        
        protected override void HighlightSeries(int series)
        {
            for (int i = 0; i < barList.Length; ++i)
            {
                if (barList[i] == null || i == series) continue;
                barList[i].material = itemMatFade[0];
            }
        }

        protected override void UnhighlightSeries(int series)
        {
            for (int i = 0; i < barList.Length; ++i)
            {
                if (barList[i] == null || i == series) continue;
                barList[i].material = itemMat[0];
            }
        }

        protected override void UpdateTooltip(int cate, int series)
        {
            string tooltipText = GetFormattedHeaderText(cate);
            if (chartInfo.options.tooltip.share)
            {
                for (int i = 0; i < chartInfo.data.series.Count; ++i)
                {
                    if (barList[i] == null || !barList[i].show[cate]) continue;
                    tooltipText += "\n" + GetFormattedPointText(i, cate);
                }
                chartInfo.tooltip.SetText(tooltipText);
            }
            else
            {
                if (barList[series] != null && barList[series].show[cate])
                {
                    tooltipText += "\n" + GetFormattedPointText(series, cate);
                }
                chartInfo.tooltip.SetText(tooltipText);

                tooltipFollowMouse = false;
                if (chartInfo.options.plotOptions.inverted) chartInfo.tooltip.SetPosition(GetItemPosInverted(series, cate) - chartInfo.GetPaddingChartSize() * 0.5f, Mathf.Sign(barList[series].data[cate].x));
                else chartInfo.tooltip.SetPosition(GetItemPos(series, cate) - chartInfo.GetPaddingChartSize() * 0.5f, Mathf.Sign(barList[series].data[cate].x));
            }
        }
    }
}