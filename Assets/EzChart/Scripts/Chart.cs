using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if CHART_TMPRO
using TMPro;
#endif

namespace ChartUtil
{
    [ExecuteInEditMode]
    public class Chart : MonoBehaviour
    {
        public ChartOptions chartOptions = null;
        public ChartData chartData = null;
        public ChartType chartType = ChartType.BarChart;
        [SerializeField] bool updateOnAwake = true;

        ChartGeneralInfo chartInfo;
        Vector2 offsetMin, offsetMax;

        public void Clear()
        {
            ChartHelper.Clear(transform);
            chartInfo = null;
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
#endif
            if (updateOnAwake) UpdateChart();
        }

        public void ToggleSeries(int index)
        {
            chartInfo.data.series[index].show = !chartInfo.data.series[index].show;
            UpdateChart();
            if (chartInfo.chartEvents != null) chartInfo.chartEvents.seriesToggleEvent.Invoke(index, chartInfo.data.series[index].show);
        }

        public void UpdateChart()
        {
            if (chartData == null || chartOptions == null) return;
            Clear();

            if (chartOptions.plotOptions.generalFont == null)
#if CHART_TMPRO
                chartOptions.plotOptions.generalFont = Resources.Load("Fonts & Materials/LiberationSans SDF", typeof(TMP_FontAsset)) as TMP_FontAsset;
#else
                chartOptions.plotOptions.generalFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif

            chartInfo = new ChartGeneralInfo();
            chartInfo.type = chartType;
            chartInfo.options = chartOptions;
            chartInfo.data = chartData;
            chartInfo.cultureInfo = new System.Globalization.CultureInfo(chartOptions.plotOptions.cultureInfoName);
            chartInfo.chartDataInfo = new ChartDataInfo();
            chartInfo.contentRect = ChartHelper.CreateEmptyRect("Content", transform);
            chartInfo.legendRect = ChartHelper.CreateEmptyRect("Legends", transform);
            chartInfo.chartMixer = GetComponent<ChartMixer>();
            chartInfo.chartEvents = GetComponent<ChartEvents>();
            chartInfo.chartAxisMapper = GetComponent<ChartLinearAxisDateTimeMapper>();
            chartInfo.chartSize = GetComponent<RectTransform>().rect.size;
            offsetMin = offsetMax = Vector2.zero;
            Image img = chartInfo.contentRect.gameObject.AddComponent<Image>();
            img.color = Color.clear;
            img.raycastTarget = chartInfo.options.plotOptions.mouseTracking;

            ChartBase chart = null;
            switch (chartInfo.type)
            {
                case ChartType.BarChart:
                    chartInfo.chartZoom = GetComponent<ChartZoom>();
                    chart = chartInfo.contentRect.gameObject.AddComponent<BarChart>();
                    chartInfo.chartDataInfo.ComputeValue(chartInfo, true, true);
                    break;
                case ChartType.LineChart:
                    chartInfo.chartZoom = GetComponent<ChartZoom>();
                    if (chartInfo.options.xAxis.type == AxisType.Category)
                    {
                        chart = chartInfo.contentRect.gameObject.AddComponent<LineChart>();
                        chartInfo.chartDataInfo.ComputeValue(chartInfo, true, true);
                    }
                    else
                    {
                        chart = chartInfo.contentRect.gameObject.AddComponent<LineChartLinear>();
                        chartInfo.isLinear = true;
                        chartInfo.chartDataInfo.ComputeValueLinear(chartInfo);
                    }
                    break;
                case ChartType.PieChart:
                    chart = chartInfo.contentRect.gameObject.AddComponent<PieChart>();
                    chartInfo.chartDataInfo.ComputeValueFirstData(chartInfo.data);
                    break;
                case ChartType.RoseChart:
                    chart = chartInfo.contentRect.gameObject.AddComponent<RoseChart>();
                    chartInfo.chartDataInfo.ComputeValue(chartInfo, false, false);
                    break;
                case ChartType.RadarChart:
                    chart = chartInfo.contentRect.gameObject.AddComponent<RadarChart>();
                    chartInfo.chartDataInfo.ComputeValue(chartInfo, false, false);
                    break;
                case ChartType.Gauge:
                    chart = chartInfo.contentRect.gameObject.AddComponent<Gauge>();
                    chartInfo.chartDataInfo.max = chartInfo.chartDataInfo.maxSum = chartInfo.options.yAxis.max;
                    break;
                case ChartType.SolidGauge:
                    chart = chartInfo.contentRect.gameObject.AddComponent<SolidGauge>();
                    chartInfo.chartDataInfo.max = chartInfo.chartDataInfo.maxSum = chartInfo.options.yAxis.max;
                    break;
            }

            if (chartInfo.options.tooltip.enable) UpdateTooltip();
            if (chartInfo.options.title.enableMainTitle) UpdateMainTitle();
            if (chartInfo.options.title.enableSubTitle) UpdateSubTitle();
            if (chartInfo.options.legend.enable && chartInfo.type != ChartType.Gauge) UpdateLegend();

            offsetMin.x = Mathf.Clamp(offsetMin.x, 0.0f, chartInfo.chartSize.x * 0.4f);
            offsetMin.y = Mathf.Clamp(offsetMin.y, 0.0f, chartInfo.chartSize.y * 0.4f);
            offsetMax.x = Mathf.Clamp(offsetMax.x, -chartInfo.chartSize.x * 0.4f, 0.0f);
            offsetMax.y = Mathf.Clamp(offsetMax.y, -chartInfo.chartSize.y * 0.4f, 0.0f);

            chartInfo.contentRect.anchorMin = Vector2.zero;
            chartInfo.contentRect.anchorMax = Vector2.one;
            chartInfo.contentRect.offsetMin = offsetMin;
            chartInfo.contentRect.offsetMax = offsetMax;
            chartInfo.chartSize -= offsetMin - offsetMax;
            Vector2 chartSize = chartInfo.chartSize;

            chart.chartInfo = chartInfo;
            chart.UpdateContent();

            if (chartInfo.options.pane.semicircle && chartInfo.options.legend.alignment == TextAnchor.MiddleCenter)
            {
                if (chartInfo.type == ChartType.SolidGauge || chartInfo.type == ChartType.PieChart)
                {
                    float offset = chartInfo.type == ChartType.SolidGauge ? chartInfo.centerOffset.y : chartInfo.centerOffset.y;
                    chartInfo.legendRect.GetComponent<LayoutGroup>().childAlignment = TextAnchor.LowerCenter;
                    chartInfo.legendRect.offsetMin += new Vector2(0.0f, chartSize.y * 0.5f + offset);
                }
            }
        }

