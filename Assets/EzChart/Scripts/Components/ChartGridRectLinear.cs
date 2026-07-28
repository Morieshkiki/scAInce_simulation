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
    public class ChartGridRectLinear : ChartGridRect
    {
        public override float GetMouseValueX()
        {
            float x = chartInfo.xAxisInfo.GetValue(chartInfo.gridMousePos.x / chartInfo.chartSize.x);
            return x;
        }

        public override void UpdateHighlight()
        {
            chartInfo.highlight = ChartHelper.CreateImage("Highlight", chartInfo.dataRect);
            chartInfo.highlight.transform.SetAsFirstSibling();
            chartInfo.highlight.color = chartInfo.options.plotOptions.itemHighlightColor;
            chartInfo.highlight.rectTransform.sizeDelta = new Vector2(chartInfo.options.yAxis.gridLineWidth * 2.0f, chartInfo.chartSize.y);
            chartInfo.highlight.gameObject.SetActive(false);
        }

        protected override void ComputeAxisInfoX()
        {
            chartInfo.xAxisInfo = new ChartAxisInfo();
            if (chartInfo.IsZooming() || !chartInfo.options.xAxis.autoAxisValues)
            {
                chartInfo.xAxisInfo.Compute(chartInfo.chartDataInfo.minX, chartInfo.chartDataInfo.maxX, chartInfo.options.xAxis.axisDivision);
            }
            else
            {
                chartInfo.xAxisInfo.Compute(chartInfo.chartDataInfo.minX, chartInfo.chartDataInfo.maxX, chartInfo.options.xAxis.axisDivision, false);
            }
            if (chartInfo.options.xAxis.labelNumericFormat != "") chartInfo.xAxisInfo.labelFormat = chartInfo.options.xAxis.labelNumericFormat;
        }

        protected override void UpdateYAxisGrid()
        {
            yGrid = ChartHelper.CreateEmptyRect("YGrid", chartInfo.gridRect, true).gameObject.AddComponent<ChartGraphicRect>();
            yGrid.color = chartInfo.options.yAxis.gridLineColor;
            yGrid.width = chartInfo.options.yAxis.gridLineWidth;
            yGrid.num = chartInfo.xAxisInfo.count;
            yGrid.inverted = chartInfo.options.plotOptions.inverted;
            yGrid.rectTransform.offsetMin = new Vector2(chartInfo.options.xAxis.minPadding, 0.0f);
            yGrid.rectTransform.offsetMax = new Vector2(-chartInfo.options.xAxis.maxPadding, 0.0f);
        }

        protected override void UpdateXAxisTick()
        {
            xTicks = ChartHelper.CreateEmptyRect("XTicks", chartInfo.gridRect).gameObject.AddComponent<ChartGraphicRect>();
            xTicks.color = chartInfo.options.xAxis.tickColor;
            xTicks.width = xTickSize.x;
            xTicks.num = chartInfo.xAxisInfo.count;
            xTicks.inverted = chartInfo.options.plotOptions.inverted;

            xTicks.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            xTicks.rectTransform.anchorMax = new Vector2(1.0f, 0.0f);
            xTicks.rectTransform.offsetMin = new Vector2(chartInfo.options.xAxis.minPadding, -xTickSize.y - xAxisWidth * 0.5f);
            xTicks.rectTransform.offsetMax = new Vector2(-chartInfo.options.xAxis.maxPadding, 0.0f);
            offsetMin.y += xTickSize.y;
        }

        protected override void UpdateXAxisLabel()
        {
            EzChartText labelTemp = ChartHelper.CreateText("XGridLabel", chartInfo.gridLabelRect, chartInfo.options.xAxis.labelOption, chartInfo.options.plotOptions.generalFont);
            labelTemp.rectTransform.sizeDelta = Vector2.zero;

            RectTransform xLabelRect = ChartHelper.CreateEmptyRect("XLabels", chartInfo.gridLabelRect);
            xLabelRect.anchorMin = new Vector2(0.0f, 0.0f);
            xLabelRect.anchorMax = new Vector2(1.0f, 0.0f);
            xLabelRect.offsetMin = new Vector2(chartInfo.options.xAxis.minPadding, 0.0f);
            xLabelRect.offsetMax = new Vector2(-chartInfo.options.xAxis.maxPadding, 0.0f);

            string[] categories = new string[(int)chartInfo.xAxisInfo.count + 1];
            float spacing = 1.0f / chartInfo.xAxisInfo.count;
            float maxWidth = 0.0f;
            string maxStr = "";
            for (int i = 0; i < categories.Length; ++i)
            {
                float value = chartInfo.xAxisInfo.min + chartInfo.xAxisInfo.interval * i;
                if (chartInfo.chartAxisMapper == null) categories[i] = chartInfo.GetValueString(value, chartInfo.xAxisInfo.labelFormat);
                else categories[i] = chartInfo.GetDateTimeString(value);
                categories[i] = chartInfo.options.xAxis.labelFormat.Replace("{value}", categories[i]);
                if (categories[i].Length > maxStr.Length) maxStr = categories[i];
            }
            EzChartText labelTmp = GameObject.Instantiate(labelTemp, chartInfo.gridLabelRect);
            labelTmp.text = maxStr;
            maxWidth = labelTmp.preferredWidth;
            ChartHelper.Destroy(labelTmp.gameObject);
            bool useLongLabel = maxWidth > unitWidth * 0.8f && chartInfo.options.xAxis.autoRotateLabel;

            float labelOffset = xTickSize.y + xAxisWidth * 0.5f + labelTemp.fontSize * 0.1f;
            labelTemp.rectTransform.anchoredPosition = new Vector2(0.0f, -labelOffset);
            if (useLongLabel)
            {
                labelTemp.alignment = ChartHelper.ConvertAlignment(TextAnchor.MiddleRight);
                labelTemp.rectTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, 45.0f);
            }
            else
            {
                labelTemp.alignment = ChartHelper.ConvertAlignment(TextAnchor.UpperCenter);
            }

            float labelSize = useLongLabel ? maxWidth * 0.8f : labelTemp.fontSize * 1.2f;
            labelSize = Mathf.Clamp(labelSize, 0.0f, chartInfo.chartSize.y * 0.5f - offsetMin.y);
            maxWidth = useLongLabel ? labelSize * 1.414f : unitWidth * 0.9f;

            int interval = Mathf.RoundToInt(chartInfo.options.xAxis.interval);
            if (interval < 1) interval = Mathf.CeilToInt(labelTemp.fontSize * 1.25f / unitWidth + 0.0001f);
            if (xTicks != null) xTicks.interval = interval;
            if (yGrid != null) yGrid.interval = interval;
            maxWidth *= interval;

            for (int i = 0; i < categories.Length; i += interval)
            {
                int posIndex = chartInfo.options.xAxis.reversed ? categories.Length - i - 1 : i;
                EzChartText label = GameObject.Instantiate(labelTemp, xLabelRect);
                label.text = categories[i];
                label.rectTransform.anchorMin = new Vector2(spacing * posIndex, 0.0f);
                label.rectTransform.anchorMax = new Vector2(spacing * posIndex, 0.0f);
                if (label.preferredWidth > maxWidth) ChartHelper.TruncateText(label, maxWidth);
            }

            offsetMin.y += labelSize + labelTemp.fontSize * 0.2f;
            offsetMin.y = Mathf.Clamp(offsetMin.y, 0.0f, chartInfo.chartSize.y * 0.5f);

            ChartHelper.Destroy(labelTemp.gameObject);
        }
    }
}