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
    public class RadarChart : ChartBase
    {
        new ChartGridCircle chartGrid;
        RadarChartPoint[] pointList;
        RadarChartLine[] lineList;
        RadarChartShade[] shadeList;
        float rInner, rRange;

        Vector2 GetItemPos(int seriesIndex, int cateIndex, float anchPos = 1.0f, float offset = 0.0f)
        {
            float rStart = rInner + rRange * pointList[seriesIndex].data[cateIndex].y;
            float r = rStart + rRange * pointList[seriesIndex].data[cateIndex].x * anchPos;
            Vector2 pos = pointList[seriesIndex].direction[cateIndex] * (r + offset);
            return pos;
        }

        float GetItemRadius(int seriesIndex, int cateIndex, float anchPos = 1.0f)
        {
            float rStart = rInner + rRange * pointList[seriesIndex].data[cateIndex].y;
            float r = rStart + rRange * pointList[seriesIndex].data[cateIndex].x * anchPos;
            return r;
        }

        float GetItemRot(int cateIndex)
        {
            float rot = chartGrid.unitWidth * (cateIndex + 0.5f);
            if (chartInfo.options.xAxis.reversed) { rot = Mathf.Repeat(-rot, 360.0001f); }
            return rot;
        }

        protected override void UpdateGrid()
        {
            if (chartGrid != null) return;
            base.chartGrid = chartGrid = new ChartGridCircle();
            chartGrid.chartInfo = chartInfo;
            chartGrid.midGrid = true;
            chartGrid.circularGrid = chartInfo.options.plotOptions.radarChartOption.circularGrid;
            chartGrid.innerSize = 0.0f;
            chartGrid.outerSize = chartInfo.options.pane.outerSize;
            chartGrid.UpdateGrid();

            rInner = chartInfo.chartSize.y * 0.5f;
            rRange = (chartInfo.chartSize.x - chartInfo.chartSize.y) * 0.5f;
        }

        protected override void UpdateItemMaterial()
        {
            itemMat = new Material[2];
            itemMatFade = new Material[3];

            if (chartInfo.options.plotOptions.radarChartOption.enablePointOutline)
            {
                itemMat[0] = new Material(Resources.Load<Material>("Materials/Chart_OutlineCircle"));
                itemMat[0].SetFloat("_Smoothness", Mathf.Clamp01(2.0f / chartInfo.options.plotOptions.radarChartOption.pointSize));
                itemMat[0].SetFloat("_OutlineWidth", Mathf.Clamp01(chartInfo.options.plotOptions.radarChartOption.pointOutlineWidth * 2.0f / chartInfo.options.plotOptions.radarChartOption.pointSize));
                itemMat[0].SetColor("_OutlineColor", chartInfo.options.plotOptions.radarChartOption.pointOutlineColor);
                itemMatFade[0] = new Material(itemMat[0]);
                itemMatFade[0].color = new Color(1.0f, 1.0f, 1.0f, fadeValue);
                itemMatFade[0].SetColor("_OutlineColor", chartInfo.options.plotOptions.radarChartOption.pointOutlineColor * fadeValue);
            }
            else
            {
                itemMat[0] = new Material(Resources.Load<Material>("Materials/Chart_Circle"));
                itemMat[0].SetFloat("_Smoothness", Mathf.Clamp01(3.0f / chartInfo.options.plotOptions.radarChartOption.pointSize));
                itemMatFade[0] = new Material(itemMat[0]);
                itemMatFade[0].color = new Color(1.0f, 1.0f, 1.0f, fadeValue);
            }
            if (chartInfo.options.plotOptions.radarChartOption.enableLine)
            {
                itemMat[1] = new Material(Resources.Load<Material>("Materials/Chart_UBlur"));
                itemMat[1].SetFloat("_Smoothness", Mathf.Clamp01(3.0f / chartInfo.options.plotOptions.radarChartOption.lineWidth));
                itemMatFade[1] = new Material(itemMat[1]);
                itemMatFade[1].color = new Color(1.0f, 1.0f, 1.0f, fadeValue);
            }
            if (chartInfo.options.plotOptions.radarChartOption.enableShade)
            {
                itemMatFade[2] = new Material(Resources.Load<Material>("Materials/Chart_UI"));
                itemMatFade[2].color = new Color(1.0f, 1.0f, 1.0f, fadeValue);
            }
        }

        protected override void UpdateItems()
        {
            pointList = new RadarChartPoint[chartInfo.data.series.Count];
            if (chartInfo.options.plotOptions.radarChartOption.enableLine) lineList = new RadarChartLine[chartInfo.data.series.Count];
            if (chartInfo.options.plotOptions.radarChartOption.enableShade) shadeList = new RadarChartShade[chartInfo.data.series.Count];
            float[] stackValueList = chartInfo.options.plotOptions.columnStacking == ColumnStacking.None ? null : new float[chartInfo.data.categories.Count];
            for (int i = 0; i < chartInfo.data.series.Count; ++i)
            {
                if (!chartInfo.data.series[i].show) continue;
                RectTransform seriesRect = ChartHelper.CreateEmptyRect(chartInfo.data.series[i].name, chartInfo.dataRect);
                seriesRect.sizeDelta = new Vector2(chartInfo.chartSize.x, chartInfo.chartSize.x);
                seriesRect.SetAsFirstSibling();

                //point
                pointList[i] = ChartHelper.CreateEmptyRect("Point", seriesRect, true).gameObject.AddComponent<RadarChartPoint>();
                pointList[i].material = itemMat[0];
                pointList[i].color = chartInfo.GetSeriesColor(i);
                pointList[i].diameter = chartInfo.options.plotOptions.radarChartOption.pointSize;
                pointList[i].reverse = chartInfo.options.xAxis.reversed;
                pointList[i].data = new Vector2[chartInfo.data.categories.Count];
                pointList[i].show = new bool[chartInfo.data.categories.Count];
                for (int j = 0; j < chartInfo.data.categories.Count; ++j)
                {
                    pointList[i].show[j] = j < chartInfo.data.series[i].data.Count && chartInfo.data.series[i].data[j].show && chartInfo.data.series[i].data[j].value >= 0.0f;
                    if (pointList[i].show[j]) pointList[i].data[j] = GetDataRatio(i, j, stackValueList);
                }
                pointList[i].RefreshBuffer();

                //line
                if (chartInfo.options.plotOptions.radarChartOption.enableLine)
                {
                    lineList[i] = ChartHelper.CreateEmptyRect("Line", seriesRect, true).gameObject.AddComponent<RadarChartLine>();
                    lineList[i].transform.SetAsFirstSibling();
                    lineList[i].material = itemMat[1];
                    lineList[i].color = pointList[i].color;
                    lineList[i].width = chartInfo.options.plotOptions.radarChartOption.lineWidth;
                    lineList[i].reverse = pointList[i].reverse;
                    lineList[i].data = pointList[i].data;
                    lineList[i].show = pointList[i].show;
                    lineList[i].direction = pointList[i].direction;
                    lineList[i].isDirty = false;
                    lineList[i].inited = true;
                }

                //shade
                if (chartInfo.options.plotOptions.radarChartOption.enableShade)
                {
                    shadeList[i] = ChartHelper.CreateEmptyRect("Shade", seriesRect, true).gameObject.AddComponent<RadarChartShade>();
                    shadeList[i].transform.SetAsFirstSibling();
                    shadeList[i].color = new Color(pointList[i].color.r, pointList[i].color.g, pointList[i].color.b, chartInfo.options.plotOptions.radarChartOption.shadeOpacity);
                    shadeList[i].reverse = pointList[i].reverse;
                    shadeList[i].data = pointList[i].data;
                    shadeList[i].show = pointList[i].show;
                    shadeList[i].direction = pointList[i].direction;
                    shadeList[i].isDirty = false;
                    shadeList[i].inited = true;
                }
            }
        }

        protected override void UpdateLabels()
        {
            EzChartText labelTemp = ChartHelper.CreateText("Label", transform, chartInfo.options.label.textOption, chartInfo.options.plotOptions.generalFont);
            labelTemp.rectTransform.sizeDelta = Vector2.zero;

            for (int i = 0; i < chartInfo.data.series.Count; ++i)
            {
                if (pointList[i] == null) continue;
                RectTransform seriesLabelRect = ChartHelper.CreateEmptyRect(chartInfo.data.series[i].name, chartInfo.labelRect);
                seriesLabelRect.sizeDelta = new Vector2(chartInfo.chartSize.x, chartInfo.chartSize.x);
                seriesLabelRect.SetAsFirstSibling();

                for (int j = 0; j < chartInfo.data.categories.Count; ++j)
                {
                    if (!pointList[i].show[j]) continue;
                    float rot = GetItemRot(j);
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
            float min = float.PositiveInfinity;
            for (int i = 0; i < chartInfo.data.series.Count; ++i)
            {
                if (pointList[i] == null || !pointList[i].show[cate]) continue;
                float currRot = GetItemRot(cate);
                int dir = chartInfo.mouseAngle > currRot ? 1 : -1;
                if (chartInfo.options.xAxis.reversed) dir *= -1;
                int nextCate = (int)Mathf.Repeat(cate + dir, chartInfo.data.categories.Count);
                
                Vector2 currPos = GetItemPos(i, cate);
                Vector2 nextPos = GetItemPos(i, nextCate);
                Vector2 pIntersect = ChartHelper.LineIntersection(new Vector2[] { currPos, chartInfo.mousePos, -chartInfo.mousePos, nextPos });
                float dist = Mathf.Abs(chartInfo.mousePos.sqrMagnitude - pIntersect.sqrMagnitude);
                if (dist < min) { index = i; min = dist; }
            }
            return index;
        }

        protected override void HighlightSeries(int series)
        {
            for (int i = 0; i < pointList.Length; ++i)
            {
                if (pointList[i] == null || i == series) continue;
                pointList[i].material = itemMatFade[0];
                if (chartInfo.options.plotOptions.radarChartOption.enableLine) lineList[i].material = itemMatFade[1];
                if (chartInfo.options.plotOptions.radarChartOption.enableShade) shadeList[i].material = itemMatFade[2];
            }
        }

        protected override void UnhighlightSeries(int series)
        {
            for (int i = 0; i < pointList.Length; ++i)
            {
                if (pointList[i] == null || i == series) continue;
                pointList[i].material = itemMat[0];
                if (chartInfo.options.plotOptions.radarChartOption.enableLine) lineList[i].material = itemMat[1];
                if (chartInfo.options.plotOptions.radarChartOption.enableShade) shadeList[i].material = null;
            }
        }

        protected override void UpdateTooltip(int cate, int series)
        {
            string tooltipText = GetFormattedHeaderText(cate);
            if (chartInfo.options.tooltip.share)
            {
                for (int i = 0; i < chartInfo.data.series.Count; ++i)
                {
                    if (pointList[i] == null || !pointList[i].show[cate]) continue;
                    tooltipText += "\n" + GetFormattedPointTextPos(i, cate);
                }
                chartInfo.tooltip.SetText(tooltipText);
            }
            else
            {
                if (pointList[series] != null && pointList[series].show[cate])
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

