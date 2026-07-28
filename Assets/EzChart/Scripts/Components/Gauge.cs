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
    public class Gauge : ChartBase
    {
        new ChartGridCircleInverted chartGrid;
        float ringWidth;
        Image pointer;

        protected override void UpdateItemMaterial()
        {

        }

        protected override void UpdateGrid()
        {
            if (chartGrid != null) return;

            base.chartGrid = chartGrid = new ChartGridCircleInverted();
            chartGrid.chartInfo = chartInfo;
            chartGrid.activeCount = 1;
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

        protected override void UpdateItems()
        {
            if (!chartInfo.data.series[0].show || chartInfo.data.series[0].data.Count == 0 || !chartInfo.data.series[0].data[0].show || chartInfo.data.series[0].data[0].value < 0.0f) return;
            
            //band
            if (chartInfo.options.plotOptions.gaugeOption.bands != null)
            {
                float smoothness = Mathf.Clamp01(3.0f / chartInfo.options.plotOptions.gaugeOption.bandWidth);
                float gridWidth = chartInfo.options.plotOptions.gaugeOption.bandWidth * (1 + smoothness);
                foreach (ChartOptions.BandOptions bandOpt in chartInfo.options.plotOptions.gaugeOption.bands)
                {
                    ChartGraphicRing band = ChartHelper.CreateEmptyRect("Band", chartInfo.gridRect).gameObject.AddComponent<ChartGraphicRing>();
                    band.transform.SetAsFirstSibling();
                    band.gameObject.AddComponent<ChartMaterialHandler>().Load("Materials/Chart_VBlur");
                    band.rectTransform.sizeDelta = new Vector2(chartInfo.chartSize.x - gridWidth, chartInfo.chartSize.x - gridWidth);
                    band.color = bandOpt.color;
                    band.width = gridWidth;
                    band.startAngle = Mathf.Lerp(chartGrid.startAngle, chartGrid.endAngle, bandOpt.from);
                    band.endAngle = Mathf.Lerp(chartGrid.startAngle, chartGrid.endAngle, bandOpt.to);
                    band.material.SetFloat("_Smoothness", smoothness);
                }
            }

            //pointer
            float r = (chartInfo.data.series[0].data[0].value - chartInfo.yAxisInfo.min) / chartInfo.yAxisInfo.span;
            float angle = Mathf.Lerp(chartGrid.startAngle, chartGrid.endAngle, r);
            Vector2 pSize = new Vector2(chartInfo.options.plotOptions.gaugeOption.pointerWidth, chartInfo.chartSize.x * 0.5f * chartInfo.options.plotOptions.gaugeOption.pointerLengthScale);

            Image background = ChartHelper.CreateImage("Pointer", chartInfo.gridRect);
            background.sprite = Resources.Load<Sprite>("Images/Chart_Circle_128x128");
            background.color = chartInfo.options.plotOptions.gaugeOption.pointerColor;
            background.rectTransform.sizeDelta = new Vector2(pSize.x, pSize.x) * 2.0f;

            pointer = ChartHelper.CreateImage("Pointer", chartInfo.dataRect);
            pointer.sprite = Resources.Load<Sprite>("Images/Chart_Pointer");
            pointer.type = Image.Type.Sliced;
            pointer.color = chartInfo.options.plotOptions.gaugeOption.pointerColor;
            pointer.rectTransform.sizeDelta = pSize;
            pointer.rectTransform.pivot = new Vector2(0.5f, 0.0f);
            pointer.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, -angle);
        }

        protected override void UpdateLabels()
        {
            EzChartText label = ChartHelper.CreateText("Label", chartInfo.labelRect, chartInfo.options.label.textOption, chartInfo.options.plotOptions.generalFont);
            label.rectTransform.sizeDelta = Vector2.zero;
            label.text = GetFormattedLabelText();
            label.rectTransform.anchoredPosition = new Vector2(0.0f, chartInfo.options.label.offset);
        }

        protected override int FindCategory()
        {
            if (chartGrid.semicircle && chartInfo.gridMousePos.y < 0.0f) return -1;
            if (chartInfo.gridMousePos.sqrMagnitude > 0.25f * chartInfo.chartSize.x * chartInfo.chartSize.x || chartInfo.gridMousePos.sqrMagnitude < 0.25f * chartInfo.chartSize.y * chartInfo.chartSize.y) return -1;
            return 0;
        }

        protected override int FindSeries(int cate)
        {
            return cate;
        }

        protected override void HighlightCategory(int cate)
        {

        }

        protected override void UnhighlightCategory(int cate)
        {

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
            tooltipText += GetFormattedPointText();
            chartInfo.tooltip.SetText(tooltipText);
        }
    }
}