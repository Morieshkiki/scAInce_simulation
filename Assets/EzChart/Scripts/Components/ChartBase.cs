using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ChartUtil
{
    public abstract class ChartBase : MonoBehaviour
    {
        protected const float fadeValue = 0.4f;

        public ChartGeneralInfo chartInfo;
        public ChartGrid chartGrid;

        ChartPointerHandler pointerHandler;
        ChartPointerDragHandler dragHandler;

        protected bool tooltipFollowMouse = false;
        protected Material[] itemMat;
        protected Material[] itemMatFade;
        int currCate = -1, currSeries = -1;
        int lastCate = -1, lastSeries = -1;

        private void OnDestroy()
        {
            if (itemMat != null)
            {
                for (int i = 0; i < itemMat.Length; ++i)
                {
                    ChartHelper.Destroy(itemMat[i]);
                }
            }

            if (itemMatFade != null)
            {
                for (int i = 0; i < itemMatFade.Length; ++i)
                {
                    ChartHelper.Destroy(itemMatFade[i]);
                }
            }
        }

        public void UpdateContent()
        {
            chartInfo.gridRect = ChartHelper.CreateEmptyRect("GridRect", transform, true);
            chartInfo.dataRect = ChartHelper.CreateEmptyRect("DataRect", transform, true);
            if (chartInfo.options.plotOptions.frontGrid) chartInfo.gridRect.SetSiblingIndex(chartInfo.dataRect.GetSiblingIndex() + 1);
            chartInfo.gridLabelRect = ChartHelper.CreateEmptyRect("GridLabelRect", transform, true);
            chartInfo.labelRect = ChartHelper.CreateEmptyRect("LabelRect", transform, true);
            UpdateGrid();
            if (chartInfo.chartMixer == null) UpdateContentItems(); 
            else chartInfo.chartMixer.UpdateContent(this);
            if (chartInfo.options.plotOptions.enableBackground) UpdateBackground();
            if (chartInfo.options.plotOptions.mouseTracking)
            {
                UpdateHighlight();
                pointerHandler = gameObject.GetComponent<ChartPointerHandler>();
                if (pointerHandler == null) pointerHandler = gameObject.AddComponent<ChartPointerHandler>();
                pointerHandler.onPointerExit += OnPointerExit;
                pointerHandler.onPointerDown += OnPointerDown;
                pointerHandler.onPointerHover += OnPointerHover;
            }
            if (chartInfo.chartZoom != null)
            {
                dragHandler = gameObject.AddComponent<ChartPointerDragHandler>();
                dragHandler.onEndDrag += chartInfo.chartZoom.SelectRangeEnd;
                chartInfo.chartZoom.CreateMask(chartInfo, chartGrid, transform);
            }
        }

        public void UpdateContentItems()
        {
            UpdateItemMaterial();
            UpdateItems();
            if (chartInfo.options.label.enable) UpdateLabels();
        }

        protected abstract void UpdateGrid();

        protected abstract void UpdateItemMaterial();

        protected abstract void UpdateItems();

        protected abstract void UpdateLabels();

        protected virtual void UpdateBackground()
        {
            chartGrid.UpdateBackground();
        }

        protected virtual void UpdateHighlight()
        {
            chartGrid.UpdateHighlight();
        }

        protected virtual int FindCategory()
        {
            return (int)chartGrid.GetMouseValueX();
        }

        protected virtual int FindSeries(int cate) { return -1; }

        protected virtual void HighlightCategory(int cate)
        {
            chartGrid.HighlightItem(cate);
        }

        protected virtual void UnhighlightCategory(int cate)
        {
            chartGrid.UnhighlightItem(cate);
        }

        protected virtual void HighlightSeries(int series) { }

        protected virtual void UnhighlightSeries(int series) { }

        protected abstract void UpdateTooltip(int cate, int series);
        
        void ShowTooltip()
        {
            if (chartInfo.tooltip == null) return;
            chartInfo.tooltip.gameObject.SetActive(true);
            chartInfo.tooltip.ResetFade();
            tooltipFollowMouse = true;
            chartInfo.tooltip.SetPosition(chartInfo.mousePos);
            UpdateTooltip(currCate, currSeries);
        }

        void HideTooltip()
        {
            if (chartInfo.tooltip == null) return;
            tooltipFollowMouse = false;
            chartInfo.tooltip.FadeOut();
        }

        void DoMouseTracking()
        {
            if (chartInfo.options.tooltip.share && !chartInfo.isLinear)
            {
                currCate = FindCategory();
                if (lastCate != currCate)
                {
                    if (lastCate >= 0) UnhighlightCategory(lastCate);
                    if (currCate >= 0)
                    {
                        HighlightCategory(currCate);
                        ShowTooltip();
                    }
                    else
                    {
                        HideTooltip();
                    }
                }
            }
            else
            {
                currCate = FindCategory();
                if (lastCate != currCate)
                {
                    if (lastCate >= 0) UnhighlightCategory(lastCate);
                    if (currCate >= 0) HighlightCategory(currCate);
                }
                if (currCate >= 0)
                {
                    currSeries = FindSeries(currCate);
                    if (lastSeries != currSeries)
                    {
                        if (lastSeries >= 0) UnhighlightSeries(lastSeries);
                        if (currSeries >= 0)
                        {
                            HighlightSeries(currSeries);
                            ShowTooltip();
                        }
                        else
                        {
                            HideTooltip();
                        }
                    }
                    else
                    {
                        if (currSeries >= 0 && lastCate != currCate)
                        {
                            if (chartInfo.tooltip != null) UpdateTooltip(currCate, currSeries);
                        }
                    }
                }
                else
                {
                    currSeries = -1;
                    if (lastSeries >= 0)
                    {
                        UnhighlightSeries(lastSeries);
                        HideTooltip();
                    }
                }
            }
            lastCate = currCate;
            lastSeries = currSeries;
        }

        public void OnPointerHover()
        {
            pointerHandler.GetMousePos(out chartInfo.mousePos);
            if (chartInfo.type == ChartType.BarChart || chartInfo.type == ChartType.LineChart)
            {
                chartInfo.gridMousePos = chartInfo.mousePos + chartInfo.GetPaddingChartSize() * 0.5f;
            }
            else
            {
                chartInfo.gridMousePos = chartInfo.mousePos - chartInfo.centerOffset;
                if (chartInfo.type != ChartType.Gauge)
                    chartInfo.mouseAngle = Mathf.Repeat(-Vector2.SignedAngle(new Vector2(0.0f, 1.0f), chartInfo.gridMousePos), 360.0001f);
            }

            DoMouseTracking();
            if (tooltipFollowMouse) chartInfo.tooltip.SetPosition(chartInfo.mousePos);
            if (chartInfo.chartZoom != null && dragHandler.isDragging) chartInfo.chartZoom.SelectRange(currCate);
        }

        public void OnPointerExit()
        {
            if (currCate >= 0)
            {
                UnhighlightCategory(currCate);
                lastCate = currCate = -1;
            }
            if (currSeries >= 0)
            {
                UnhighlightSeries(currSeries);
                lastSeries = currSeries = -1;
            }
            HideTooltip();
        }

        public void OnPointerDown()
        {
            pointerHandler.GetMousePos(out chartInfo.mousePos);
            currCate = FindCategory();
            currSeries = currCate >= 0? FindSeries(currCate) : -1;
            if (chartInfo.chartZoom != null) { chartInfo.chartZoom.SelectRangeStart(currCate); }
            if (chartInfo.chartEvents != null)
            {
                int cate = chartInfo.isLinear ? ((LineChartLinear)this).GetCategory(currCate, currSeries) : currCate;
                chartInfo.chartEvents.itemClickEvent.Invoke(cate, currSeries);
                chartInfo.chartEvents.valueClickEvent.Invoke(chartGrid.GetMouseValueX(), chartGrid.GetMouseValueY());
            }
        }

        protected string GetFormattedHeaderText()
        {
            string format = chartInfo.options.tooltip.headerFormat;
            format = format.Replace("\\n", "\n");
            format = format.Replace("{category}", "");
            return format;
        }

        protected string GetFormattedHeaderText(int cateIndex)
        {
            string format = chartInfo.options.tooltip.headerFormat;
            format = format.Replace("\\n", "\n");
            format = format.Replace("{category}", chartInfo.data.categories[cateIndex]);
            return format;
        }

        protected string GetFormattedPointText()
        {
            float value = chartInfo.data.series[0].data[0].value;
            string format = chartInfo.options.tooltip.pointFormat;
            string nFormat = chartInfo.options.tooltip.pointNumericFormat;
            float sum = chartInfo.yAxisInfo.max;
            format = format.Replace("\\n", "\n");
            format = format.Replace("{category}", "");
            format = format.Replace("{series.name}", chartInfo.data.series[0].name);
            format = format.Replace("{data.value}", chartInfo.GetValueString(value, nFormat));
            format = format.Replace("{data.percentage}", chartInfo.GetPercentageString(value / sum, nFormat));
            return format;
        }

        protected string GetFormattedPointText(int seriesIndex)
        {
            float value = chartInfo.data.series[seriesIndex].data[0].value;
            string format = chartInfo.options.tooltip.pointFormat;
            string nFormat = chartInfo.options.tooltip.pointNumericFormat;
            float sum = chartInfo.chartDataInfo.maxSum;
            format = format.Replace("\\n", "\n");
            format = format.Replace("{category}", "");
            format = format.Replace("{series.name}", chartInfo.data.series[seriesIndex].name);
            format = format.Replace("{data.value}", chartInfo.GetValueString(value, nFormat));
            format = format.Replace("{data.percentage}", chartInfo.GetPercentageString(value / sum, nFormat));
            return format;
        }

        protected string GetFormattedPointText(int seriesIndex, int cateIndex)
        {
            float value = chartInfo.data.series[seriesIndex].data[cateIndex].value;
            string format = chartInfo.options.tooltip.pointFormat;
            string nFormat = chartInfo.options.tooltip.pointNumericFormat;
            float sum = value >= 0.0f ? chartInfo.chartDataInfo.posSum[cateIndex] : chartInfo.chartDataInfo.negSum[cateIndex];
            if (chartInfo.options.tooltip.absoluteValue) value = Mathf.Abs(value);
            format = format.Replace("\\n", "\n");
            format = format.Replace("{category}", chartInfo.data.categories[cateIndex]);
            format = format.Replace("{series.name}", chartInfo.data.series[seriesIndex].name);
            format = format.Replace("{data.value}", chartInfo.GetValueString(value, nFormat));
            format = format.Replace("{data.percentage}", chartInfo.GetPercentageString(value / sum, nFormat));
            return format;
        }

        protected string GetFormattedPointTextLinear(int seriesIndex, int cateIndex)
        {
            float x = chartInfo.data.series[seriesIndex].data[cateIndex].x;
            float value = chartInfo.data.series[seriesIndex].data[cateIndex].value;
            string format = chartInfo.options.tooltip.pointFormat;
            string nFormat = chartInfo.options.tooltip.pointNumericFormat;
            if (chartInfo.options.tooltip.absoluteValue) value = Mathf.Abs(value);
            format = format.Replace("{category}", chartInfo.chartAxisMapper == null ? chartInfo.GetValueString(x, chartInfo.xAxisInfo.labelFormat) : chartInfo.GetDateTimeString(x));
            format = format.Replace("{series.name}", chartInfo.data.series[seriesIndex].name);
            format = format.Replace("{data.value}", chartInfo.GetValueString(value, nFormat));
            format = format.Replace("{data.percentage}", "");
            return format;
        }

        protected string GetFormattedPointTextPos(int seriesIndex, int cateIndex)
        {
            float value = chartInfo.data.series[seriesIndex].data[cateIndex].value;
            string format = chartInfo.options.tooltip.pointFormat;
            string nFormat = chartInfo.options.tooltip.pointNumericFormat;
            float sum = chartInfo.chartDataInfo.posSum[cateIndex];
            format = format.Replace("\\n", "\n");
            format = format.Replace("{category}", chartInfo.data.categories[cateIndex]);
            format = format.Replace("{series.name}", chartInfo.data.series[seriesIndex].name);
            format = format.Replace("{data.value}", chartInfo.GetValueString(value, nFormat));
            format = format.Replace("{data.percentage}", chartInfo.GetPercentageString(value / sum, nFormat));
            return format;
        }

        protected string GetFormattedLabelText()
        {
            float value = chartInfo.data.series[0].data[0].value;
            string format = chartInfo.options.label.format;
            string nFormat = chartInfo.options.label.numericFormat;
            float sum = chartInfo.yAxisInfo.max;
            format = format.Replace("\\n", "\n");
            format = format.Replace("{category}", "");
            format = format.Replace("{series.name}", chartInfo.data.series[0].name);
            format = format.Replace("{data.value}", chartInfo.GetValueString(value, nFormat));
            format = format.Replace("{data.percentage}", chartInfo.GetPercentageString(value / sum, nFormat));
            return format;
        }

        protected string GetFormattedLabelText(int seriesIndex)
        {
            float value = chartInfo.data.series[seriesIndex].data[0].value;
            string format = chartInfo.options.label.format;
            string nFormat = chartInfo.options.label.numericFormat;
            float sum = chartInfo.chartDataInfo.maxSum;
            format = format.Replace("\\n", "\n");
            format = format.Replace("{category}", "");
            format = format.Replace("{series.name}", chartInfo.data.series[seriesIndex].name);
            format = format.Replace("{data.value}", chartInfo.GetValueString(value, nFormat));
            format = format.Replace("{data.percentage}", chartInfo.GetPercentageString(value / sum, nFormat));
            return format;
        }

        protected string GetFormattedLabelText(int seriesIndex, int cateIndex)
        {
            float value = chartInfo.data.series[seriesIndex].data[cateIndex].value;
            string format = chartInfo.options.label.format;
            string nFormat = chartInfo.options.label.numericFormat;
            float sum = value >= 0.0f ? chartInfo.chartDataInfo.posSum[cateIndex] : chartInfo.chartDataInfo.negSum[cateIndex];
            if (chartInfo.options.label.absoluteValue) value = Mathf.Abs(value);
            format = format.Replace("\\n", "\n");
            format = format.Replace("{category}", chartInfo.data.categories[cateIndex]);
            format = format.Replace("{series.name}", chartInfo.data.series[seriesIndex].name);
            format = format.Replace("{data.value}", chartInfo.GetValueString(value, nFormat));
            format = format.Replace("{data.percentage}", chartInfo.GetPercentageString(value / sum, nFormat));
            return format;
        }

        protected string GetFormattedLabelTextLinear(int seriesIndex, int cateIndex)
        {
            float x = chartInfo.data.series[seriesIndex].data[cateIndex].x;
            float value = chartInfo.data.series[seriesIndex].data[cateIndex].value;
            string format = chartInfo.options.label.format;
            string nFormat = chartInfo.options.label.numericFormat;
            if (chartInfo.options.label.absoluteValue) value = Mathf.Abs(value);
            format = format.Replace("{category}", chartInfo.chartAxisMapper == null ? chartInfo.GetValueString(x, chartInfo.xAxisInfo.labelFormat) : chartInfo.GetDateTimeString(x));
            format = format.Replace("{series.name}", chartInfo.data.series[seriesIndex].name);
            format = format.Replace("{data.value}", chartInfo.GetValueString(value, nFormat));
            format = format.Replace("{data.percentage}", "");
            return format;
        }

        protected string GetFormattedLabelTextPos(int seriesIndex, int cateIndex)
        {
            float value = chartInfo.data.series[seriesIndex].data[cateIndex].value;
            string format = chartInfo.options.label.format;
            string nFormat = chartInfo.options.label.numericFormat;
            float sum = chartInfo.chartDataInfo.posSum[cateIndex];
            format = format.Replace("\\n", "\n");
            format = format.Replace("{category}", chartInfo.data.categories[cateIndex]);
            format = format.Replace("{series.name}", chartInfo.data.series[seriesIndex].name);
            format = format.Replace("{data.value}", chartInfo.GetValueString(value, nFormat));
            format = format.Replace("{data.percentage}", chartInfo.GetPercentageString(value / sum, nFormat));
            return format;
        }

        protected Vector2 GetDataRatio(int seriesIndex, int cateIndex)
        {
            Vector2 value = new Vector2();
            if (!chartInfo.data.series[seriesIndex].data[cateIndex].show) return value;
            value.x = (chartInfo.data.series[seriesIndex].data[cateIndex].value - chartInfo.yAxisInfo.baseLine) / chartInfo.yAxisInfo.span;
            value.y = chartInfo.yAxisInfo.baseLineRatio;
            value.x = Mathf.Clamp(value.x, -1.0f, 1.0f);
            value.y = Mathf.Clamp(value.y, -1.0f, 1.0f);
            return value;
        }

        protected Vector2 GetDataRatio(int seriesIndex, int cateIndex, float[] stackValueList)
        {
            Vector2 value = new Vector2();
            switch (chartInfo.options.plotOptions.columnStacking)
            {
                case ColumnStacking.None:
                    value.x = (chartInfo.data.series[seriesIndex].data[cateIndex].value - chartInfo.yAxisInfo.min) / chartInfo.yAxisInfo.span;
                    value.y = 0.0f;
                    break;
                case ColumnStacking.Normal:
                    value.x = (chartInfo.data.series[seriesIndex].data[cateIndex].value - chartInfo.yAxisInfo.min) / chartInfo.yAxisInfo.span;
                    value.y = stackValueList[cateIndex];
                    stackValueList[cateIndex] += value.x;
                    break;
                case ColumnStacking.Percent:
                    value.x = (chartInfo.data.series[seriesIndex].data[cateIndex].value - chartInfo.yAxisInfo.min) / chartInfo.chartDataInfo.posSum[cateIndex] / chartInfo.yAxisInfo.span;
                    value.y = stackValueList[cateIndex];
                    stackValueList[cateIndex] += value.x;
                    break;
                default:
                    break;
            }
            value.y = Mathf.Clamp01(value.y);
            value.x = Mathf.Clamp01(value.y + value.x) - value.y;
            return value;
        }

        protected Vector2 GetDataRatio(int seriesIndex, int cateIndex, float[] stackValueList, float[] stackValueListNeg)
        {
            Vector2 value = new Vector2();
            switch (chartInfo.options.plotOptions.columnStacking)
            {
                case ColumnStacking.None:
                    value.x = (chartInfo.data.series[seriesIndex].data[cateIndex].value - chartInfo.yAxisInfo.baseLine) / chartInfo.yAxisInfo.span;
                    value.y = chartInfo.yAxisInfo.baseLineRatio;
                    break;
                case ColumnStacking.Normal:
                    value.x = (chartInfo.data.series[seriesIndex].data[cateIndex].value - chartInfo.yAxisInfo.baseLine) / chartInfo.yAxisInfo.span;
                    if (chartInfo.data.series[seriesIndex].data[cateIndex].value >= 0.0f)
                    {
                        value.y = chartInfo.yAxisInfo.baseLineRatio + stackValueList[cateIndex];
                        stackValueList[cateIndex] += value.x;
                    }
                    else
                    {
                        value.y = chartInfo.yAxisInfo.baseLineRatio + stackValueListNeg[cateIndex];
                        stackValueListNeg[cateIndex] += value.x;
                    }
                    break;
                case ColumnStacking.Percent:
                    if (chartInfo.data.series[seriesIndex].data[cateIndex].value >= 0.0f)
                    {
                        value.x = (chartInfo.data.series[seriesIndex].data[cateIndex].value / chartInfo.chartDataInfo.posSum[cateIndex] - chartInfo.yAxisInfo.baseLine) / chartInfo.yAxisInfo.span;
                        value.y = chartInfo.yAxisInfo.baseLineRatio + stackValueList[cateIndex];
                        stackValueList[cateIndex] += value.x;
                    }
                    else
                    {
                        value.x = (chartInfo.data.series[seriesIndex].data[cateIndex].value / chartInfo.chartDataInfo.negSum[cateIndex] - chartInfo.yAxisInfo.baseLine) / chartInfo.yAxisInfo.span;
                        value.y = chartInfo.yAxisInfo.baseLineRatio + stackValueListNeg[cateIndex];
                        stackValueListNeg[cateIndex] += value.x;
                    }
                    break;
                default:
                    break;
            }
            return value;
        }
    }
}