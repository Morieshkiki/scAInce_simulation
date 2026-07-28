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
    public class ChartTooltip : MonoBehaviour
    {
        const float offset = 6.0f;

        public EzChartText tooltipText;
        public Image background;
        public RectTransform chartRect;
        public RectTransform contentRect;
        public bool inverted;
        public bool isFading { get { return fadingTimer > 0.0f; } }

        Image triangle;
        float fadingTimer;
        float backgroundAlpha;
        float textAlpha;
        Vector2 pivotMin, pivotMax;
        int currDir = -1;    //up down left right
        Vector2 posOffset;
        Vector2 trianglePos;

        private void Update()
        {
            if (fadingTimer > 0.0f)
            {
                fadingTimer -= Time.deltaTime;
                if (fadingTimer <= 0.2f)
                {
                    SetAlpha(fadingTimer / 0.2f);
                }
                if (fadingTimer <= 0.0f)
                {
                    gameObject.SetActive(false);
                }
            }
        }

        public void Init()
        {
            pivotMin = new Vector2(chartRect.rect.size.x * (-chartRect.pivot.x), chartRect.rect.size.y * (-chartRect.pivot.y));
            pivotMax = new Vector2(chartRect.rect.size.x * (1.0f - chartRect.pivot.x), chartRect.rect.size.y * (1.0f - chartRect.pivot.y));

            triangle = ChartHelper.CreateImage("Triangle", transform, false);
            triangle.sprite = Resources.Load<Sprite>("Images/Chart_Triangle");
            triangle.rectTransform.sizeDelta = new Vector2(offset * 2.0f, offset);
            triangle.color = background.color;

            backgroundAlpha = background.color.a;
            textAlpha = tooltipText.color.a;
            if (inverted) SetDirection(3); 
            else SetDirection(0);
        }

        public void SetText(string text)
        {
            tooltipText.text = text;
            background.rectTransform.sizeDelta = new Vector2(tooltipText.preferredWidth + 16, tooltipText.preferredHeight + 6);
        }

        public void SetPosition(Vector3 pos, float axisDir = 1.0f)
        {
            int d = 0;
            if (inverted) d = axisDir > 0.0f ? 3 : 2;
            else d = axisDir > 0.0f ? 0 : 1;
            SetPosition(pos, d);
        }

        public void SetPosition(Vector3 pos, int direction)
        {
            transform.localPosition = contentRect.localPosition + pos;
            SetDirection(direction);
            ValidatePosition();
        }

        void ValidatePosition()
        {
            Vector2 pos = transform.localPosition;
            Vector2 size = background.rectTransform.sizeDelta;
            Vector2 triangleOffset = new Vector2();
            switch (currDir)
            {
                case 0://up
                    if (pos.y > pivotMax.y - size.y) SetDirection(1);
                    if (pos.x > pivotMax.x - size.x * 0.5f) triangleOffset = new Vector2(pivotMax.x - size.x * 0.5f - pos.x, 0.0f);
                    if (pos.x < pivotMin.x + size.x * 0.5f) triangleOffset = new Vector2(pivotMin.x + size.x * 0.5f - pos.x, 0.0f);
                    break;
                case 1://down
                    if (pos.y < pivotMin.y + size.y) SetDirection(0);
                    if (pos.x > pivotMax.x - size.x * 0.5f) triangleOffset = new Vector2(pivotMax.x - size.x * 0.5f - pos.x, 0.0f);
                    if (pos.x < pivotMin.x + size.x * 0.5f) triangleOffset = new Vector2(pivotMin.x + size.x * 0.5f - pos.x, 0.0f);
                    break;
                case 2://left
                    if (pos.x < pivotMin.x + size.x) SetDirection(3);
                    if (pos.y > pivotMax.y - size.y * 0.5f) triangleOffset = new Vector2(0.0f, pivotMax.y - size.y * 0.5f - pos.y);
                    if (pos.y < pivotMin.y + size.y * 0.5f) triangleOffset = new Vector2(0.0f, pivotMin.y + size.y * 0.5f - pos.y);
                    break;
                case 3://right
                    if (pos.x > pivotMax.x - size.x) SetDirection(2);
                    if (pos.y > pivotMax.y - size.y * 0.5f) triangleOffset = new Vector2(0.0f, pivotMax.y - size.y * 0.5f - pos.y);
                    if (pos.y < pivotMin.y + size.y * 0.5f) triangleOffset = new Vector2(0.0f, pivotMin.y + size.y * 0.5f - pos.y);
                    break;
                default:
                    break;
            }
            transform.localPosition = pos + posOffset + triangleOffset;
            triangle.rectTransform.anchoredPosition = trianglePos - triangleOffset;
        }

        public void FadeOut()
        {
            fadingTimer = 0.6f;
        }

        public void ResetFade()
        {
            fadingTimer = 0.0f;
            SetAlpha(1.0f);
        }

        void SetDirection(int d)
        {
            if (currDir == d) return;
            currDir = d;
            switch (currDir)
            {
                case 0://up
                    background.rectTransform.pivot = new Vector2(0.5f, 0.0f);
                    posOffset = new Vector2(0.0f, offset);
                    triangle.rectTransform.anchorMin = triangle.rectTransform.anchorMax = new Vector2(0.5f, 0.0f);
                    triangle.rectTransform.anchoredPosition = trianglePos = new Vector2(0.0f, -(offset * 0.5f - 1));
                    triangle.rectTransform.localEulerAngles = new Vector3(0.0f, 0.0f, 180.0f);
                    break;
                case 1://down
                    background.rectTransform.pivot = new Vector2(0.5f, 1.0f);
                    posOffset = new Vector2(0.0f, -offset);
                    triangle.rectTransform.anchorMin = triangle.rectTransform.anchorMax = new Vector2(0.5f, 1.0f);
                    triangle.rectTransform.anchoredPosition = trianglePos = new Vector2(0.0f, (offset * 0.5f - 1));
                    triangle.rectTransform.localEulerAngles = new Vector3(0.0f, 0.0f, 0.0f);
                    break;
                case 2://left
                    background.rectTransform.pivot = new Vector2(1.0f, 0.5f);
                    posOffset = new Vector2(-offset, 0.0f);
                    triangle.rectTransform.anchorMin = triangle.rectTransform.anchorMax = new Vector2(1.0f, 0.5f);
                    triangle.rectTransform.anchoredPosition = trianglePos = new Vector2((offset * 0.5f - 1), 0.0f);
                    triangle.rectTransform.localEulerAngles = new Vector3(0.0f, 0.0f, -90.0f);
                    break;
                case 3://right
                    background.rectTransform.pivot = new Vector2(0.0f, 0.5f);
                    posOffset = new Vector2(offset, 0.0f);
                    triangle.rectTransform.anchorMin = triangle.rectTransform.anchorMax = new Vector2(0.0f, 0.5f);
                    triangle.rectTransform.anchoredPosition = trianglePos = new Vector2(-(offset * 0.5f - 1), 0.0f);
                    triangle.rectTransform.localEulerAngles = new Vector3(0.0f, 0.0f, 90.0f);
                    break;
                default:
                    break;
            }
        }

        void SetAlpha(float a)
        {
            Color c = background.color;
            c.a = backgroundAlpha * a;
            background.color = c;
            triangle.color = c;
            c = tooltipText.color;
            c.a = textAlpha;
            tooltipText.color = c;
        }
    }
}