        void UpdateTooltip()
        {
            chartInfo.tooltip = ChartHelper.CreateEmptyRect("ChartTooltip", transform).gameObject.AddComponent<ChartTooltip>();

            chartInfo.tooltip.background = chartInfo.tooltip.gameObject.AddComponent<Image>();
            chartInfo.tooltip.background.rectTransform.anchorMin = Vector2.zero;
            chartInfo.tooltip.background.rectTransform.anchorMax = Vector2.zero;
            chartInfo.tooltip.background.sprite = Resources.Load<Sprite>("Images/Chart_Square");
            chartInfo.tooltip.background.color = chartInfo.options.tooltip.backgroundColor;
            chartInfo.tooltip.background.type = Image.Type.Sliced;
            chartInfo.tooltip.background.raycastTarget = false;

            Canvas c = chartInfo.tooltip.gameObject.AddComponent<Canvas>();
            c.overrideSorting = true;
            c.sortingOrder = 10000;
            chartInfo.tooltip.tooltipText = ChartHelper.CreateText("TooltipText", chartInfo.tooltip.transform, chartInfo.options.tooltip.textOption, chartInfo.options.plotOptions.generalFont, TextAnchor.UpperLeft, true);
            chartInfo.tooltip.tooltipText.rectTransform.offsetMin = new Vector2(8, 3);
            chartInfo.tooltip.tooltipText.rectTransform.offsetMax = new Vector2(-8, -3);

            chartInfo.tooltip.chartRect = GetComponent<RectTransform>();
            chartInfo.tooltip.contentRect = chartInfo.contentRect;
            chartInfo.tooltip.inverted = (chartInfo.type == ChartType.BarChart || chartInfo.type == ChartType.LineChart) && chartInfo.options.plotOptions.inverted;
            chartInfo.tooltip.Init();
            chartInfo.tooltip.gameObject.SetActive(false);
        }

