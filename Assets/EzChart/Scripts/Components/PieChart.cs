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
    [ExecuteInEditMode]
    public class PieChart : ChartBase
    {
        PieChartCircle[] pieList;
        Vector2 circleSize;
        float startAngle, endAngle;
        float fitRatio = 1.0f;

        protected override void UpdateGrid()
        {
            float tmp = chartInfo.options.pane.semicircle ? 2.0f : 1.0f;
            circleSize.x = (chartInfo.chartSize.x < chartInfo.chartSize.y * tmp ? chartInfo.chartSize.x : chartInfo.chartSize.y * tmp) * Mathf.Clamp01(chartInfo.options.pane.outerSize) * 0.9f;
            startAngle = chartInfo.options.pane.startAngle;
            endAngle = chartInfo.options.pane.endAngle;
            if (chartInfo.options.pane.semicircle)
            {
                startAngle = Mathf.Clamp(startAngle, -90.0f, 90.0f);
                endAngle = Mathf.Clamp(endAngle, -90.0f, 90.0f);
                chartInfo.centerOffset.y = -circleSize.x * 0.25f;
            }
            chartInfo.gridRect.anchoredPosition = chartInfo.centerOffset;
            chartInfo.dataRect.anchoredPosition = chartInfo.centerOffset;
            chartInfo.labelRect.anchoredPosition = chartInfo.centerOffset;
            
            circleSize.x *= 0.95f;
            circleSize.y = circleSize.x * Mathf.Clamp(chartInfo.options.pane.innerSize, 0.0f, chartInfo.options.pane.outerSize);
        }

        protected override void UpdateItemMaterial()
        {
            itemMat = new Material[1];
            itemMatFade = new Material[1];
            float smoothness = Mathf.Clamp01(4.0f / (circleSize.x - circleSize.y));

            itemMat[0] = new Material(Resources.Load<Material>("Materials/Chart_VBlur"));
            itemMat[0].SetFloat("_Smoothness", smoothness);
            itemMatFade[0] = new Material(itemMat[0]);
            itemMatFade[0].color = new Color(1.0f, 1.0f, 1.0f, fadeValue);
        }

        protected override void UpdateItems()
        {
            pieList = new PieChartCircle[chartInfo.data.series.Count];
            float stack = startAngle;
            float total = Mathf.Repeat(endAngle - startAngle, 360.0001f);
            for (int i = 0; i < chartInfo.data.series.Count; ++i)
            {
                if (!IsValid(i)) continue;
                RectTransform seriesRect = ChartHelper.CreateEmptyRect(chartInfo.data.series[i].name, chartInfo.dataRect);
                seriesRect.sizeDelta = new Vector2(circleSize.x, circleSize.x);
                seriesRect.SetAsFirstSibling();

                //pie
                pieList[i] = ChartHelper.CreateEmptyRect("Pie", seriesRect, true).gameObject.AddComponent<PieChartCircle>();
                pieList[i].material = itemMat[0];
                pieList[i].color = chartInfo.GetSeriesColor(i);
                pieList[i].angle = total * chartInfo.data.series[i].data[0].value / chartInfo.chartDataInfo.maxSum;
                //if (chartInfo.options.xAxis.reversed)
                //{
                //    pieList[i].center = stack - pieList[i].angle * 0.5f + 360.0f;
                //    stack -= pieList[i].angle;
                //}
                //else
                {
                    pieList[i].center = Mathf.Repeat(stack + pieList[i].angle * 0.5f, 360.0001f);
                    stack += pieList[i].angle;
                }
                pieList[i].innerSize = chartInfo.options.pane.innerSize;
                pieList[i].separation = chartInfo.options.plotOptions.pieChartOption.itemSeparation;
                pieList[i].RefreshBuffer();
            }
        }

        protected override void UpdateLabels()
        {
            //templates
            EzChartText labelTemp = ChartHelper.CreateText("Label", transform, chartInfo.options.label.textOption, chartInfo.options.plotOptions.generalFont);
            labelTemp.rectTransform.sizeDelta = Vector2.zero;
            labelTemp.rectTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, chartInfo.options.label.rotation);

            Image lineTemp = ChartHelper.CreateImage("Line", transform);
            lineTemp.sprite = Resources.Load<Sprite>("Images/Chart_Line");
            lineTemp.type = Image.Type.Sliced;
            lineTemp.rectTransform.pivot = new Vector2(0.5f, 0.0f);
            Image[] lineList = chartInfo.options.label.enable ? new Image[chartInfo.data.series.Count] : null;

            float labelDist = Mathf.Lerp(circleSize.y * 0.5f, circleSize.x * 0.5f, chartInfo.options.label.anchoredPosition) + chartInfo.options.label.offset;
            EzChartText[] labelList = chartInfo.options.label.enable ? new EzChartText[chartInfo.data.series.Count] : null;
            for (int i = 0; i < chartInfo.data.series.Count; ++i)
            {
                if (!IsValid(i)) continue;
                RectTransform seriesLabelRect = ChartHelper.CreateEmptyRect(chartInfo.data.series[i].name, chartInfo.labelRect, true);
                seriesLabelRect.SetAsFirstSibling();

                labelList[i] = Instantiate(labelTemp, seriesLabelRect);
                labelList[i].rectTransform.pivot = pieList[i].direction.x > 0.0f ? new Vector2(0.0f, 0.5f) : new Vector2(1.0f, 0.5f);
                labelList[i].text = GetFormattedLabelText(i);
                labelList[i].rectTransform.anchoredPosition = pieList[i].direction * labelDist;
                if (chartInfo.options.label.offset > 0.0f) lineList[i] = Instantiate(lineTemp, seriesLabelRect);
                //labelList[i].gameObject.SetActive(circle.fillAmount > 0.01f);
            }

            ChartHelper.Destroy(labelTemp.gameObject);
            ChartHelper.Destroy(lineTemp.gameObject);

            //adjust label position
            if (chartInfo.options.label.offset < 0.0f) return;

            float height = labelTemp.fontSize * 1.2f;
            List<int> label_right = new List<int>();
            List<int> label_left = new List<int>();

            for (int i = 0; i < labelList.Length; ++i)
            {
                if (labelList[i] == null || !labelList[i].gameObject.activeSelf) continue;
                if (labelList[i].rectTransform.anchoredPosition.x > 0.0f) label_right.Add(i);
                else label_left.Add(i);
            }
            label_left.Reverse();

            //right
            float y = 99999.0f;
            foreach (int i in label_right)
            {
                if (labelList[i].rectTransform.anchoredPosition.y < y - height)
                {
                    y = labelList[i].rectTransform.anchoredPosition.y;
                }
                else
                {
                    y -= height;
                    if (y < -labelDist) break;
                    float x = Mathf.Sqrt(labelDist * labelDist - y * y);
                    labelList[i].rectTransform.anchoredPosition = new Vector2(x, y);
                }
            }

            //reverse right
            y = -99999.0f;
            label_right.Reverse();
            foreach (int i in label_right)
            {
                if (labelList[i].rectTransform.anchoredPosition.y > y + height)
                {
                    y = labelList[i].rectTransform.anchoredPosition.y;
                    labelList[i].rectTransform.anchoredPosition = new Vector2(labelList[i].rectTransform.anchoredPosition.x + chartInfo.options.label.offset * 0.5f, y);
                }
                else
                {
                    y += height;
                    if (y > labelDist) break;
                    float x = Mathf.Sqrt(labelDist * labelDist - y * y);
                    labelList[i].rectTransform.anchoredPosition = new Vector2(x + chartInfo.options.label.offset * 0.5f, y);
                }
            }

            //left
            y = 99999.0f;
            foreach (int i in label_left)
            {
                if (labelList[i].rectTransform.anchoredPosition.y < y - height)
                {
                    y = labelList[i].rectTransform.anchoredPosition.y;
                }
                else
                {
                    y -= height;
                    if (y <= -labelDist) break;
                    float x = -Mathf.Sqrt(labelDist * labelDist - y * y);
                    labelList[i].rectTransform.anchoredPosition = new Vector2(x, y);
                }
            }

            //reverse left
            y = -99999.0f;
            label_left.Reverse();
            foreach (int i in label_left)
            {
                if (labelList[i].rectTransform.anchoredPosition.y > y + height)
                {
                    y = labelList[i].rectTransform.anchoredPosition.y;
                    labelList[i].rectTransform.anchoredPosition = new Vector2(labelList[i].rectTransform.anchoredPosition.x - chartInfo.options.label.offset * 0.5f, y);
                }
                else
                {
                    y += height;
                    if (y > labelDist) break;
                    float x = -Mathf.Sqrt(labelDist * labelDist - y * y);
                    labelList[i].rectTransform.anchoredPosition = new Vector2(x - chartInfo.options.label.offset * 0.5f, y);
                }
            }

            //find max delta
            float delta_xMax = 0.0f, delta_yMax = 0.0f;
            for (int i = 0; i < labelList.Length; ++i)
            {
                if (labelList[i] == null || !labelList[i].gameObject.activeSelf) continue;

                float wLimit = chartInfo.options.label.bestFit ? chartInfo.chartSize.x * 0.3f : Mathf.Clamp(chartInfo.chartSize.x * 0.5f - Mathf.Abs(labelList[i].rectTransform.anchoredPosition.x), 0.0f, chartInfo.chartSize.x * 0.3f);
                float width = labelList[i].preferredWidth;
                if (width > wLimit) { width = wLimit; ChartHelper.TruncateText(labelList[i], wLimit); }
                labelList[i].rectTransform.sizeDelta = new Vector2(width, height);

                float delta_x = Mathf.Abs(labelList[i].rectTransform.anchoredPosition.x) + width - chartInfo.chartSize.x * 0.5f;
                if (delta_x > delta_xMax) delta_xMax = delta_x;
                float delta_y = Mathf.Abs(labelList[i].rectTransform.anchoredPosition.y) + height * 0.5f - chartInfo.chartSize.y * 0.5f + chartInfo.centerOffset.y;
                if (delta_y > delta_yMax) delta_yMax = delta_y;
            }
            delta_xMax = Mathf.Clamp(delta_xMax, 0.0f, chartInfo.chartSize.x * 0.3f);
            delta_yMax = Mathf.Clamp(delta_yMax, 0.0f, chartInfo.chartSize.y * 0.3f);

            float delta = delta_xMax > delta_yMax ? delta_xMax : delta_yMax;
            fitRatio = (circleSize.x * 0.5f - delta) / (circleSize.x * 0.5f);
            for (int i = 0; i < chartInfo.data.series.Count; ++i)
            {
                if (pieList[i] == null) continue;
                pieList[i].rectTransform.sizeDelta *= fitRatio;
                labelList[i].rectTransform.anchoredPosition *= fitRatio;
            }

            //update line
            for (int i = 0; i < lineList.Length; ++i)
            {
                if (lineList[i] == null || !lineList[i].gameObject.activeSelf) continue;

                Vector2 p1 = pieList[i].direction * circleSize.x * 0.5f * fitRatio;
                Vector2 p2 = labelList[i].rectTransform.anchoredPosition;
                Vector2 dif = p2 - p1;

                lineList[i].color = pieList[i].color;
                lineList[i].rectTransform.anchoredPosition = p1;
                lineList[i].rectTransform.sizeDelta = new Vector2(labelTemp.fontSize / 6.0f, dif.magnitude);
                lineList[i].rectTransform.localRotation = Quaternion.FromToRotation(Vector2.up, dif);
            }
        }

        protected override void UpdateBackground()
        {
            chartInfo.background = ChartHelper.CreateImage("Background", chartInfo.contentRect);
            chartInfo.background.transform.SetAsFirstSibling();
            chartInfo.background.sprite = Resources.Load<Sprite>("Images/Chart_Circle_512x512");
            chartInfo.background.color = chartInfo.options.plotOptions.backgroundColor;
            chartInfo.background.rectTransform.sizeDelta = new Vector2(circleSize.x, circleSize.x) * fitRatio;
            chartInfo.background.rectTransform.anchoredPosition = chartInfo.centerOffset;
            if (chartInfo.options.pane.semicircle)
            {
                chartInfo.background.type = Image.Type.Filled;
                chartInfo.background.fillMethod = Image.FillMethod.Radial360;
                chartInfo.background.fillOrigin = 3;
                chartInfo.background.fillAmount = 0.5f;
            }
        }

        protected override void UpdateHighlight()
        {
            //highlight = ChartHelper.CreateImage("Highlight", gridRect);
            //highlight.sprite = Resources.Load<Sprite>("Images/Chart_Circle_512x512");
            //highlight.color = chartInfo.options.plotOptions.itemHighlightColor;
            //highlight.rectTransform.sizeDelta = new Vector2(circleSize.x, circleSize.x) * fitRatio;
            //if (chartInfo.options.pane.semicircle)
            //{
            //    highlight.type = Image.Type.Filled;
            //    highlight.fillMethod = Image.FillMethod.Radial360;
            //    highlight.fillOrigin = 3;
            //    highlight.fillAmount = 0.5f;
            //}
            //highlight.gameObject.SetActive(false);
        }

        protected override int FindCategory()
        {
            int index = -1;
            if (chartInfo.gridMousePos.sqrMagnitude > 0.25f * circleSize.x * circleSize.x || chartInfo.gridMousePos.sqrMagnitude < 0.25f * circleSize.y * circleSize.y) return -1;
            for (int i = 0; i < chartInfo.data.series.Count; ++i)
            {
                if (!IsValid(i)) continue;
                float tmp = pieList[i].angle * 0.5f;
                if (Mathf.Abs(chartInfo.mouseAngle - pieList[i].center) <= tmp) { index = i; break; }
            }
            return index;
        }

        protected override void HighlightCategory(int cate)
        {
            //highlight.gameObject.SetActive(true);
            pieList[cate].rectTransform.localScale /= 0.95f;
            for (int i = 0; i < pieList.Length; ++i)
            {
                if (pieList[i] == null || i == cate) continue;
                pieList[i].material = itemMatFade[0];
            }
        }

        protected override void UnhighlightCategory(int cate)
        {
            //highlight.gameObject.SetActive(false);
            pieList[cate].rectTransform.localScale = Vector3.one;
            for (int i = 0; i < pieList.Length; ++i)
            {
                if (pieList[i] == null || i == cate) continue;
                pieList[i].material = itemMat[0];
            }
        }

        protected override void UpdateTooltip(int cate, int series)
        {
            string tooltipText = GetFormattedHeaderText();
            if (tooltipText.Length > 0) tooltipText += "\n";
            tooltipText += GetFormattedPointText(cate);
            chartInfo.tooltip.SetText(tooltipText);
        }

        public bool IsValid(int seriesIndex)
        {
            return chartInfo.data.series[seriesIndex].show && chartInfo.data.series[seriesIndex].data.Count > 0 && chartInfo.data.series[seriesIndex].data[0].show && chartInfo.data.series[seriesIndex].data[0].value > 0.0f;
        }
    }
}
