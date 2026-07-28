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
    public class SolidGauge : ChartBase
    {
        new ChartGridCircleInverted chartGrid;
        float ringWidth = 0.0f;
        float backgroundWidth = 0.0f;
        ChartGraphicRing[] ringList;

        Vector2 GetItemPos(int seriesIndex, float anchPos = 1.0f, float offset = 0.0f)
        {
            float r = ringList[seriesIndex].rectTransform.sizeDelta.x * 0.5f;
            float offsetAngle = (offset / r) * Mathf.Rad2Deg;
            float angle = Mathf.Lerp(ringList[seriesIndex].startAngle, ringList[seriesIndex].endAngle, anchPos) + offsetAngle;
            Vector2 cossin = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            Vector2 pos = ChartGraphic.RotateCW(Vector2.up, cossin) * r;
            return pos;
        }

        Vector2 GetItemPos(int seriesIndex, float angle)
        {
            float r = ringList[seriesIndex].rectTransform.sizeDelta.x * 0.5f;
            Vector2 cossin = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            Vector2 pos = ChartGraphic.RotateCW(Vector2.up, cossin) * r;
            return pos;
        }

        float GetItemRot(int seriesIndex, float anchPos = 1.0f, float offset = 0.0f)
        {
            float r = ringList[seriesIndex].rectTransform.sizeDelta.x * 0.5f;
            float offsetAngle = (offset / r) * Mathf.Rad2Deg;
            float angle = Mathf.Lerp(ringList[seriesIndex].startAngle, ringList[seriesIndex].endAngle, anchPos) + offsetAngle;
            return angle;
        }

        protected override void UpdateGrid()
        {
            if (chartGrid != null) return;

            int activeCount = 0;
            for (int i = 0; i < chartInfo.data.series.Count; ++i)
            {
                if (!chartInfo.data.series[i].show) continue;
                activeCount++;
            }

            base.chartGrid = chartGrid = new ChartGridCircleInverted();
            chartGrid.chartInfo = chartInfo;
            chartGrid.activeCount = activeCount;
            chartGrid.midGrid = false;
            chartGrid.circularGrid = true;
            chartGrid.semicircle = chartInfo.options.pane.semicircle;
            chartGrid.startAngle = chartInfo.options.pane.startAngle;
            chartGrid.endAngle = chartInfo.options.pane.endAngle;
            chartGrid.innerSize = chartInfo.options.pane.innerSize;
            chartGrid.outerSize = chartInfo.options.pane.outerSize;
            chartGrid.UpdateGrid();

            chartInfo.dataRect.anchoredPosition = chartInfo.centerOffset;
            chartInfo.labelRect.anchoredPosition = chartInfo.centerOffset;
        }

        protected override void UpdateItemMaterial()
        {
            ringWidth = Mathf.Clamp(chartInfo.options.plotOptions.solidGaugeOption.barWidth, 0.0f, chartGrid.unitWidth);
            backgroundWidth = Mathf.Clamp(chartInfo.options.plotOptions.solidGaugeOption.barBackgroundWidth, 0.0f, chartGrid.unitWidth);
            itemMat = new Material[2];
            itemMatFade = new Material[1];

            itemMat[0] = new Material(Resources.Load<Material>("Materials/Chart_VBlur"));
            itemMat[0].SetFloat("_Smoothness", Mathf.Clamp01(3.0f / ringWidth));
            
            itemMatFade[0] = new Material(itemMat[0]);
            itemMatFade[0].color = new Color(1.0f, 1.0f, 1.0f, fadeValue);

            if (chartInfo.options.plotOptions.solidGaugeOption.enableBarBackground)
            {
                itemMat[1] = new Material(Resources.Load<Material>("Materials/Chart_VBlur"));
                itemMat[1].SetFloat("_Smoothness", Mathf.Clamp01(3.0f / backgroundWidth));
            }
        }

        protected override void UpdateItems()
        {
            ringList = new ChartGraphicRing[chartInfo.data.series.Count];
            int counter = 0;
            for (int i = 0; i < chartInfo.data.series.Count; ++i)
            {
                if (!chartInfo.data.series[i].show || chartInfo.data.series[i].data.Count == 0 || !chartInfo.data.series[i].data[0].show || chartInfo.data.series[i].data[0].value < 0.0f) continue;
                RectTransform seriesRect = ChartHelper.CreateEmptyRect(chartInfo.data.series[i].name, chartInfo.dataRect);
                seriesRect.sizeDelta = new Vector2(chartInfo.chartSize.x, chartInfo.chartSize.x);
                seriesRect.SetAsFirstSibling();

                //ring
                float rSize = chartInfo.chartSize.y + chartGrid.unitWidth * (counter + 0.5f) * 2.0f;
                float r = (chartInfo.data.series[i].data[0].value - chartInfo.yAxisInfo.min) / chartInfo.yAxisInfo.span;
                ringList[i] = ChartHelper.CreateEmptyRect("Ring", seriesRect).gameObject.AddComponent<ChartGraphicRing>();
                ringList[i].material = itemMat[0];
                ringList[i].rectTransform.sizeDelta = new Vector2(rSize, rSize);
                ringList[i].color = chartInfo.GetSeriesColor(i);
                ringList[i].width = ringWidth + 1.5f;   //fill gap
                ringList[i].startAngle = chartGrid.startAngle;
                ringList[i].endAngle = Mathf.Lerp(chartGrid.startAngle, chartGrid.endAngle, r);
                ringList[i].RefreshBuffer();

                //background
                if (chartInfo.options.plotOptions.solidGaugeOption.enableBarBackground)
                {
                    ChartGraphicRing background = ChartHelper.CreateEmptyRect("Background", seriesRect).gameObject.AddComponent<ChartGraphicRing>();
                    background.transform.SetAsFirstSibling();
                    background.material = itemMat[1];
                    background.rectTransform.sizeDelta = new Vector2(rSize, rSize);
                    background.color = chartInfo.options.plotOptions.solidGaugeOption.barBackgroundColor;
                    background.width = backgroundWidth + 1.5f;   //fill gap
                    background.startAngle = chartGrid.startAngle;
                    background.endAngle = chartGrid.endAngle;
                }

                counter++;
            }
        }

        protected override void UpdateLabels()
        {
            EzChartText labelTemp = ChartHelper.CreateText("Label", transform, chartInfo.options.label.textOption, chartInfo.options.plotOptions.generalFont);
            labelTemp.rectTransform.sizeDelta = Vector2.zero;
            for (int i = 0; i < chartInfo.data.series.Count; ++i)
            {
                if (ringList[i] == null) continue;
                RectTransform seriesLabelRect = ChartHelper.CreateEmptyRect(chartInfo.data.series[i].name, chartInfo.labelRect, true);
                seriesLabelRect.SetAsFirstSibling();

                float angle = GetItemRot(i, chartInfo.options.label.anchoredPosition, chartInfo.options.label.offset);
                EzChartText label = Instantiate(labelTemp, seriesLabelRect);
                label.text = GetFormattedLabelText(i);
                label.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, -angle + chartInfo.options.label.rotation);
                label.rectTransform.anchoredPosition = GetItemPos(i, angle);
            }
            ChartHelper.Destroy(labelTemp.gameObject);
        }

        protected override int FindCategory()
        {
            if (chartGrid.semicircle && chartInfo.gridMousePos.y < 0.0f) return -1;
            int index = -1;
            if (chartInfo.gridMousePos.sqrMagnitude > 0.25f * chartInfo.chartSize.x * chartInfo.chartSize.x || chartInfo.gridMousePos.sqrMagnitude < 0.25f * chartInfo.chartSize.y * chartInfo.chartSize.y) return -1;
            int counter = 0;
            for (int i = 0; i < chartInfo.data.series.Count; ++i)
            {
                if (ringList[i] == null) continue;
                counter++;
                float dist = chartInfo.chartSize.y * 0.5f + chartGrid.unitWidth * counter;
                float dist_ = dist - chartGrid.unitWidth;
                if (chartInfo.gridMousePos.sqrMagnitude < dist * dist && chartInfo.gridMousePos.sqrMagnitude > dist_ * dist_)
                {
                    float range = ringList[i].endAngle - ringList[i].startAngle;
                    float startAngle = Mathf.Repeat(ringList[i].startAngle, 360.0001f);
                    if ((chartInfo.mouseAngle > startAngle && chartInfo.mouseAngle < startAngle + range) ||
                        (chartInfo.mouseAngle + 360.0f > startAngle && chartInfo.mouseAngle + 360.0f < startAngle + range)) { index = i; break; }
                }
            }
            return index;
        }

        protected override int FindSeries(int cate)
        {
            return cate;
        }

        protected override void HighlightCategory(int cate)
        {
            for (int i = 0; i < ringList.Length; ++i)
            {
                if (ringList[i] == null || i == cate) continue;
                ringList[i].material = itemMatFade[0];
            }
        }

        protected override void UnhighlightCategory(int cate)
        {
            for (int i = 0; i < ringList.Length; ++i)
            {
                if (ringList[i] == null || i == cate) continue;
                ringList[i].material = itemMat[0];
            }
        }

        protected override void HighlightSeries(int series)
        {

        }

        protected override void UnhighlightSeries(int series)
        {

        }

        protected override void UpdateTooltip(int cate, int series)
        {
            string tooltipText = GetFormattedHeaderText();
            if (tooltipText.Length > 0) tooltipText += "\n";
            tooltipText += GetFormattedPointText(cate);
            chartInfo.tooltip.SetText(tooltipText);

            //tooltipFollowMouse = false;
            //chartInfo.tooltip.SetPosition(GetItemPos(cate));
        }
    }
}