        void UpdateMainTitle()
        {
            var mainTitle = ChartHelper.CreateText("MainTitle", transform, chartInfo.options.title.mainTitleOption, chartInfo.options.plotOptions.generalFont);
            mainTitle.text = chartInfo.options.title.mainTitle;
            if (mainTitle.preferredWidth > chartInfo.chartSize.x) ChartHelper.TruncateText(mainTitle, chartInfo.chartSize.x);

            float height = mainTitle.fontSize * 1.4f;
            mainTitle.rectTransform.anchorMin = new Vector2(0.0f, 1.0f);
            mainTitle.rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
            mainTitle.rectTransform.offsetMin = new Vector2(0.0f, -height);
            mainTitle.rectTransform.offsetMax = new Vector2(0.0f, 0.0f);
            offsetMax.y -= height;
        }

        void UpdateSubTitle()
        {
            var subTitle = ChartHelper.CreateText("SubTitle", transform, chartInfo.options.title.subTitleOption, chartInfo.options.plotOptions.generalFont);
            subTitle.text = chartInfo.options.title.subTitle;
            if (subTitle.preferredWidth > chartInfo.chartSize.x) ChartHelper.TruncateText(subTitle, chartInfo.chartSize.x);

            float height = subTitle.fontSize * 1.2f;
            subTitle.rectTransform.anchorMin = new Vector2(0.0f, 1.0f);
            subTitle.rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
            subTitle.rectTransform.offsetMin = new Vector2(0.0f, offsetMax.y - height);
            subTitle.rectTransform.offsetMax = new Vector2(0.0f, offsetMax.y);
            offsetMax.y -= height;
        }

        string GetFormattedLegendText(int seriesIndex, string format, float sum)
        {
            string f = chartInfo.options.legend.numericFormat;
            format = format.Replace("\\n", "\n");
            format = format.Replace("{series.name}", chartInfo.data.series[seriesIndex].name);
            if ((chartInfo.type == ChartType.PieChart || chartInfo.type == ChartType.SolidGauge))
            {
                format = format.Replace("{data.value}", chartInfo.GetValueString(chartInfo.data.series[seriesIndex].data[0].value, f));
                float pValue = chartInfo.data.series[seriesIndex].show && chartInfo.data.series[seriesIndex].data[0].show ? chartInfo.data.series[seriesIndex].data[0].value / sum : 0.0f;
                format = format.Replace("{data.percentage}", chartInfo.GetPercentageString(pValue, f));
            }
            else
            {
                format = format.Replace("{data.value}", "");
                format = format.Replace("{data.percentage}", "");
            }
            return format;
        }

