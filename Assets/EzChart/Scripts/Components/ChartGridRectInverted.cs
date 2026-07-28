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
    public class ChartGridRectInverted : ChartGridRect
    {
        public override float GetMouseValueY()
        {
            float y = chartInfo.yAxisInfo.GetValue(chartInfo.gridMousePos.x / chartInfo.chartSize.x);
            return y;
        }

        public override float GetMouseValueX()
        {
            if (chartInfo.gridMousePos.y < 0 || chartInfo.gridMousePos.y > chartInfo.chartSize.y) return -1;
            int index = 0;
            index = Mathf.FloorToInt(chartInfo.gridMousePos.y / unitWidth);
            index = Mathf.Clamp(index, 0, (int)chartInfo.xAxisInfo.count - 1);
            if (!chartInfo.options.xAxis.reversed) index = (int)chartInfo.xAxisInfo.max - index;
            else index += (int)chartInfo.xAxisInfo.min;
            return index;
        }

        public override void HighlightItem(int index)
        {
            if (!chartInfo.options.xAxis.reversed) index = (int)chartInfo.xAxisInfo.max - index;
            else index -= (int)chartInfo.xAxisInfo.min;
            Vector2 offset = -0.5f * (new Vector2(0.0f, chartInfo.chartSize.y) + chartInfo.GetPadding());
            chartInfo.highlight.transform.localPosition = new Vector2(offset.x, unitWidth * (index + 0.5f) + offset.y);
            chartInfo.highlight.gameObject.SetActive(true);
        }

        public override void UpdateHighlight()
        {
            chartInfo.highlight = ChartHelper.CreateImage("Highlight", chartInfo.dataRect);
            chartInfo.highlight.transform.SetAsFirstSibling();
            chartInfo.highlight.color = chartInfo.options.plotOptions.itemHighlightColor;
            chartInfo.highlight.rectTransform.sizeDelta = new Vector2(chartInfo.chartSize.x, unitWidth);
            chartInfo.highlight.gameObject.SetActive(false);
        }

        protected override void UpdateYAxisTitle()
        {
            yTitle = ChartHelper.CreateText("YTitle", chartInfo.contentRect, chartInfo.options.yAxis.titleOption, chartInfo.options.plotOptions.generalFont);
            yTitle.text = chartInfo.options.yAxis.title;
            float height = yTitle.fontSize * 1.2f;
            if (chartInfo.options.yAxis.mirrored) { offsetMax.y = -height; }
            else { offsetMin.y = height; }
        }

        protected override void AdjustYAxisTitle()
        {
            float height = yTitle.fontSize * 1.2f;
            float width = yTitle.preferredWidth;
            if (chartInfo.options.yAxis.mirrored)
            {
                yTitle.rectTransform.anchorMin = new Vector2(0.5f, 1.0f);
                yTitle.rectTransform.anchorMax = new Vector2(0.5f, 1.0f);
                yTitle.rectTransform.anchoredPosition = new Vector2(0.0f, -offsetMax.y - height * 0.5f);
            }
            else
            {
                yTitle.rectTransform.anchorMin = new Vector2(0.5f, 0.0f);
                yTitle.rectTransform.anchorMax = new Vector2(0.5f, 0.0f);
                yTitle.rectTransform.anchoredPosition = new Vector2(0.0f, -offsetMin.y + height * 0.5f);
            }
            yTitle.rectTransform.sizeDelta = new Vector2(width, height);
        }

        protected override void UpdateXAxisTitle()
        {
            xTitle = ChartHelper.CreateText("XTitle", chartInfo.contentRect, chartInfo.options.xAxis.titleOption, chartInfo.options.plotOptions.generalFont);
            xTitle.text = chartInfo.options.xAxis.title;
            float height = xTitle.fontSize * 1.2f;
            offsetMin.x = height;
        }

        protected override void AdjustXAxisTitle()
        {
            float height = xTitle.fontSize * 1.2f;
            float width = xTitle.preferredWidth;
            xTitle.rectTransform.anchorMin = new Vector2(0.0f, 0.5f);
            xTitle.rectTransform.anchorMax = new Vector2(0.0f, 0.5f);
            xTitle.rectTransform.sizeDelta = new Vector2(width, height);
            xTitle.rectTransform.anchoredPosition = new Vector2(-offsetMin.x + height * 0.5f, 0.0f);
            xTitle.rectTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, 90.0f);
        }

        protected override void UpdateYAxisLine()
        {
            Image yAxis = ChartHelper.CreateImage("YAxis", chartInfo.gridRect);
            yAxis.color = chartInfo.options.yAxis.axisLineColor;
            if (chartInfo.options.yAxis.mirrored)
            {
                offsetMax.y -= yAxisWidth * 0.5f;
                yAxis.rectTransform.anchorMin = new Vector2(0.0f, 1.0f);
                yAxis.rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
            }
            else
            {
                offsetMin.y += yAxisWidth * 0.5f;
                yAxis.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
                yAxis.rectTransform.anchorMax = new Vector2(1.0f, 0.0f);
            }
            yAxis.rectTransform.sizeDelta = new Vector2(0.0f, yAxisWidth);
            yAxis.rectTransform.offsetMin -= new Vector2(xTickSize.y + yAxisWidth * 0.5f, 0.0f);
            yAxis.rectTransform.offsetMax += new Vector2(
                chartInfo.options.yAxis.tickSize.x > chartInfo.options.xAxis.gridLineWidth ?
                chartInfo.options.yAxis.tickSize.x * 0.5f : chartInfo.options.xAxis.gridLineWidth * 0.5f, 0.0f);
        }

        protected override void UpdateYAxisGrid()
        {
            yGrid = ChartHelper.CreateEmptyRect("YGrid", chartInfo.gridRect, true).gameObject.AddComponent<ChartGraphicRect>();
            yGrid.color = chartInfo.options.yAxis.gridLineColor;
            yGrid.width = chartInfo.options.yAxis.gridLineWidth;
            yGrid.num = chartInfo.xAxisInfo.count;
            yGrid.mid = midGrid;
            yGrid.inverted = chartInfo.options.plotOptions.inverted;
            yGrid.rectTransform.offsetMin = new Vector2(0.0f, chartInfo.options.xAxis.minPadding);
            yGrid.rectTransform.offsetMax = new Vector2(0.0f, -chartInfo.options.xAxis.maxPadding);
        }

        protected override void UpdateYAxisTick()
        {
            ChartGraphicRect yTicks = ChartHelper.CreateEmptyRect("YTicks", chartInfo.gridRect).gameObject.AddComponent<ChartGraphicRect>();
            yTicks.color = chartInfo.options.yAxis.tickColor;
            yTicks.width = chartInfo.options.yAxis.tickSize.x;
            yTicks.num = chartInfo.yAxisInfo.count;
            yTicks.inverted = !chartInfo.options.plotOptions.inverted;

            if (chartInfo.options.yAxis.mirrored)
            {
                yTicks.rectTransform.anchorMin = new Vector2(0.0f, 1.0f);
                yTicks.rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
                yTicks.rectTransform.offsetMin = new Vector2(chartInfo.options.yAxis.minPadding, 1.0f);
                yTicks.rectTransform.offsetMax = new Vector2(-chartInfo.options.yAxis.maxPadding, yTickSize.y + yAxisWidth * 0.5f);
                offsetMax.y -= yTickSize.y;
            }
            else
            {
                yTicks.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
                yTicks.rectTransform.anchorMax = new Vector2(1.0f, 0.0f);
                yTicks.rectTransform.offsetMin = new Vector2(chartInfo.options.yAxis.minPadding, -yTickSize.y - yAxisWidth * 0.5f);
                yTicks.rectTransform.offsetMax = new Vector2(-chartInfo.options.yAxis.maxPadding, 0.0f);
                offsetMin.y += yTickSize.y;
            }
        }

        protected override void UpdateYAxisLabel()
        {
            EzChartText labelTemp = ChartHelper.CreateText("YGridLabel", chartInfo.gridLabelRect, chartInfo.options.yAxis.labelOption, chartInfo.options.plotOptions.generalFont);
            labelTemp.rectTransform.sizeDelta = Vector2.zero;

            float spacing = 1.0f / chartInfo.yAxisInfo.count;
            float labelOffset = yTickSize.y + yAxisWidth * 0.5f + labelTemp.fontSize * 0.1f;
            RectTransform yLabelRect = ChartHelper.CreateEmptyRect("YLabels", chartInfo.gridLabelRect);

            if (chartInfo.options.yAxis.mirrored)
            {
                labelTemp.rectTransform.anchoredPosition = new Vector2(0.0f, labelOffset);
                labelTemp.alignment = ChartHelper.ConvertAlignment(TextAnchor.LowerCenter);
                yLabelRect.anchorMin = new Vector2(0.0f, 1.0f);
                yLabelRect.anchorMax = new Vector2(1.0f, 1.0f);
                yLabelRect.offsetMin = new Vector2(chartInfo.options.yAxis.minPadding, 1.0f);
                yLabelRect.offsetMax = new Vector2(-chartInfo.options.yAxis.maxPadding, 1.0f);
            }
            else
            {
                labelTemp.rectTransform.anchoredPosition = new Vector2(0.0f, -labelOffset);
                labelTemp.alignment = ChartHelper.ConvertAlignment(TextAnchor.UpperCenter);
                yLabelRect.anchorMin = new Vector2(0.0f, 0.0f);
                yLabelRect.anchorMax = new Vector2(1.0f, 0.0f);
                yLabelRect.offsetMin = new Vector2(chartInfo.options.yAxis.minPadding, 0.0f);
                yLabelRect.offsetMax = new Vector2(-chartInfo.options.yAxis.maxPadding, 0.0f);
            }
            
            for (int i = 0; i < chartInfo.yAxisInfo.count + 1; ++i)
            {
                EzChartText label = GameObject.Instantiate(labelTemp, yLabelRect);
                label.rectTransform.anchorMin = new Vector2(spacing * i, 0.0f);
                label.rectTransform.anchorMax = new Vector2(spacing * i, 0.0f);
                float value = chartInfo.yAxisInfo.min + chartInfo.yAxisInfo.interval * i;
                if (chartInfo.options.yAxis.absoluteValue) value = Mathf.Abs(value);
                string valueStr = chartInfo.options.plotOptions.columnStacking == ColumnStacking.Percent ?
                    chartInfo.GetPercentageString(value, "f0") : chartInfo.GetValueString(value, chartInfo.yAxisInfo.labelFormat);
                label.text = chartInfo.options.yAxis.labelFormat.Replace("{value}", valueStr);
            }

            if (chartInfo.options.yAxis.mirrored)
            {
                offsetMax.y -= labelTemp.fontSize * 1.2f;
                offsetMax.y = -Mathf.Clamp(-offsetMax.y, 0.0f, chartInfo.chartSize.y * 0.5f);
            }
            else
            {
                offsetMin.y += labelTemp.fontSize * 1.2f;
                offsetMin.y = Mathf.Clamp(offsetMin.y, 0.0f, chartInfo.chartSize.y * 0.5f);
            }

            ChartHelper.Destroy(labelTemp.gameObject);
        }

        protected override void ComputeXAxis()
        {
            unitWidth = (chartInfo.chartSize.y - offsetMin.y + offsetMax.y - chartInfo.options.xAxis.minPadding - chartInfo.options.xAxis.maxPadding) / chartInfo.xAxisInfo.count;
        }

        protected override void UpdateXAxisLine()
        {
            Image xAxis = ChartHelper.CreateImage("XAxis", chartInfo.gridRect);
            xAxis.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            xAxis.rectTransform.anchorMax = new Vector2(0.0f, 1.0f);
            xAxis.gameObject.name = "XAxis";
            xAxis.color = chartInfo.options.xAxis.axisLineColor;
            xAxis.rectTransform.sizeDelta = new Vector2(xAxisWidth, 0.0f);
            //xAxis.rectTransform.offsetMin -= new Vector2(0.0f, yTickSize.y + yAxisWidth * 0.5f);
            xAxis.rectTransform.offsetMax += new Vector2(0.0f,
                xTickSize.x > chartInfo.options.yAxis.gridLineWidth ?
                xTickSize.x * 0.5f : chartInfo.options.yAxis.gridLineWidth * 0.5f);
            offsetMin.x += xAxisWidth * 0.5f;
        }

        protected override void UpdateXAxisGrid()
        {
            ChartGraphicRect xGrid = ChartHelper.CreateEmptyRect("XGrid", chartInfo.gridRect, true).gameObject.AddComponent<ChartGraphicRect>();
            xGrid.color = chartInfo.options.xAxis.gridLineColor;
            xGrid.width = chartInfo.options.xAxis.gridLineWidth;
            xGrid.num = chartInfo.yAxisInfo.count;
            xGrid.inverted = !chartInfo.options.plotOptions.inverted;
            xGrid.rectTransform.offsetMin = new Vector2(chartInfo.options.yAxis.minPadding, 0.0f);
            xGrid.rectTransform.offsetMax = new Vector2(-chartInfo.options.yAxis.maxPadding, 0.0f);
        }

        protected override void UpdateXAxisTick()
        {
            xTicks = ChartHelper.CreateEmptyRect("XTicks", chartInfo.gridRect).gameObject.AddComponent<ChartGraphicRect>();
            xTicks.color = chartInfo.options.xAxis.tickColor;
            xTicks.width = xTickSize.x;
            xTicks.num = chartInfo.xAxisInfo.count;
            xTicks.mid = midGrid;
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

            float spacing = 1.0f / chartInfo.xAxisInfo.count;
            float maxWidth = 0.0f;
            string maxStr = "";
            for (int i = (int)chartInfo.xAxisInfo.min; i <= chartInfo.xAxisInfo.max; ++i)
            {
                if (chartInfo.data.categories[i].Length > maxStr.Length) maxStr = chartInfo.data.categories[i];
            }
            EzChartText labelTmp = GameObject.Instantiate(labelTemp, chartInfo.gridLabelRect);
            labelTmp.text = maxStr;
            maxWidth = labelTmp.preferredWidth;
            ChartHelper.Destroy(labelTmp.gameObject);

            float labelOffset = xTickSize.y + xAxisWidth * 0.5f + labelTemp.fontSize * 0.5f;
            labelTemp.rectTransform.anchoredPosition = new Vector2(-labelOffset, 0.0f);
            labelTemp.alignment = ChartHelper.ConvertAlignment(TextAnchor.MiddleRight);

            float labelSize = maxWidth;
            labelSize = Mathf.Clamp(labelSize, 0.0f, chartInfo.chartSize.x * 0.5f - offsetMin.x);
            maxWidth = labelSize;

            int interval = Mathf.RoundToInt(chartInfo.options.xAxis.interval);
            if (chartInfo.IsZooming())
            {
                float span = chartInfo.options.xAxis.autoAxisValues ? chartInfo.data.categories.Count : chartInfo.options.xAxis.max - chartInfo.options.xAxis.min;
                interval = (int)((chartInfo.xAxisInfo.span / span) * interval);
            }
            if (interval < 1) interval = Mathf.CeilToInt(labelTemp.fontSize * 1.25f / unitWidth + 0.0001f);
            if (xTicks != null) xTicks.interval = interval;
            if (yGrid != null) yGrid.interval = interval;

            for (int i = 0; i < chartInfo.xAxisInfo.count; i += interval)
            {
                int posIndex = !chartInfo.options.xAxis.reversed ? (int)chartInfo.xAxisInfo.count - i - 1 : i;
                EzChartText label = GameObject.Instantiate(labelTemp, xLabelRect);
                label.text = chartInfo.data.categories[(int)chartInfo.xAxisInfo.min + i];
                label.rectTransform.anchorMin = new Vector2(0.0f, spacing * (posIndex + 0.5f));
                label.rectTransform.anchorMax = new Vector2(0.0f, spacing * (posIndex + 0.5f));
                if (label.preferredWidth > maxWidth) ChartHelper.TruncateText(label, maxWidth);
            }

            offsetMin.x += labelSize + labelTemp.fontSize;
            offsetMin.x = Mathf.Clamp(offsetMin.x, 0.0f, chartInfo.chartSize.x * 0.5f);

            ChartHelper.Destroy(labelTemp.gameObject);
        }
    }
}