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
    public class ChartGridRect : ChartGrid
    {
        protected Vector2 offsetMin, offsetMax;
        protected Vector2 yTickSize, xTickSize;
        protected float yAxisWidth, xAxisWidth;
        protected EzChartText xTitle, yTitle;
        protected ChartGraphicRect yGrid;
        protected ChartGraphicRect xTicks;

        public override float GetMouseValueY()
        {
            float y = chartInfo.yAxisInfo.GetValue(chartInfo.gridMousePos.y / chartInfo.chartSize.y);
            return y;
        }

        public override float GetMouseValueX()
        {
            if (chartInfo.gridMousePos.x < 0 || chartInfo.gridMousePos.x > chartInfo.chartSize.x) return -1;
            int index = 0;
            index = Mathf.FloorToInt(chartInfo.gridMousePos.x / unitWidth);
            index = Mathf.Clamp(index, 0, (int)chartInfo.xAxisInfo.count - 1);
            if (chartInfo.options.xAxis.reversed) index = (int)chartInfo.xAxisInfo.max - index;
            else index += (int)chartInfo.xAxisInfo.min;
            return index;
        }

        public override void HighlightItem(int index)
        {
            if (chartInfo.options.xAxis.reversed) index = (int)chartInfo.xAxisInfo.max - index;
            else index -= (int)chartInfo.xAxisInfo.min;
            Vector2 offset = -0.5f * (new Vector2(chartInfo.chartSize.x, 0.0f) + chartInfo.GetPadding());
            chartInfo.highlight.transform.localPosition = new Vector2(unitWidth * (index + 0.5f) + offset.x, offset.y);
            chartInfo.highlight.gameObject.SetActive(true);
        }

        public override void UnhighlightItem(int index)
        {
            chartInfo.highlight.gameObject.SetActive(false);
        }

        public override void UpdateHighlight()
        {
            chartInfo.highlight = ChartHelper.CreateImage("Highlight", chartInfo.dataRect);
            chartInfo.highlight.transform.SetAsFirstSibling();
            chartInfo.highlight.color = chartInfo.options.plotOptions.itemHighlightColor;
            chartInfo.highlight.rectTransform.sizeDelta = new Vector2(unitWidth, chartInfo.chartSize.y);
            chartInfo.highlight.gameObject.SetActive(false);
        }

        public override void UpdateBackground()
        {
            chartInfo.background = ChartHelper.CreateImage("Background", chartInfo.contentRect, false, true);
            chartInfo.background.transform.SetAsFirstSibling();
            chartInfo.background.color = chartInfo.options.plotOptions.backgroundColor;
        }

        public override void UpdateGrid()
        {
            offsetMin = offsetMax = Vector2.zero;
            chartInfo.dataRect.gameObject.AddComponent<Image>().raycastTarget = false;
            chartInfo.dataRect.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            yTickSize = chartInfo.options.yAxis.enableTick ? chartInfo.options.yAxis.tickSize : new Vector2();
            xTickSize = chartInfo.options.xAxis.enableTick ? chartInfo.options.xAxis.tickSize : new Vector2();
            yAxisWidth = chartInfo.options.yAxis.enableAxisLine ? chartInfo.options.yAxis.axisLineWidth : 0.0f;
            xAxisWidth = chartInfo.options.xAxis.enableAxisLine ? chartInfo.options.xAxis.axisLineWidth : 0.0f;

            ComputeAxisInfoY();
            ComputeAxisInfoX();
            if (chartInfo.options.yAxis.enableTitle) UpdateYAxisTitle();
            if (chartInfo.options.xAxis.enableTitle) UpdateXAxisTitle();

            ComputeYAxis();
            if (chartInfo.options.yAxis.enableGridLine) UpdateYAxisGrid();
            if (chartInfo.options.yAxis.enableTick) UpdateYAxisTick();
            if (chartInfo.options.yAxis.enableLabel) UpdateYAxisLabel();

            ComputeXAxis();
            if (chartInfo.options.xAxis.enableGridLine) UpdateXAxisGrid();
            if (chartInfo.options.xAxis.enableTick) UpdateXAxisTick();
            if (chartInfo.options.xAxis.enableLabel) UpdateXAxisLabel();

            chartInfo.contentRect.offsetMin += offsetMin;
            chartInfo.contentRect.offsetMax += offsetMax;
            chartInfo.chartSize -= offsetMin - offsetMax;
            chartInfo.chartSize -= new Vector2(chartInfo.options.xAxis.minPadding, chartInfo.options.yAxis.minPadding) +
                                   new Vector2(chartInfo.options.xAxis.maxPadding, chartInfo.options.yAxis.maxPadding);

            if (yTitle != null) AdjustYAxisTitle();
            if (xTitle != null) AdjustXAxisTitle();
            
            if (chartInfo.options.xAxis.enableAxisLine) UpdateXAxisLine();
            if (chartInfo.options.yAxis.enableAxisLine) UpdateYAxisLine();
        }

        protected virtual void ComputeAxisInfoY()
        {
            chartInfo.yAxisInfo = new ChartAxisInfo();
            switch (chartInfo.options.plotOptions.columnStacking)
            {
                case ColumnStacking.None:
                    if (chartInfo.options.yAxis.autoAxisValues)
                    {
                        chartInfo.yAxisInfo.Compute(chartInfo.chartDataInfo.min, chartInfo.chartDataInfo.max, chartInfo.options.yAxis.axisDivision, chartInfo.options.yAxis.startFromZero);
                    }
                    else
                    {
                        chartInfo.yAxisInfo.Compute(chartInfo.options.yAxis.min, chartInfo.options.yAxis.max, chartInfo.options.yAxis.axisDivision);
                    }
                    break;
                case ColumnStacking.Normal:
                    chartInfo.yAxisInfo.Compute(chartInfo.chartDataInfo.minSum, chartInfo.chartDataInfo.maxSum, chartInfo.options.yAxis.axisDivision, chartInfo.options.yAxis.startFromZero);
                    break;
                case ColumnStacking.Percent:
                    chartInfo.yAxisInfo.Compute(chartInfo.chartDataInfo.minSum < 0.0f ? -1.0f : 0.0f, chartInfo.chartDataInfo.maxSum > 0.0f ? 1.0f : 0.0f,
                        chartInfo.chartDataInfo.minSum < 0.0f && chartInfo.chartDataInfo.maxSum > 0.0f ? 10 : 5);
                    break;
                default:
                    break;
            }
            if (chartInfo.options.yAxis.labelNumericFormat != "") chartInfo.yAxisInfo.labelFormat = chartInfo.options.yAxis.labelNumericFormat;
        }

        protected virtual void ComputeAxisInfoX()
        {
            chartInfo.xAxisInfo = new ChartAxisInfo();
            chartInfo.xAxisInfo.min = (int)chartInfo.chartDataInfo.minX;
            chartInfo.xAxisInfo.max = (int)chartInfo.chartDataInfo.maxX;
            chartInfo.xAxisInfo.span = chartInfo.xAxisInfo.max - chartInfo.xAxisInfo.min;
            chartInfo.xAxisInfo.count = chartInfo.xAxisInfo.span + 1;
        }

        protected virtual void UpdateYAxisTitle()
        {
            yTitle = ChartHelper.CreateText("YTitle", chartInfo.contentRect, chartInfo.options.yAxis.titleOption, chartInfo.options.plotOptions.generalFont);
            yTitle.text = chartInfo.options.yAxis.title;
            float height = yTitle.fontSize * 1.2f;
            if (chartInfo.options.yAxis.mirrored) { offsetMax.x = -height; }
            else { offsetMin.x = height; }
        }

        protected virtual void UpdateXAxisTitle()
        {
            xTitle = ChartHelper.CreateText("XTitle", chartInfo.contentRect, chartInfo.options.xAxis.titleOption, chartInfo.options.plotOptions.generalFont);
            xTitle.text = chartInfo.options.xAxis.title;
            float height = xTitle.fontSize * 1.2f;
            offsetMin.y = height;
        }

        protected virtual void AdjustYAxisTitle()
        {
            float height = yTitle.fontSize * 1.2f;
            float width = yTitle.preferredWidth;
            if (chartInfo.options.yAxis.mirrored)
            {
                yTitle.rectTransform.anchorMin = new Vector2(1.0f, 0.5f);
                yTitle.rectTransform.anchorMax = new Vector2(1.0f, 0.5f);
                yTitle.rectTransform.anchoredPosition = new Vector2(-offsetMax.x - height * 0.5f, 0.0f);
                yTitle.rectTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, -90.0f);
            }
            else
            {
                yTitle.rectTransform.anchorMin = new Vector2(0.0f, 0.5f);
                yTitle.rectTransform.anchorMax = new Vector2(0.0f, 0.5f);
                yTitle.rectTransform.anchoredPosition = new Vector2(-offsetMin.x + height * 0.5f, 0.0f);
                yTitle.rectTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, 90.0f);
            }
            yTitle.rectTransform.sizeDelta = new Vector2(width, height);
        }

        protected virtual void AdjustXAxisTitle()
        {
            float height = xTitle.fontSize * 1.2f;
            float width = xTitle.preferredWidth;
            xTitle.rectTransform.anchorMin = new Vector2(0.5f, 0.0f);
            xTitle.rectTransform.anchorMax = new Vector2(0.5f, 0.0f);
            xTitle.rectTransform.sizeDelta = new Vector2(width, height);
            xTitle.rectTransform.anchoredPosition = new Vector2(0.0f, -offsetMin.y + height * 0.5f);
        }

        protected virtual void ComputeYAxis()
        {

        }

        protected virtual void UpdateYAxisLine()
        {
            Image yAxis = ChartHelper.CreateImage("YAxis", chartInfo.gridRect);
            yAxis.color = chartInfo.options.yAxis.axisLineColor;
            if (chartInfo.options.yAxis.mirrored)
            {
                offsetMax.x -= yAxisWidth * 0.5f;
                yAxis.rectTransform.anchorMin = new Vector2(1.0f, 0.0f);
                yAxis.rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
            }
            else
            {
                offsetMin.x += yAxisWidth * 0.5f;
                yAxis.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
                yAxis.rectTransform.anchorMax = new Vector2(0.0f, 1.0f);
            }
            yAxis.rectTransform.sizeDelta = new Vector2(yAxisWidth, 0.0f);
            yAxis.rectTransform.offsetMin -= new Vector2(0.0f, xTickSize.y + yAxisWidth * 0.5f);
            yAxis.rectTransform.offsetMax += new Vector2(0.0f,
                chartInfo.options.yAxis.tickSize.x > chartInfo.options.xAxis.gridLineWidth ?
                chartInfo.options.yAxis.tickSize.x * 0.5f : chartInfo.options.xAxis.gridLineWidth * 0.5f);
        }

        protected virtual void UpdateYAxisGrid()
        {
            yGrid = ChartHelper.CreateEmptyRect("YGrid", chartInfo.gridRect, true).gameObject.AddComponent<ChartGraphicRect>();
            yGrid.color = chartInfo.options.yAxis.gridLineColor;
            yGrid.width = chartInfo.options.yAxis.gridLineWidth;
            yGrid.num = chartInfo.xAxisInfo.count;
            yGrid.mid = midGrid;
            yGrid.inverted = chartInfo.options.plotOptions.inverted;
            yGrid.rectTransform.offsetMin = new Vector2(chartInfo.options.xAxis.minPadding, 0.0f);
            yGrid.rectTransform.offsetMax = new Vector2(-chartInfo.options.xAxis.maxPadding, 0.0f);
        }

        protected virtual void UpdateYAxisTick()
        {
            ChartGraphicRect yTicks = ChartHelper.CreateEmptyRect("YTicks", chartInfo.gridRect).gameObject.AddComponent<ChartGraphicRect>();
            yTicks.color = chartInfo.options.yAxis.tickColor;
            yTicks.width = chartInfo.options.yAxis.tickSize.x;
            yTicks.num = chartInfo.yAxisInfo.count;
            yTicks.inverted = !chartInfo.options.plotOptions.inverted;

            if (chartInfo.options.yAxis.mirrored)
            {
                yTicks.rectTransform.anchorMin = new Vector2(1.0f, 0.0f);
                yTicks.rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
                yTicks.rectTransform.offsetMin = new Vector2(1.0f, chartInfo.options.yAxis.minPadding);
                yTicks.rectTransform.offsetMax = new Vector2(yTickSize.y + yAxisWidth * 0.5f, -chartInfo.options.yAxis.maxPadding);
                offsetMax.x -= yTickSize.y;
            }
            else
            {
                yTicks.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
                yTicks.rectTransform.anchorMax = new Vector2(0.0f, 1.0f);
                yTicks.rectTransform.offsetMin = new Vector2(-yTickSize.y - yAxisWidth * 0.5f, chartInfo.options.yAxis.minPadding);
                yTicks.rectTransform.offsetMax = new Vector2(0.0f, -chartInfo.options.yAxis.maxPadding);
                offsetMin.x += yTickSize.y;
            }
        }

        protected virtual void UpdateYAxisLabel()
        {
            EzChartText labelTemp = ChartHelper.CreateText("YGridLabel", chartInfo.gridLabelRect, chartInfo.options.yAxis.labelOption, chartInfo.options.plotOptions.generalFont);
            labelTemp.rectTransform.sizeDelta = Vector2.zero;

            float spacing = 1.0f / chartInfo.yAxisInfo.count;
            float labelOffset = yTickSize.y + yAxisWidth * 0.5f + labelTemp.fontSize * 0.5f;
            RectTransform yLabelRect = ChartHelper.CreateEmptyRect("YLabels", chartInfo.gridLabelRect);
            if (chartInfo.options.yAxis.mirrored)
            {
                labelTemp.rectTransform.anchoredPosition = new Vector2(labelOffset, 0.0f);
                labelTemp.alignment = ChartHelper.ConvertAlignment(TextAnchor.MiddleLeft);
                yLabelRect.anchorMin = new Vector2(1.0f, 0.0f);
                yLabelRect.anchorMax = new Vector2(1.0f, 1.0f);
                yLabelRect.offsetMin = new Vector2(1.0f, chartInfo.options.yAxis.minPadding);
                yLabelRect.offsetMax = new Vector2(1.0f, -chartInfo.options.yAxis.maxPadding);
            }
            else
            {
                labelTemp.rectTransform.anchoredPosition = new Vector2(-labelOffset, 0.0f);
                labelTemp.alignment = ChartHelper.ConvertAlignment(TextAnchor.MiddleRight);
                yLabelRect.anchorMin = new Vector2(0.0f, 0.0f);
                yLabelRect.anchorMax = new Vector2(0.0f, 1.0f);
                yLabelRect.offsetMin = new Vector2(0.0f, chartInfo.options.yAxis.minPadding);
                yLabelRect.offsetMax = new Vector2(0.0f, -chartInfo.options.yAxis.maxPadding);
            }

            for (int i = 0; i < chartInfo.yAxisInfo.count + 1; ++i)
            {
                EzChartText label = GameObject.Instantiate(labelTemp, yLabelRect);
                label.rectTransform.anchorMin = new Vector2(0.0f, spacing * i);
                label.rectTransform.anchorMax = new Vector2(0.0f, spacing * i);
                float value = chartInfo.yAxisInfo.min + chartInfo.yAxisInfo.interval * i;
                if (chartInfo.options.yAxis.absoluteValue) value = Mathf.Abs(value);
                string valueStr = chartInfo.options.plotOptions.columnStacking == ColumnStacking.Percent ? 
                    chartInfo.GetPercentageString(value, "f0") : chartInfo.GetValueString(value, chartInfo.yAxisInfo.labelFormat);
                label.text = chartInfo.options.yAxis.labelFormat.Replace("{value}", valueStr);
            }

            EzChartText firstLabel = yLabelRect.GetChild(1).gameObject.GetComponent<EzChartText>();
            EzChartText lastLabel = yLabelRect.GetChild(yLabelRect.childCount - 1).gameObject.GetComponent<EzChartText>();
            EzChartText temp = firstLabel.text.Length > lastLabel.text.Length ? firstLabel : lastLabel;

            if (chartInfo.options.yAxis.mirrored)
            {
                offsetMax.x -= temp.preferredWidth + labelTemp.fontSize;
                offsetMax.x = -Mathf.Clamp(-offsetMax.x, 0.0f, chartInfo.chartSize.x * 0.5f);
            }
            else
            {
                offsetMin.x += temp.preferredWidth + labelTemp.fontSize;
                offsetMin.x = Mathf.Clamp(offsetMin.x, 0.0f, chartInfo.chartSize.x * 0.5f);
            }

            ChartHelper.Destroy(labelTemp.gameObject);
        }

        protected virtual void ComputeXAxis()
        {
            unitWidth = (chartInfo.chartSize.x - offsetMin.x + offsetMax.x - chartInfo.options.xAxis.minPadding - chartInfo.options.xAxis.maxPadding) / chartInfo.xAxisInfo.count;
        }

        protected virtual void UpdateXAxisLine()
        {
            Image xAxis = ChartHelper.CreateImage("XAxis", chartInfo.gridRect);
            xAxis.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            xAxis.rectTransform.anchorMax = new Vector2(1.0f, 0.0f);
            xAxis.gameObject.name = "XAxis";
            xAxis.color = chartInfo.options.xAxis.axisLineColor;
            xAxis.rectTransform.sizeDelta = new Vector2(0.0f, xAxisWidth);
            //xAxis.rectTransform.offsetMin -= new Vector2(yTickSize.y + yAxisWidth * 0.5f, 0.0f);
            xAxis.rectTransform.offsetMax += new Vector2(
                xTickSize.x > chartInfo.options.yAxis.gridLineWidth ?
                xTickSize.x * 0.5f : chartInfo.options.yAxis.gridLineWidth * 0.5f, 0.0f);
            offsetMin.y += xAxisWidth * 0.5f;
        }

        protected virtual void UpdateXAxisGrid()
        {
            ChartGraphicRect xGrid = ChartHelper.CreateEmptyRect("XGrid", chartInfo.gridRect, true).gameObject.AddComponent<ChartGraphicRect>();
            xGrid.color = chartInfo.options.xAxis.gridLineColor;
            xGrid.width = chartInfo.options.xAxis.gridLineWidth;
            xGrid.num = chartInfo.yAxisInfo.count;
            xGrid.inverted = !chartInfo.options.plotOptions.inverted;
            xGrid.rectTransform.offsetMin = new Vector2(0.0f, chartInfo.options.yAxis.minPadding);
            xGrid.rectTransform.offsetMax = new Vector2(0.0f, -chartInfo.options.yAxis.maxPadding);
        }

        protected virtual void UpdateXAxisTick()
        {
            xTicks = ChartHelper.CreateEmptyRect("XTicks", chartInfo.gridRect).gameObject.AddComponent<ChartGraphicRect>();
            xTicks.color = chartInfo.options.xAxis.tickColor;
            xTicks.width = xTickSize.x;
            xTicks.num = chartInfo.xAxisInfo.count;
            xTicks.mid = midGrid;
            xTicks.inverted = chartInfo.options.plotOptions.inverted;

            xTicks.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            xTicks.rectTransform.anchorMax = new Vector2(1.0f, 0.0f);
            xTicks.rectTransform.offsetMin = new Vector2(chartInfo.options.xAxis.minPadding, -xTickSize.y - xAxisWidth * 0.5f);
            xTicks.rectTransform.offsetMax = new Vector2(-chartInfo.options.xAxis.maxPadding, 0.0f);
            offsetMin.y += xTickSize.y;
        }

        protected virtual void UpdateXAxisLabel()
        {
            EzChartText labelTemp = ChartHelper.CreateText("XGridLabel", chartInfo.gridLabelRect, chartInfo.options.xAxis.labelOption, chartInfo.options.plotOptions.generalFont);
            labelTemp.rectTransform.sizeDelta = Vector2.zero;

            RectTransform xLabelRect = ChartHelper.CreateEmptyRect("XLabels", chartInfo.gridLabelRect);
            xLabelRect.anchorMin = new Vector2(0.0f, 0.0f);
            xLabelRect.anchorMax = new Vector2(1.0f, 0.0f);
            xLabelRect.offsetMin = new Vector2(chartInfo.options.xAxis.minPadding, 0.0f);
            xLabelRect.offsetMax = new Vector2(-chartInfo.options.xAxis.maxPadding, 0.0f);

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

            int interval = (int)chartInfo.options.xAxis.interval;
            if (chartInfo.IsZooming())
            {
                float span = chartInfo.options.xAxis.autoAxisValues ? chartInfo.data.categories.Count : chartInfo.options.xAxis.max - chartInfo.options.xAxis.min;
                interval = (int)((chartInfo.xAxisInfo.span / span) * interval);
            }
            if (interval < 1) interval = Mathf.CeilToInt(labelTemp.fontSize * 1.25f / unitWidth + 0.0001f);
            if (xTicks != null) xTicks.interval = interval;
            if (yGrid != null) yGrid.interval = interval;
            maxWidth *= interval;

            for (int i = 0; i < chartInfo.xAxisInfo.count; i += interval)
            {
                int posIndex = chartInfo.options.xAxis.reversed ? (int)chartInfo.xAxisInfo.count - i - 1 : i;
                EzChartText label = GameObject.Instantiate(labelTemp, xLabelRect);
                label.text = chartInfo.data.categories[(int)chartInfo.xAxisInfo.min + i];
                label.rectTransform.anchorMin = new Vector2(spacing * (posIndex + 0.5f), 0.0f);
                label.rectTransform.anchorMax = new Vector2(spacing * (posIndex + 0.5f), 0.0f);
                if (label.preferredWidth > maxWidth) ChartHelper.TruncateText(label, maxWidth);
            }

            offsetMin.y += labelSize + labelTemp.fontSize * 0.2f;
            offsetMin.y = Mathf.Clamp(offsetMin.y, 0.0f, chartInfo.chartSize.y * 0.5f);

            ChartHelper.Destroy(labelTemp.gameObject);
        }
    }
}