        void UpdateLegend()
        {
            //legend template
            ChartLegend legendTemp = ChartHelper.CreateEmptyRect("ChartLegend", transform).gameObject.AddComponent<ChartLegend>();
            legendTemp.background = legendTemp.gameObject.AddComponent<Image>();
            legendTemp.background.sprite = Resources.Load<Sprite>("Images/Chart_Square");
            legendTemp.background.color = chartInfo.options.legend.backgroundColor;
            legendTemp.background.type = Image.Type.Sliced;
            legendTemp.text = ChartHelper.CreateText("LegendLabel", legendTemp.transform, chartInfo.options.legend.textOption, chartInfo.options.plotOptions.generalFont, TextAnchor.MiddleLeft, true);
            legendTemp.text.rectTransform.offsetMin = new Vector2(chartInfo.options.legend.enableIcon? legendTemp.text.fontSize * 1.5f : 0.0f, 0.0f);
            if (chartInfo.options.legend.enableIcon && 
                !(chartInfo.type == ChartType.BarChart && chartInfo.options.plotOptions.barChartOption.colorByCategories) &&
                !(chartInfo.type == ChartType.RoseChart && chartInfo.options.plotOptions.roseChartOption.colorByCategories))
            {
                legendTemp.icon = ChartHelper.CreateImage("Icon", legendTemp.transform);
                legendTemp.icon.rectTransform.anchorMin = new Vector2(0.0f, 0.5f);
                legendTemp.icon.rectTransform.anchorMax = new Vector2(0.0f, 0.5f);
                legendTemp.icon.rectTransform.sizeDelta = new Vector2(legendTemp.text.fontSize * 0.75f, legendTemp.text.fontSize * 0.75f);
                legendTemp.icon.rectTransform.anchoredPosition = new Vector2(legendTemp.text.fontSize * 0.75f, 0.0f);
            }

            //update items
            float itemMaxWidth = 0.0f;
            float baseWidth = legendTemp.icon == null ? 0.0f : legendTemp.text.fontSize * 2.0f;
            Vector2 itemSumSize = Vector2.zero;
            List<ChartLegend> legendList = new List<ChartLegend>();
            for (int i = 0; i < chartInfo.data.series.Count; ++i)
            {
                ChartLegend legend = Instantiate(legendTemp, chartInfo.legendRect);
                legend.gameObject.name = chartInfo.data.series[i].name;
                legend.text.text = GetFormattedLegendText(i, chartInfo.options.legend.format, chartInfo.chartDataInfo.maxSum);
                legend.gameObject.layer = gameObject.layer;

                float width = legend.text.preferredWidth + baseWidth;
                if (width > itemMaxWidth) itemMaxWidth = width;
                legend.background.rectTransform.sizeDelta = new Vector2(width, legendTemp.text.fontSize * 1.5f);
                legend.Init(i, this, chartInfo.GetSeriesColor(i));
                legend.SetStatus(chartInfo.data.series[i].show);
                itemSumSize += legend.background.rectTransform.sizeDelta;
                legendList.Add(legend);
            }

            //update rect
            TextAnchor alignment;
            float limitW = 0.0f, limitH = 0.0f;
            bool controlW = false, controlH = false;
            float offset = 0.0f;
            if (chartInfo.options.legend.itemLayout == RectTransform.Axis.Horizontal)
            {
                int rows = chartInfo.options.legend.horizontalRows < 1 ? 1 : chartInfo.options.legend.horizontalRows;
                switch (chartInfo.options.legend.alignment)
                {
                    case TextAnchor.LowerCenter:
                    case TextAnchor.LowerLeft:
                    case TextAnchor.LowerRight:
                        alignment = (TextAnchor)((int)chartInfo.options.legend.alignment - 3);
                        limitW = chartInfo.chartSize.x;
                        limitH = chartInfo.chartSize.y - chartInfo.chartSize.x > chartInfo.chartSize.x ? chartInfo.chartSize.y - chartInfo.chartSize.x : chartInfo.chartSize.y * 0.4f;
                        offset = Mathf.Clamp(legendTemp.text.fontSize * 1.5f * rows, 0.0f, limitH);
                        chartInfo.legendRect.anchorMin = new Vector2(0.0f, 0.0f);
                        chartInfo.legendRect.anchorMax = new Vector2(1.0f, 0.0f);
                        chartInfo.legendRect.offsetMin = new Vector2(0.0f, 0.0f);
                        chartInfo.legendRect.offsetMax = new Vector2(0.0f, offset);
                        offsetMin.y += offset;
                        break;
                    case TextAnchor.UpperCenter:
                    case TextAnchor.UpperLeft:
                    case TextAnchor.UpperRight:
                        alignment = (TextAnchor)((int)chartInfo.options.legend.alignment + 3);
                        limitW = chartInfo.chartSize.x;
                        limitH = chartInfo.chartSize.y - chartInfo.chartSize.x > chartInfo.chartSize.x ? chartInfo.chartSize.y - chartInfo.chartSize.x : chartInfo.chartSize.y * 0.4f;
                        offset = Mathf.Clamp(legendTemp.text.fontSize * 1.5f * rows, 0.0f, limitH);
                        chartInfo.legendRect.anchorMin = new Vector2(0.0f, 1.0f);
                        chartInfo.legendRect.anchorMax = new Vector2(1.0f, 1.0f);
                        chartInfo.legendRect.offsetMin = new Vector2(0.0f, offsetMax.y - offset);
                        chartInfo.legendRect.offsetMax = new Vector2(0.0f, offsetMax.y);
                        offsetMax.y -= offset;
                        break;
                    case TextAnchor.MiddleLeft:
                        alignment = TextAnchor.MiddleCenter;
                        limitW = chartInfo.chartSize.x - chartInfo.chartSize.y > chartInfo.chartSize.y ? chartInfo.chartSize.x - chartInfo.chartSize.y : chartInfo.chartSize.x * 0.4f;
                        limitH = chartInfo.chartSize.y;
                        offset = Mathf.Clamp(itemSumSize.x, 0.0f, limitW);
                        chartInfo.legendRect.anchorMin = new Vector2(0.0f, 0.0f);
                        chartInfo.legendRect.anchorMax = new Vector2(0.0f, 1.0f);
                        chartInfo.legendRect.offsetMin = new Vector2(0.0f, 0.0f);
                        chartInfo.legendRect.offsetMax = new Vector2(offset, offsetMax.y);
                        offsetMin.x += offset;
                        break;
                    case TextAnchor.MiddleRight:
                        alignment = TextAnchor.MiddleCenter;
                        limitW = chartInfo.chartSize.x - chartInfo.chartSize.y > chartInfo.chartSize.y ? chartInfo.chartSize.x - chartInfo.chartSize.y : chartInfo.chartSize.x * 0.4f;
                        limitH = chartInfo.chartSize.y;
                        offset = Mathf.Clamp(itemSumSize.x, 0.0f, limitW);
                        chartInfo.legendRect.anchorMin = new Vector2(1.0f, 0.0f);
                        chartInfo.legendRect.anchorMax = new Vector2(1.0f, 1.0f);
                        chartInfo.legendRect.offsetMin = new Vector2(-offset, 0.0f);
                        chartInfo.legendRect.offsetMax = new Vector2(0.0f, offsetMax.y);
                        offsetMax.x -= offset;
                        break;
                    default:
                        alignment = TextAnchor.MiddleCenter;
                        limitW = chartInfo.chartSize.x;
                        limitH = chartInfo.chartSize.y;
                        chartInfo.legendRect.anchorMin = new Vector2(0.0f, 0.0f);
                        chartInfo.legendRect.anchorMax = new Vector2(1.0f, 1.0f);
                        chartInfo.legendRect.offsetMin = new Vector2(0.0f, 0.0f);
                        chartInfo.legendRect.offsetMax = new Vector2(0.0f, offsetMax.y);
                        break;
                }

                if (rows > 1)
                {
                    ChartHelper.AddVerticalLayout(chartInfo.legendRect.gameObject, alignment, true, false);
                    List<RectTransform> legendRow = new List<RectTransform>();
                    List<ChartLegend>[] legends = new List<ChartLegend>[rows];
                    float[] sumWidth = new float[rows];
                    for (int i = 0; i < rows; ++i)
                    {
                        RectTransform row = ChartHelper.CreateEmptyRect("Legends", chartInfo.legendRect);
                        row.sizeDelta = new Vector2(0.0f, legendTemp.text.fontSize * 1.5f);
                        legendRow.Add(row);
                        legends[i] = new List<ChartLegend>();
                    }

                    int num = Mathf.CeilToInt(legendList.Count / (float)rows);
                    for (int i = 0; i < legendList.Count; ++i)
                    {
                        int index = i / num;
                        legendList[i].transform.SetParent(legendRow[index]);
                        legends[index].Add(legendList[i]);
                        sumWidth[index] += legendList[i].background.rectTransform.sizeDelta.x;
                    }

                    for (int i = 0; i < rows; ++i)
                    {
                        if (sumWidth[i] > limitW)
                        {
                            controlW = true;
                            float wLimit = limitW / legends[i].Count - legendTemp.text.fontSize * 1.5f;
                            for (int j = 0; j < legends[i].Count; ++j)
                            {
                                if (legends[i][j].text.preferredWidth > wLimit) ChartHelper.TruncateText(legends[i][j].text, wLimit);
                            }
                        }
                        else
                        {
                            controlW = false;
                        }
                        ChartHelper.AddHorizontalLayout(legendRow[i].gameObject, alignment, controlW, controlH);
                    }
                }
                else
                {
                    if (itemSumSize.x > limitW)
                    {
                        controlW = true;
                        float wLimit = limitW / legendList.Count - legendTemp.text.fontSize * 1.5f;
                        foreach (ChartLegend l in legendList) if (l.text.preferredWidth > wLimit) ChartHelper.TruncateText(l.text, wLimit);
                    }
                    ChartHelper.AddHorizontalLayout(chartInfo.legendRect.gameObject, alignment, controlW, controlH);
                }
            }
            else
            {
                limitH = Mathf.Clamp(itemSumSize.y, 0.0f, chartInfo.chartSize.y * 0.4f);
                switch (chartInfo.options.legend.alignment)
                {
                    case TextAnchor.MiddleLeft:
                    case TextAnchor.UpperLeft:
                    case TextAnchor.LowerLeft:
                        alignment = chartInfo.options.legend.alignment;
                        limitW = chartInfo.chartSize.x - chartInfo.chartSize.y > chartInfo.chartSize.y ? chartInfo.chartSize.x - chartInfo.chartSize.y : chartInfo.chartSize.x * 0.4f;
                        limitH = chartInfo.chartSize.y;
                        offset = Mathf.Clamp(itemMaxWidth, 0.0f, limitW);
                        chartInfo.legendRect.anchorMin = new Vector2(0.0f, 0.0f);
                        chartInfo.legendRect.anchorMax = new Vector2(0.0f, 1.0f);
                        chartInfo.legendRect.offsetMin = new Vector2(0.0f, 0.0f);
                        chartInfo.legendRect.offsetMax = new Vector2(offset, offsetMax.y);
                        offsetMin.x += offset;
                        break;
                    case TextAnchor.MiddleRight:
                    case TextAnchor.UpperRight:
                    case TextAnchor.LowerRight:
                        alignment = (TextAnchor)((int)chartInfo.options.legend.alignment - 2);
                        limitW = chartInfo.chartSize.x - chartInfo.chartSize.y > chartInfo.chartSize.y ? chartInfo.chartSize.x - chartInfo.chartSize.y : chartInfo.chartSize.x * 0.4f;
                        limitH = chartInfo.chartSize.y;
                        offset = Mathf.Clamp(itemMaxWidth, 0.0f, limitW);
                        chartInfo.legendRect.anchorMin = new Vector2(1.0f, 0.0f);
                        chartInfo.legendRect.anchorMax = new Vector2(1.0f, 1.0f);
                        chartInfo.legendRect.offsetMin = new Vector2(-offset, 0.0f);
                        chartInfo.legendRect.offsetMax = new Vector2(0.0f, offsetMax.y);
                        offsetMax.x -= offset;
                        break;
                    case TextAnchor.LowerCenter:
                        alignment = TextAnchor.MiddleCenter;
                        limitW = chartInfo.chartSize.x;
                        limitH = chartInfo.chartSize.y - chartInfo.chartSize.x > chartInfo.chartSize.x ? chartInfo.chartSize.y - chartInfo.chartSize.x : chartInfo.chartSize.y * 0.4f;
                        offset = Mathf.Clamp(itemSumSize.y, 0.0f, limitH);
                        chartInfo.legendRect.anchorMin = new Vector2(0.0f, 0.0f);
                        chartInfo.legendRect.anchorMax = new Vector2(1.0f, 0.0f);
                        chartInfo.legendRect.offsetMin = new Vector2(0.0f, 0.0f);
                        chartInfo.legendRect.offsetMax = new Vector2(0.0f, offset);
                        offsetMin.y += offset;
                        break;
                    case TextAnchor.UpperCenter:
                        alignment = TextAnchor.MiddleCenter;
                        limitW = chartInfo.chartSize.x;
                        limitH = chartInfo.chartSize.y - chartInfo.chartSize.x > chartInfo.chartSize.x ? chartInfo.chartSize.y - chartInfo.chartSize.x : chartInfo.chartSize.y * 0.4f;
                        offset = Mathf.Clamp(itemSumSize.y, 0.0f, limitH);
                        chartInfo.legendRect.anchorMin = new Vector2(0.0f, 1.0f);
                        chartInfo.legendRect.anchorMax = new Vector2(1.0f, 1.0f);
                        chartInfo.legendRect.offsetMin = new Vector2(0.0f, offsetMax.y - offset);
                        chartInfo.legendRect.offsetMax = new Vector2(0.0f, offsetMax.y);
                        offsetMax.y -= offset;
                        break;
                    default:
                        alignment = TextAnchor.MiddleCenter;
                        limitW = chartInfo.chartSize.x;
                        limitH = chartInfo.chartSize.y;
                        chartInfo.legendRect.anchorMin = new Vector2(0.0f, 0.0f);
                        chartInfo.legendRect.anchorMax = new Vector2(1.0f, 1.0f);
                        chartInfo.legendRect.offsetMin = new Vector2(0.0f, 0.0f);
                        chartInfo.legendRect.offsetMax = new Vector2(0.0f, offsetMax.y);
                        break;
                }
                if (itemMaxWidth > limitW)
                {
                    controlW = true;
                    foreach (ChartLegend l in legendList) if (l.text.preferredWidth > limitW) ChartHelper.TruncateText(l.text, limitW);
                }
                if (itemSumSize.y > limitH) controlH = true;
                ChartHelper.AddVerticalLayout(chartInfo.legendRect.gameObject, alignment, controlW, controlH);
            }

            ChartHelper.Destroy(legendTemp.gameObject);
        }
    }
}
