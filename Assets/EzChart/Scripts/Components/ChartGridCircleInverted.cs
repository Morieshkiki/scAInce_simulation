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
    public class ChartGridCircleInverted : ChartGridCircle
    {
        [HideInInspector] public int activeCount = 0;
        [HideInInspector] public float startAngle = 0.0f;
        [HideInInspector] public float endAngle = 360.0f;
        [HideInInspector] public bool semicircle = false;

        public override float GetMouseValueY()
        {
            return -1;
        }

        public override float GetMouseValueX()
        {
            return -1;
        }

        public override void UpdateBackground()
        {
            chartInfo.background = ChartHelper.CreateImage("Background", chartInfo.contentRect);
            chartInfo.background.transform.SetAsFirstSibling();
            chartInfo.background.sprite = Resources.Load<Sprite>("Images/Chart_Circle_512x512");
            chartInfo.background.color = chartInfo.options.plotOptions.backgroundColor;
            float bSize = chartInfo.chartSize.x + yTickSize.y * 2.0f + outerBorderWidth;
            chartInfo.background.rectTransform.sizeDelta = new Vector2(bSize, bSize);
            chartInfo.background.rectTransform.anchoredPosition = chartInfo.centerOffset;
            if (semicircle)
            {
                chartInfo.background.type = Image.Type.Filled;
                chartInfo.background.fillMethod = Image.FillMethod.Radial360;
                chartInfo.background.fillOrigin = 3;
                chartInfo.background.fillAmount = 0.5f;
            }
        }

        protected override void ComputeChartSize()
        {
            float tmp = semicircle ? 2.0f : 1.0f;
            chartInfo.chartSize.x = (chartInfo.chartSize.x < chartInfo.chartSize.y * tmp ? chartInfo.chartSize.x : chartInfo.chartSize.y * tmp) * Mathf.Clamp01(outerSize);

            float labelSize = 0.0f;
            if (chartInfo.options.yAxis.enableLabel)
                labelSize += chartInfo.options.yAxis.labelOption.customizedText == null ? 
                    chartInfo.options.yAxis.labelOption.fontSize : chartInfo.options.yAxis.labelOption.customizedText.fontSize;
            chartInfo.chartSize.x -= (labelSize + yTickSize.y + innerBorderWidth * 0.5f) * 2.0f * tmp;
            chartInfo.chartSize.y = chartInfo.chartSize.x * Mathf.Clamp(innerSize, 0.0f, outerSize);

            if (semicircle)
            {
                startAngle = Mathf.Clamp(startAngle, -90.0f, 90.0f);
                endAngle = Mathf.Clamp(endAngle, -90.0f, 90.0f);
                chartInfo.centerOffset.y = -chartInfo.chartSize.x * 0.25f;
            }
            chartInfo.gridRect.anchoredPosition = chartInfo.centerOffset;
            chartInfo.labelRect.anchoredPosition = chartInfo.centerOffset;
        }

        protected override void UpdateInnerBorder()
        {
            float smoothness = Mathf.Clamp01(3.0f / innerBorderWidth);
            float gridWidth = innerBorderWidth * (1 + smoothness);
            ChartGraphicRing innerBorder = ChartHelper.CreateEmptyRect("InnerRing", chartInfo.gridRect).gameObject.AddComponent<ChartGraphicRing>();
            innerBorder.gameObject.AddComponent<ChartMaterialHandler>().Load("Materials/Chart_VBlur");
            innerBorder.rectTransform.sizeDelta = new Vector2(chartInfo.chartSize.y, chartInfo.chartSize.y);
            innerBorder.color = chartInfo.options.pane.innerBorderColor;
            innerBorder.width = gridWidth;
            innerBorder.mid = midGrid;
            innerBorder.side = activeCount;
            innerBorder.startAngle = startAngle;
            innerBorder.endAngle = endAngle;
            innerBorder.isCircular = circularGrid;
            innerBorder.material.SetFloat("_Smoothness", smoothness);
        }

        protected override void UpdateOuterBorder()
        {
            float smoothness = Mathf.Clamp01(3.0f / outerBorderWidth);
            float gridWidth = outerBorderWidth * (1 + smoothness);
            ChartGraphicRing outerBorder = ChartHelper.CreateEmptyRect("OuterRing", chartInfo.gridRect).gameObject.AddComponent<ChartGraphicRing>();
            outerBorder.gameObject.AddComponent<ChartMaterialHandler>().Load("Materials/Chart_VBlur");
            outerBorder.rectTransform.sizeDelta = new Vector2(chartInfo.chartSize.x, chartInfo.chartSize.x);
            outerBorder.color = chartInfo.options.pane.outerBorderColor;
            outerBorder.width = gridWidth;
            outerBorder.mid = midGrid;
            outerBorder.side = activeCount;
            outerBorder.startAngle = startAngle;
            outerBorder.endAngle = endAngle;
            outerBorder.isCircular = circularGrid;
            outerBorder.material.SetFloat("_Smoothness", smoothness);
        }

        protected override void UpdateYAxisGrid()
        {
            float smoothness = Mathf.Clamp01(3.0f / chartInfo.options.yAxis.gridLineWidth);
            float gridWidth = chartInfo.options.yAxis.gridLineWidth * (1 + smoothness);
            ChartGraphicCircle yGrid = ChartHelper.CreateEmptyRect("XGrid", chartInfo.gridRect).gameObject.AddComponent<ChartGraphicCircle>();
            yGrid.gameObject.AddComponent<ChartMaterialHandler>().Load("Materials/Chart_VBlur");
            yGrid.rectTransform.sizeDelta = new Vector2(chartInfo.chartSize.x, chartInfo.chartSize.x);
            yGrid.color = chartInfo.options.yAxis.gridLineColor;
            yGrid.width = gridWidth;
            yGrid.num = activeCount;
            yGrid.mid = midGrid;
            yGrid.side = (int)chartInfo.yAxisInfo.count;
            yGrid.innerSize = innerSize;
            yGrid.startAngle = startAngle;
            yGrid.endAngle = endAngle;
            yGrid.isCircular = circularGrid;
            yGrid.material.SetFloat("_Smoothness", smoothness);
        }

        protected override void UpdateYAxisTick()
        {
            float smoothness = Mathf.Clamp01(3.0f / yTickSize.x);
            float gridWidth = yTickSize.x * (1 + smoothness);
            ChartGraphicRadialLine yTicks = ChartHelper.CreateEmptyRect("XTicks", chartInfo.gridRect).gameObject.AddComponent<ChartGraphicRadialLine>();
            yTicks.gameObject.AddComponent<ChartMaterialHandler>().Load("Materials/Chart_UBlur");
            float bSize = chartInfo.chartSize.x + yTickSize.y * 2.0f + outerBorderWidth;
            yTicks.rectTransform.sizeDelta = new Vector2(bSize, bSize);
            yTicks.color = chartInfo.options.yAxis.tickColor;
            yTicks.width = gridWidth;
            yTicks.innerSize = chartInfo.chartSize.x / yTicks.rectTransform.sizeDelta.x;
            yTicks.startAngle = startAngle;
            yTicks.endAngle = endAngle;
            yTicks.mid = midGrid;
            yTicks.side = (int)chartInfo.yAxisInfo.count;
            yTicks.material.SetFloat("_Smoothness", smoothness);
        }

        protected override void UpdateYAxisLabel()
        {
            EzChartText labelTemp = ChartHelper.CreateText("YGridLabel", chartInfo.gridLabelRect, chartInfo.options.yAxis.labelOption, chartInfo.options.plotOptions.generalFont);
            labelTemp.rectTransform.sizeDelta = Vector2.zero;

            float total = Mathf.Repeat(endAngle - startAngle, 360.0001f);
            float dist = chartInfo.chartSize.x * 0.5f + labelTemp.fontSize * 0.5f + yTickSize.y + innerBorderWidth * 0.5f;
            float steps = total > 359.0f ? chartInfo.yAxisInfo.count - 1 : chartInfo.yAxisInfo.count;
            for (int i = 0; i <= steps; ++i)
            {
                EzChartText label = GameObject.Instantiate(labelTemp, chartInfo.labelRect);
                float value = chartInfo.yAxisInfo.min + chartInfo.yAxisInfo.interval * i;
                string valueStr = chartInfo.GetValueString(value, chartInfo.yAxisInfo.labelFormat);
                label.text = chartInfo.options.yAxis.labelFormat.Replace("{value}", valueStr);
                label.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, -startAngle - total / chartInfo.yAxisInfo.count * i);
                label.rectTransform.anchoredPosition = label.transform.up * dist;
            }

            ChartHelper.Destroy(labelTemp.gameObject);
        }

        protected override void ComputeXAxis()
        {
            unitWidth = (chartInfo.chartSize.x - chartInfo.chartSize.y) * 0.5f / activeCount;
        }

        protected override void UpdateXAxisGrid()
        {
            float smoothness = Mathf.Clamp01(3.0f / chartInfo.options.xAxis.gridLineWidth);
            float gridWidth = chartInfo.options.xAxis.gridLineWidth * (1 + smoothness);
            ChartGraphicRadialLine xGrid = ChartHelper.CreateEmptyRect("XGrid", chartInfo.gridRect).gameObject.AddComponent<ChartGraphicRadialLine>();
            if (!midGrid) xGrid.transform.SetAsFirstSibling();
            xGrid.gameObject.AddComponent<ChartMaterialHandler>().Load("Materials/Chart_UBlur");
            xGrid.rectTransform.sizeDelta = new Vector2(chartInfo.chartSize.x, chartInfo.chartSize.x);
            xGrid.color = chartInfo.options.xAxis.gridLineColor;
            xGrid.width = gridWidth;
            xGrid.innerSize = innerSize;
            xGrid.startAngle = startAngle;
            xGrid.endAngle = endAngle;
            xGrid.mid = midGrid;
            xGrid.side = (int)chartInfo.yAxisInfo.count;
            xGrid.material.SetFloat("_Smoothness", smoothness);
        }

        protected override void UpdateXAxisTick()
        {

        }

        protected override void UpdateXAxisLabel()
        {

        }
    }
}