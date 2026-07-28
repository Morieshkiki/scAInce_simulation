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
    public class ChartGridCircle : ChartGrid
    {
        [HideInInspector] public bool circularGrid = true;
        [HideInInspector] public float innerSize = 0.0f;
        [HideInInspector] public float outerSize = 1.0f;

        protected Vector2 yTickSize, xTickSize;
        protected float outerBorderWidth, innerBorderWidth;

        public override float GetMouseValueY()
        {
            float r = (chartInfo.gridMousePos.magnitude * 2 - chartInfo.chartSize.y) / (chartInfo.chartSize.x - chartInfo.chartSize.y);
            float y = chartInfo.yAxisInfo.GetValue(r);
            return y;
        }

        public override float GetMouseValueX()
        {
            if (chartInfo.gridMousePos.sqrMagnitude > 0.25f * chartInfo.chartSize.x * chartInfo.chartSize.x || chartInfo.gridMousePos.sqrMagnitude < 0.25f * chartInfo.chartSize.y * chartInfo.chartSize.y) return -1;
            int index = -1;
            index = Mathf.FloorToInt(chartInfo.mouseAngle / unitWidth) % (int)chartInfo.xAxisInfo.count;
            if (chartInfo.options.xAxis.reversed) index = (int)chartInfo.xAxisInfo.count - index - 1;
            return index;
        }

        public override void HighlightItem(int index)
        {
            if (chartInfo.options.xAxis.reversed) index = (int)chartInfo.xAxisInfo.count - index - 1;
            chartInfo.highlight.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, -unitWidth * index);
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
            chartInfo.highlight.sprite = Resources.Load<Sprite>("Images/Chart_Circle_512x512");
            chartInfo.highlight.type = Image.Type.Filled;
            chartInfo.highlight.fillMethod = Image.FillMethod.Radial360;
            chartInfo.highlight.fillOrigin = (int)Image.Origin360.Top;
            chartInfo.highlight.fillAmount = Mathf.Clamp01(1.0f / chartInfo.xAxisInfo.count);
            chartInfo.highlight.rectTransform.sizeDelta = new Vector2(chartInfo.chartSize.x, chartInfo.chartSize.x);
            chartInfo.highlight.gameObject.SetActive(false);
        }

        public override void UpdateBackground()
        {
            chartInfo.background = ChartHelper.CreateImage("Background", chartInfo.contentRect);
            chartInfo.background.transform.SetAsFirstSibling();
            chartInfo.background.sprite = Resources.Load<Sprite>("Images/Chart_Circle_512x512");
            chartInfo.background.color = chartInfo.options.plotOptions.backgroundColor;
            float bSize = chartInfo.chartSize.x + xTickSize.y * 2.0f + innerBorderWidth;
            chartInfo.background.rectTransform.sizeDelta = new Vector2(bSize, bSize);
        }

        public override void UpdateGrid()
        {
            yTickSize = chartInfo.options.yAxis.enableTick ? chartInfo.options.yAxis.tickSize : new Vector2();
            xTickSize = chartInfo.options.xAxis.enableTick ? chartInfo.options.xAxis.tickSize : new Vector2();
            innerBorderWidth = chartInfo.options.pane.enableInnerBorder ? chartInfo.options.pane.innerBorderWidth : 0.0f;
            outerBorderWidth = chartInfo.options.pane.enableOuterBorder ? chartInfo.options.pane.outerBorderWidth : 0.0f;
            
            ComputeChartSize();
            ComputeAxisInfoY();
            ComputeAxisInfoX();

            ComputeYAxis();
            if (chartInfo.options.yAxis.enableGridLine) UpdateYAxisGrid();
            if (chartInfo.options.yAxis.enableTick) UpdateYAxisTick();
            if (chartInfo.options.yAxis.enableLabel) UpdateYAxisLabel();

            ComputeXAxis();
            if (chartInfo.options.xAxis.enableGridLine) UpdateXAxisGrid();
            if (chartInfo.options.xAxis.enableTick) UpdateXAxisTick();
            if (chartInfo.options.xAxis.enableLabel) UpdateXAxisLabel();
            
            if (chartInfo.options.pane.enableInnerBorder) UpdateInnerBorder();
            if (chartInfo.options.pane.enableOuterBorder) UpdateOuterBorder();
        }
        
        protected virtual void ComputeChartSize()
        {
            chartInfo.chartSize.x = (chartInfo.chartSize.x < chartInfo.chartSize.y ? chartInfo.chartSize.x : chartInfo.chartSize.y) * Mathf.Clamp01(outerSize);

            float labelSize = 0.0f;
            if (chartInfo.options.xAxis.enableLabel)
                labelSize += chartInfo.options.xAxis.labelOption.customizedText == null ?
                    chartInfo.options.xAxis.labelOption.fontSize : chartInfo.options.xAxis.labelOption.customizedText.fontSize;
            chartInfo.chartSize.x -= (labelSize + xTickSize.y + innerBorderWidth * 0.5f) * 2.0f;
            chartInfo.chartSize.y = chartInfo.chartSize.x * Mathf.Clamp(innerSize, 0.0f, outerSize);
        }

        protected virtual void ComputeAxisInfoY()
        {
            chartInfo.yAxisInfo = new ChartAxisInfo();
            switch (chartInfo.options.plotOptions.columnStacking)
            {
                case ColumnStacking.None:
                    if (chartInfo.options.yAxis.autoAxisValues)
                    {
                        chartInfo.yAxisInfo.Compute(chartInfo.chartDataInfo.min > 0.0f ? chartInfo.chartDataInfo.min : 0.0f, chartInfo.chartDataInfo.max, chartInfo.options.yAxis.axisDivision, chartInfo.options.yAxis.startFromZero);
                    }
                    else
                    {
                        chartInfo.yAxisInfo.Compute(chartInfo.options.yAxis.min > 0.0f ? chartInfo.options.yAxis.min : 0.0f, chartInfo.options.yAxis.max, chartInfo.options.yAxis.axisDivision);
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

        protected virtual void UpdateInnerBorder()
        {
            if (chartInfo.chartSize.y < 0.01f) return;
            float smoothness = Mathf.Clamp01(3.0f / innerBorderWidth);
            float gridWidth = innerBorderWidth * (1 + smoothness);
            ChartGraphicRing innerBorder = ChartHelper.CreateEmptyRect("InnerRing", chartInfo.gridRect).gameObject.AddComponent<ChartGraphicRing>();
            innerBorder.gameObject.AddComponent<ChartMaterialHandler>().Load("Materials/Chart_VBlur");
            innerBorder.rectTransform.sizeDelta = new Vector2(chartInfo.chartSize.y, chartInfo.chartSize.y);
            innerBorder.color = chartInfo.options.pane.innerBorderColor;
            innerBorder.width = gridWidth;
            innerBorder.mid = midGrid;
            innerBorder.side = chartInfo.xAxisInfo.count;
            innerBorder.isCircular = circularGrid;
            innerBorder.material.SetFloat("_Smoothness", smoothness);
        }

        protected virtual void UpdateOuterBorder()
        {
            float smoothness = Mathf.Clamp01(3.0f / outerBorderWidth);
            float gridWidth = outerBorderWidth * (1 + smoothness);
            ChartGraphicRing outerBorder = ChartHelper.CreateEmptyRect("OuterRing", chartInfo.gridRect).gameObject.AddComponent<ChartGraphicRing>();
            outerBorder.gameObject.AddComponent<ChartMaterialHandler>().Load("Materials/Chart_VBlur");
            outerBorder.rectTransform.sizeDelta = new Vector2(chartInfo.chartSize.x, chartInfo.chartSize.x);
            outerBorder.color = chartInfo.options.pane.outerBorderColor;
            outerBorder.width = gridWidth;
            outerBorder.mid = midGrid;
            outerBorder.side = chartInfo.xAxisInfo.count;
            outerBorder.isCircular = circularGrid;
            outerBorder.material.SetFloat("_Smoothness", smoothness);
        }

        protected virtual void ComputeYAxis()
        {

        }

        protected virtual void UpdateYAxisGrid()
        {
            int interval = Mathf.RoundToInt(chartInfo.options.xAxis.interval);
            if (interval < 1) interval = 1;
            float smoothness = Mathf.Clamp01(3.0f / chartInfo.options.yAxis.gridLineWidth);
            float gridWidth = chartInfo.options.yAxis.gridLineWidth * (1 + smoothness);
            ChartGraphicRadialLine yGrid = ChartHelper.CreateEmptyRect("YGrid", chartInfo.gridRect).gameObject.AddComponent<ChartGraphicRadialLine>();
            if (!midGrid) yGrid.transform.SetAsFirstSibling();
            yGrid.gameObject.AddComponent<ChartMaterialHandler>().Load("Materials/Chart_UBlur");
            yGrid.rectTransform.sizeDelta = new Vector2(chartInfo.chartSize.x, chartInfo.chartSize.x);
            yGrid.color = chartInfo.options.yAxis.gridLineColor;
            yGrid.width = gridWidth;
            yGrid.innerSize = innerSize;
            yGrid.mid = midGrid;
            yGrid.side = chartInfo.xAxisInfo.count;
            yGrid.interval = interval;
            yGrid.material.SetFloat("_Smoothness", smoothness);
        }

        protected virtual void UpdateYAxisTick()
        {
            //float circularGridRatio = circularGrid ? 1.0f : Mathf.Sin((90.0f - 360.0f / chartInfo.xAxisInfo.count * 0.5f) * Mathf.Deg2Rad);
            //ChartGraphicRect yTicks = ChartHelper.CreateEmptyRect("YTicks", gridRect).gameObject.AddComponent<ChartGraphicRect>();
            //yTicks.color = chartInfo.options.yAxis.tickColor;
            //yTicks.width = yTickSize.y;
            //yTicks.num = chartInfo.yAxisInfo.count;
            //yTicks.inverted = true;
            //yTicks.rectTransform.sizeDelta = new Vector2(yTickSize.x, (chartInfo.chartSize.x - chartInfo.chartSize.y) * 0.5f * circularGridRatio);
            //yTicks.rectTransform.anchoredPosition = new Vector2(0.0f, yTicks.rectTransform.sizeDelta.y * 0.5f + chartInfo.options.xAxis.gridLineWidth * 0.5f + yTickSize.y * 0.5f);
        }

        protected virtual void UpdateYAxisLabel()
        {
            EzChartText labelTemp = ChartHelper.CreateText("YGridLabel", chartInfo.gridLabelRect, chartInfo.options.yAxis.labelOption, chartInfo.options.plotOptions.generalFont, TextAnchor.LowerCenter);
            labelTemp.rectTransform.sizeDelta = Vector2.zero;

            float circularGridRatio = circularGrid ? 1.0f : Mathf.Sin((90.0f - 360.0f / chartInfo.xAxisInfo.count * 0.5f) * Mathf.Deg2Rad);
            float spacing = (chartInfo.chartSize.x - chartInfo.chartSize.y) * circularGridRatio * 0.5f / chartInfo.yAxisInfo.count;
            float offset = chartInfo.chartSize.y * 0.5f + chartInfo.options.xAxis.gridLineWidth * 0.5f;
            for (int i = 0; i <= chartInfo.yAxisInfo.count; ++i)
            {
                float h = offset + spacing * i;
                EzChartText label = GameObject.Instantiate(labelTemp, chartInfo.labelRect);
                float value = chartInfo.yAxisInfo.min + chartInfo.yAxisInfo.interval * i;
                string valueStr = chartInfo.options.plotOptions.columnStacking == ColumnStacking.Percent ?
                    chartInfo.GetPercentageString(value, "f0") : chartInfo.GetValueString(value, chartInfo.yAxisInfo.labelFormat);
                label.text = chartInfo.options.yAxis.labelFormat.Replace("{value}", valueStr);
                label.transform.localPosition = new Vector2(0.0f, h);
            }

            ChartHelper.Destroy(labelTemp.gameObject);
        }

        protected virtual void ComputeXAxis()
        {
            unitWidth = 360.0f / chartInfo.xAxisInfo.count;
        }

        protected virtual void UpdateXAxisGrid()
        {
            float smoothness = Mathf.Clamp01(3.0f / chartInfo.options.xAxis.gridLineWidth);
            float gridWidth = chartInfo.options.xAxis.gridLineWidth * (1 + smoothness);
            ChartGraphicCircle xGrid = ChartHelper.CreateEmptyRect("XGrid", chartInfo.gridRect).gameObject.AddComponent<ChartGraphicCircle>();
            xGrid.gameObject.AddComponent<ChartMaterialHandler>().Load("Materials/Chart_VBlur");
            xGrid.rectTransform.sizeDelta = new Vector2(chartInfo.chartSize.x, chartInfo.chartSize.x);
            xGrid.color = chartInfo.options.xAxis.gridLineColor;
            xGrid.width = gridWidth;
            xGrid.num = chartInfo.yAxisInfo.count;
            xGrid.mid = midGrid;
            xGrid.side = chartInfo.xAxisInfo.count;
            xGrid.innerSize = innerSize;
            xGrid.isCircular = circularGrid;
            xGrid.material.SetFloat("_Smoothness", smoothness);
        }

        protected virtual void UpdateXAxisTick()
        {
            int interval = Mathf.RoundToInt(chartInfo.options.xAxis.interval);
            if (interval < 1) interval = 1;
            float smoothness = Mathf.Clamp01(3.0f / xTickSize.x);
            float gridWidth = xTickSize.x * (1 + smoothness);
            ChartGraphicRadialLine xTicks = ChartHelper.CreateEmptyRect("XTicks", chartInfo.gridRect).gameObject.AddComponent<ChartGraphicRadialLine>();
            xTicks.gameObject.AddComponent<ChartMaterialHandler>().Load("Materials/Chart_UBlur");
            float bSize = chartInfo.chartSize.x + xTickSize.y * 2.0f + innerBorderWidth;
            xTicks.rectTransform.sizeDelta = new Vector2(bSize, bSize);
            xTicks.color = chartInfo.options.xAxis.tickColor;
            xTicks.width = gridWidth;
            xTicks.innerSize = chartInfo.chartSize.x / xTicks.rectTransform.sizeDelta.x;
            xTicks.mid = midGrid;
            xTicks.side = chartInfo.xAxisInfo.count;
            xTicks.interval = interval;
            xTicks.material.SetFloat("_Smoothness", smoothness);
        }

        protected virtual void UpdateXAxisLabel()
        {
            EzChartText labelTemp = ChartHelper.CreateText("XGridLabel", chartInfo.gridLabelRect, chartInfo.options.xAxis.labelOption, chartInfo.options.plotOptions.generalFont);
            labelTemp.rectTransform.sizeDelta = Vector2.zero;

            int interval = Mathf.RoundToInt(chartInfo.options.xAxis.interval);
            if (interval < 1) interval = 1;
            float dist = chartInfo.chartSize.x * 0.5f + labelTemp.fontSize * 0.5f + xTickSize.y + innerBorderWidth * 0.5f;
            for (int i = 0; i < chartInfo.xAxisInfo.count; i += interval)
            {
                int posIndex = chartInfo.options.xAxis.reversed ? (int)chartInfo.xAxisInfo.count - i - 1 : i;
                EzChartText label = GameObject.Instantiate(labelTemp, chartInfo.labelRect);
                label.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, -unitWidth * posIndex - unitWidth * 0.5f);
                label.rectTransform.anchoredPosition = label.transform.up * dist;
                label.text = chartInfo.data.categories[i];
            }

            ChartHelper.Destroy(labelTemp.gameObject);
        }
    }
}