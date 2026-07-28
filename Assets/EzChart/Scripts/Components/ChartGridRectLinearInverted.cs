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
    public class ChartGridRectLinearInverted : ChartGridRectInverted
    {
        public override float GetMouseValueX()
        {
            float x = chartInfo.xAxisInfo.GetValue(chartInfo.gridMousePos.y / chartInfo.chartSize.y);
            return x;
        }

        public override void UpdateHighlight()
        {
            chartInfo.highlight = ChartHelper.CreateImage("Highlight", chartInfo.dataRect);
            chartInfo.highlight.transform.SetAsFirstSibling();
            chartInfo.highlight.color = chartInfo.options.plotOptions.itemHighlightColor;
            chartInfo.highlight.rectTransform.sizeDelta = new Vector2(chartInfo.chartSize.x, chartInfo.options.yAxis.gridLineWidth * 2.0f);
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
        }

        protected override void UpdateYAxisGrid()
        {
            yGrid = ChartHelper.CreateEmptyRect("YGrid", chartInfo.gridRect, true).gameObject.AddComponent<ChartGraphicRect>();
            yGrid.color = chartInfo.options.yAxis.gridLineColor;
            yGrid.width = chartInfo.options.yAxis.gridLineWidth;
            yGrid.num = chartInfo.xAxisInfo.count;
            yGrid.inverted = chartInfo.options.plotOptions.inverted;
            yGrid.rectTransform.offsetMin = new Vector2(0.0f, chartInfo.options.xAxis.minPadding);
            yGrid.rectTransform.offsetMax = new Vector2(0.0f, -chartInfo.options.xAxis.maxPadding);
        }

        protected override void UpdateXAxisTick()
        {
            xTicks = ChartHelper.CreateEmptyRect("XTicks", chartInfo.gridRect).gameObject.AddComponent<ChartGraphicRect>();
            xTicks.color = chartInfo.options.xAxis.tickColor;
            xTicks.width = xTickSize.x;
            xTicks.num = chartInfo.xAxisInfo.count;
            xTicks.inverted = chartInfo.options.plotOptions.inverted;

            xTicks.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            xTicks.rectTransform.anchorMax = new Vector2(0.0f, 1.0f);
            xTicks.rectTransform.offsetMin = new Vector2(-xTickSize.y - xAxisWidth * 0.5f, chartInfo.options.xAxis.minPadding);
            xTicks.rectTransform.offsetMax = new Vector2(0.0f, -chartInfo.options.xAxis.maxPadding);
            offsetMin.x += xTickSize.y;
        }

        protected override void UpdateXAxisLabel()
        {
            EzChartText labelTemp = ChartHelper.CreateText("XGridLabel", chartInfo.gridLabelRect, chartInfo.options.xAxis.labelOption, chartInfo.options.plotOptions.generalFont);
            labelTemp.rectTransform.sizeDelta = Vector2.zero;

            RectTransform xLabelRect = ChartHelper.CreateEmptyRect("XLabels", chartInfo.gridLabelRect);
            xLabelRect.anchorMin = new Vector2(0.0f, 0.0f);
            xLabelRect.anchorMax = new Vector2(0.0f, 1.0f);
            xLabelRect.offsetMin = new Vector2(1.0f, chartInfo.options.xAxis.minPadding);
            xLabelRect.offsetMax = new Vector2(1.0f, -chartInfo.options.xAxis.maxPadding);

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

            float labelOffset = xTickSize.y + xAxisWidth * 0.5f + labelTemp.fontSize * 0.5f + chartInfo.options.yAxis.minPadding;
            labelTemp.rectTransform.anchoredPosition = new Vector2(-labelOffset, 0.0f);
            labelTemp.alignment = ChartHelper.ConvertAlignment(TextAnchor.MiddleRight);

            float labelSize = maxWidth;
            labelSize = Mathf.Clamp(labelSize, 0.0f, chartInfo.chartSize.x * 0.5f - offsetMin.x);
            maxWidth = labelSize;

            int interval = Mathf.RoundToInt(chartInfo.options.xAxis.interval);
            if (interval < 1) interval = Mathf.CeilToInt(labelTemp.fontSize * 1.25f / unitWidth + 0.0001f);
            if (xTicks != null) xTicks.interval = interval;
            if (yGrid != null) yGrid.interval = interval;

            for (int i = 0; i < categories.Length; i += interval)
            {
                int posIndex = chartInfo.options.xAxis.reversed ? categories.Length - i - 1 : i;
                EzChartText label = GameObject.Instantiate(labelTemp, xLabelRect);
                label.text = categories[i];
                label.rectTransform.anchorMin = new Vector2(0.0f, spacing * posIndex);
                label.rectTransform.anchorMax = new Vector2(0.0f, spacing * posIndex);
                if (label.preferredWidth > maxWidth) ChartHelper.TruncateText(label, maxWidth);
            }

            offsetMin.x += labelSize + labelTemp.fontSize + chartInfo.options.yAxis.minPadding;
            offsetMin.x = Mathf.Clamp(offsetMin.x, 0.0f, chartInfo.chartSize.x * 0.5f);

            ChartHelper.Destroy(labelTemp.gameObject);
        }
    }
}