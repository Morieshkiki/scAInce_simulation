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
    [RequireComponent(typeof(Chart))]
    public class ChartZoom : MonoBehaviour
    {
        [Tooltip("Used as a navigator to control another chart")]
        public bool navigatorMode = false;
        public Chart detailChart = null;

        [Header("Reset Button Options")]
        [Tooltip("Reset button text options")]
        public ChartOptions.ChartTextOptions textOption = new ChartOptions.ChartTextOptions(new Color(0.2f, 0.2f, 0.2f, 1.0f), null, 14);
        [Tooltip("Reset button background image")]
        public Sprite buttonImage;
        [Tooltip("Reset button background color")]
        public Color buttonColor = new Color(0.95f, 0.95f, 0.95f, 0.7f);

        bool m_zooming = false;
        public bool isZooming { get { return m_zooming; } }
        float m_start = 0.0f, m_end = 0.0f;
        public float zoomStart { get { return m_start; } }
        public float zoomEnd { get { return m_end; } }
        Image mask;
        Button resetButton;
        ChartGrid chartGrid;
        ChartGeneralInfo chartInfo;
        Chart chart;

        private void Reset()
        {
            if (buttonImage == null) buttonImage = Resources.Load<Sprite>("Images/Chart_Square");
        }

        public void CreateMask(ChartGeneralInfo info, ChartGrid grid, Transform parent)
        {
            chartInfo = info;
            chartGrid = grid;

            mask = ChartHelper.CreateImage("ZoomMask", parent);
            mask.color = chartInfo.options.plotOptions.itemHighlightColor;
            mask.gameObject.SetActive(navigatorMode);

            m_start = chartInfo.xAxisInfo.min;
            m_end = chartInfo.xAxisInfo.max;

            if (navigatorMode)
            {
                UpdateMask();
            }
            else
            {
                chart = GetComponent<Chart>();
                resetButton = ChartHelper.CreateImage("ZoomResetButton", parent.parent, true).gameObject.AddComponent<Button>();
                resetButton.image.rectTransform.anchorMin = resetButton.image.rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
                resetButton.image.rectTransform.pivot = new Vector2(1.0f, 1.0f);
                resetButton.image.rectTransform.anchoredPosition = new Vector2(-2.0f, -2.0f);
                resetButton.image.sprite = buttonImage;
                resetButton.image.color = buttonColor;
                resetButton.onClick.AddListener(ResetZoom);
                EzChartText resetText = ChartHelper.CreateText("Text", resetButton.transform, textOption, chartInfo.options.plotOptions.generalFont, TextAnchor.MiddleCenter, true);
                resetText.text = "Reset";
                resetButton.image.rectTransform.sizeDelta = new Vector2(resetText.preferredWidth + 16, resetText.preferredHeight + 6);
                resetButton.gameObject.SetActive(false);
            }

            if (detailChart != null)
            {
                UpdateTargetChart();
            }
        }

        public void ResetZoom()
        {
            m_zooming = false;
            m_start = chartInfo.xAxisInfo.min;
            m_end = chartInfo.xAxisInfo.max;

            if (navigatorMode)
            {
                UpdateMask();
            }
            else
            {
                if (chart != null)
                {
                    chart.UpdateChart();
                    resetButton.gameObject.SetActive(false);
                }
            }

            if (detailChart != null)
            {
                UpdateTargetChart();
            }
        }

        public void SelectRangeStart(float start)
        {
            m_start = chartInfo.isLinear ? chartInfo.GetXValue(start, chartInfo.options.plotOptions.inverted) : start;
        }
        
        public void SelectRangeEnd()
        {
            mask.gameObject.SetActive(navigatorMode);
            if (m_end == m_start) return;
            if (m_end < m_start)
            {
                float tmp = m_end;
                m_end = m_start;
                m_start = tmp;
            }

            if (navigatorMode)
            {

            }
            else
            {
                m_zooming = true;
                if (chart != null)
                {
                    chart.UpdateChart();
                    resetButton.gameObject.SetActive(true);
                }
                m_zooming = false;
            }

            if (detailChart != null)
            {
                UpdateTargetChart();
            }
        }

        public void SelectRange(float end)
        {
            mask.gameObject.SetActive(end >= 0 || navigatorMode);
            if (!mask.gameObject.activeSelf) return;
            m_end = chartInfo.isLinear ? chartInfo.GetXValue(end, chartInfo.options.plotOptions.inverted) : end;
            UpdateMask();
        }

        void UpdateTargetChart()
        {
            detailChart.chartData = chartInfo.data;
            detailChart.chartOptions.xAxis.autoAxisValues = false;
            detailChart.chartOptions.xAxis.min = m_start;
            detailChart.chartOptions.xAxis.max = m_end;
            detailChart.UpdateChart();
        }

        void UpdateMask()
        {
            if (chartInfo.isLinear)
            {
                if (chartInfo.options.plotOptions.inverted)
                {
                    mask.rectTransform.sizeDelta = new Vector2(chartInfo.chartSize.x, chartInfo.chartSize.y * (Mathf.Abs(m_end - m_start) / chartInfo.xAxisInfo.span));
                    Vector2 offset = -0.5f * (new Vector2(0.0f, chartInfo.chartSize.y) + chartInfo.GetPadding());
                    float pos = (m_end + m_start) * 0.5f - chartInfo.xAxisInfo.min;
                    mask.rectTransform.localPosition = new Vector2(offset.x, chartInfo.chartSize.y * (pos / chartInfo.xAxisInfo.span) + offset.y);
                }
                else
                {
                    mask.rectTransform.sizeDelta = new Vector2(chartInfo.chartSize.x * (Mathf.Abs(m_end - m_start) / chartInfo.xAxisInfo.span), chartInfo.chartSize.y);
                    Vector2 offset = -0.5f * (new Vector2(chartInfo.chartSize.x, 0.0f) + chartInfo.GetPadding());
                    float pos = (m_end + m_start) * 0.5f - chartInfo.xAxisInfo.min;
                    mask.rectTransform.localPosition = new Vector2(chartInfo.chartSize.x * (pos / chartInfo.xAxisInfo.span) + offset.x, offset.y);
                }
            }
            else
            {
                if (chartInfo.options.plotOptions.inverted)
                {
                    mask.rectTransform.sizeDelta = new Vector2(chartInfo.chartSize.x, chartGrid.unitWidth * (Mathf.Abs(m_end - m_start) + 1));
                    Vector2 offset = -0.5f * (new Vector2(0.0f, chartInfo.chartSize.y) + chartInfo.GetPadding());
                    float pos = (m_end + m_start + 1) * 0.5f - chartInfo.xAxisInfo.min;
                    mask.rectTransform.localPosition = new Vector2(offset.x, chartGrid.unitWidth * pos + offset.y);
                }
                else
                {
                    mask.rectTransform.sizeDelta = new Vector2(chartGrid.unitWidth * (Mathf.Abs(m_end - m_start) + 1), chartInfo.chartSize.y);
                    Vector2 offset = -0.5f * (new Vector2(chartInfo.chartSize.x, 0.0f) + chartInfo.GetPadding());
                    float pos = (m_end + m_start + 1) * 0.5f - chartInfo.xAxisInfo.min;
                    mask.rectTransform.localPosition = new Vector2(chartGrid.unitWidth * pos + offset.x, offset.y);
                }
            }
        }
    }
}