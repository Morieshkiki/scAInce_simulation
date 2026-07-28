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
    public class RoseChart : ChartBase
    {
        new ChartGridCircle chartGrid;
        float rInner, rRange;
        RoseChartBar[] barList;

        Vector2 GetItemPos(int seriesIndex, int cateIndex, float anchPos = 1.0f, float offset = 0.0f)
        {
            float rStart = rInner + rRange * barList[seriesIndex].data[cateIndex].y;
            float r = rStart + rRange * barList[seriesIndex].data[cateIndex].x * anchPos;
            Vector2 pos = barList[seriesIndex].GetDirection(cateIndex) * (r + offset);
            return pos;
        }

        float GetItemRadius(int seriesIndex, int cateIndex, float anchPos = 1.0f)
        {
            float rStart = rInner + rRange * barList[seriesIndex].data[cateIndex].y;
            float r = rStart + rRange * barList[seriesIndex].data[cateIndex].x * anchPos;
            return r;
        }

        float GetItemRot(int seriesIndex, int cateIndex)
        {
            float rot = chartGrid.unitWidth * (cateIndex + 0.5f);
            if (chartInfo.options.xAxis.reversed) { rot = Mathf.Repeat(-rot, 360.0001f); }
            return rot + barList[seriesIndex].offset;
        }

        protected override void UpdateGrid()
        {
            if (chartGrid != null) return;
            base.chartGrid = chartGrid = new ChartGridCircle();
            chartGrid.chartInfo = chartInfo;
            chartGrid.midGrid = false;
            chartGrid.circularGrid = true;
            chartGrid.innerSize = chartInfo.options.pane.innerSize;
            chartGrid.outerSize = chartInfo.options.pane.outerSize;
            chartGrid.UpdateGrid();

            rInner = chartInfo.chartSize.y * 0.5f;
            rRange = (chartInfo.chartSize.x - chartInfo.chartSize.y) * 0.5f;
        }

        protected override void UpdateItemMaterial()
        {
            itemMat = new Material[1];
            itemMatFade = new Material[1];

            float smoothness = Mathf.Clamp01(4.0f / (chartInfo.chartSize.x - chartInfo.chartSize.y));
            itemMat[0] = new Material(Resources.Load<Material>("Materials/Chart_VBlur"));
            itemMat[0].SetFloat("_Smoothness", smoothness);
            itemMatFade[0] = new Material(itemMat[0]);
            itemMatFade[0].color = new Color(1.0f, 1.0f, 1.0f, fadeValue);
        }

        protected override void UpdateItems()
        {
            float maxBarWidth = chartInfo.options.plotOptions.columnStacking == ColumnStacking.None ? chartGrid.unitWidth / chartInfo.data.series.Count : chartGrid.unitWidth;
            float barWidth = Mathf.Clamp(chartInfo.options.plotOptions.roseChartOption.barWidth, 0.0f, maxBarWidth);
            float barSpace = Mathf.Clamp(chartInfo.options.plotOptions.roseChartOption.itemSeparation, -barWidth * 0.5f, maxBarWidth - barWidth);
            float barUnit = barWidth + barSpace;

            float[] barOffset = new float[chartInfo.data.series.Count];
            if (chartInfo.options.plotOptions.columnStacking == ColumnStacking.None)
            {
                float offsetMin = 0.0f;
                for (int i = 0; i < chartInfo.data.series.Count; ++i)
                {
                    if (!chartInfo.data.series[i].show) continue;
                    offsetMin += barUnit;
                }
                offsetMin = -(offsetMin - barUnit) * 0.5f;
                int activeCount = 0;
                for (int i = 0; i < chartInfo.data.series.Count; ++i)
                {
                    if (!chartInfo.data.series[i].show) continue;
                    barOffset[i] = offsetMin + barUnit * activeCount;
                    activeCount++;
                }
            }
            else
            {
                for (int i = 0; i < chartInfo.data.series.Count; ++i) barOffset[i] = 0.0f;
            }

            barList = new RoseChartBar[chartInfo.data.series.Count];
            float[] stackValueList = chartInfo.options.plotOptions.columnStacking == ColumnStacking.None ? null : new float[chartInfo.data.categories.Count];
            for (int i = 0; i < chartInfo.data.series.Count; ++i)
            {
                if (!chartInfo.data.series[i].show) continue;
                RectTransform seriesRect = ChartHelper.CreateEmptyRect(chartInfo.data.series[i].name, chartInfo.dataRect);
                seriesRect.sizeDelta = new Vector2(chartInfo.chartSize.x, chartInfo.chartSize.x);
                seriesRect.SetAsFirstSibling();

                //bar
                barList[i] = ChartHelper.CreateEmptyRect("Bar", seriesRect, true).gameObject.AddComponent<RoseChartBar>();
                barList[i].material = itemMat[0];
                barList[i].color = chartInfo.GetSeriesColor(i);
                barList[i].width = barWidth;
                barList[i].offset = barOffset[i];
                barList[i].reverse = chartInfo.options.xAxis.reversed;
                barList[i].innerSize = chartInfo.options.pane.innerSize;
                barList[i].innerExtend = 1.0f;
                if (chartInfo.options.plotOptions.roseChartOption.colorByCategories) barList[i].barColors = chartInfo.options.plotOptions.dataColor;
                barList[i].data = new Vector2[chartInfo.data.categories.Count];
                barList[i].show = new bool[chartInfo.data.categories.Count];
                for (int j = 0; j < chartInfo.data.categories.Count; ++j)
                {
                    barList[i].show[j] = j < chartInfo.data.series[i].data.Count && chartInfo.data.series[i].data[j].show && chartInfo.data.series[i].data[j].value >= 0.0f;
                    if (barList[i].show[j]) barList[i].data[j] = GetDataRatio(i, j, stackValueList);
                }
                barList[i].RefreshBuffer();
            }
        }

        protected override void UpdateLabels()
        {
            EzChartText labelTemp = ChartHelper.CreateText("Label", transform, chartInfo.options.label.textOption, chartInfo.options.plotOptions.generalFont);
            labelTemp.rectTransform.sizeDelta = Vector2.zero;
            for (int i = 0; i < chartInfo.data.series.Count; ++i)
            {
                if (barList[i] == null) continue;
                RectTransform seriesLabelRect = ChartHelper.CreateEmptyRect(chartInfo.data.series[i].name, chartInfo.labelRect);
                seriesLabelRect.sizeDelta = new Vector2(chartInfo.chartSize.x, chartInfo.chartSize.x);
                seriesLabelRect.SetAsFirstSibling();

                for (int j = 0; j < chartInfo.data.categories.Count; ++j)
                {
                    if (!barList[i].show[j]) continue;
                    float rot = GetItemRot(i, j);
                    EzChartText label = Instantiate(labelTemp, seriesLabelRect);
                    label.text = GetFormattedLabelTextPos(i, j);
                    label.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, -rot + chartInfo.options.label.rotation);
                    label.rectTransform.anchoredPosition = GetItemPos(i, j, chartInfo.options.label.anchoredPosition, chartInfo.options.label.offset);
                }
            }
            ChartHelper.Destroy(labelTemp.gameObject);
        }

        protected override int FindSeries(int cate)
        {
            int index = -1;
            for (int i = 0; i < chartInfo.data.series.Count; ++i)
            {
                if (!chartInfo.data.series[i].show) continue;
                float rot = GetItemRot(i, cate) - barList[i].width * 0.5f;
                float distStart = GetItemRadius(i, cate, 0.0f);
                float dist = GetItemRadius(i, cate, 1.0f);
                if (chartInfo.mouseAngle > rot && chartInfo.mouseAngle < rot + barList[i].width &&
                    chartInfo.mousePos.sqrMagnitude > distStart * distStart && chartInfo.mousePos.sqrMagnitude < dist * dist)
                { index = i; break; }
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
                barList[i].material = null;
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
                    tooltipText += "\n" + GetFormattedPointTextPos(i, cate);
                }
                chartInfo.tooltip.SetText(tooltipText);
            }
            else
            {
                if (barList[series] != null && barList[series].show[cate])
                {
                    tooltipText += "\n" + GetFormattedPointTextPos(series, cate);
                }
                chartInfo.tooltip.SetText(tooltipText);

                tooltipFollowMouse = false;
                chartInfo.tooltip.SetPosition(GetItemPos(series, cate));
            }
        }
    }
}