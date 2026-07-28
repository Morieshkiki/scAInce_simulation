using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if CHART_TMPRO
using TMPro;
#endif

namespace ChartUtil
{
    public static class ChartHelper
    {
        public static void Destroy(Object obj)
        {
#if UNITY_EDITOR
            GameObject.DestroyImmediate(obj);
#else
            GameObject.Destroy(obj);
#endif
        }

        public static void Clear(Transform trans)
        {
            for (int i = trans.childCount - 1; i >= 0; --i)
            {
                Destroy(trans.GetChild(i).gameObject);
            }
        }

        //=========================================================
        // Create components
        //=========================================================

        public static Image CreateImage(string name, Transform parent, bool raycast = false, bool setMax = false)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            go.AddComponent<RectTransform>();
            Image img = go.AddComponent<Image>();
            img.raycastTarget = raycast;
            if (setMax) SetRectTransformMax(img.rectTransform);
            return img;
        }

        public static RectTransform CreateEmptyRect(string name, Transform parent, bool setMax = false)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            RectTransform t = go.AddComponent<RectTransform>();
            if (setMax) SetRectTransformMax(t);
            return t;
        }

        public static void SetRectTransformMax(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        public static HorizontalLayoutGroup AddHorizontalLayout(GameObject go, TextAnchor alignment, bool controlWidth = true, bool controlHeight = true)
        {
            HorizontalLayoutGroup horiLayout = go.gameObject.AddComponent<HorizontalLayoutGroup>();
            horiLayout.childControlWidth = controlWidth;
            horiLayout.childControlHeight = controlHeight;
            horiLayout.childForceExpandWidth = controlWidth;
            horiLayout.childForceExpandHeight = controlHeight;
            horiLayout.childAlignment = alignment;
            return horiLayout;
        }

        public static VerticalLayoutGroup AddVerticalLayout(GameObject go, TextAnchor alignment, bool controlWidth = true, bool controlHeight = true)
        {
            VerticalLayoutGroup vertLayout = go.gameObject.AddComponent<VerticalLayoutGroup>();
            vertLayout.childControlWidth = controlWidth;
            vertLayout.childControlHeight = controlHeight;
            vertLayout.childForceExpandWidth = controlWidth;
            vertLayout.childForceExpandHeight = controlHeight;
            vertLayout.childAlignment = alignment;
            return vertLayout;
        }

#if CHART_TMPRO
        static Dictionary<TextAnchor, TextAlignmentOptions> TMProAlignmentDict = new Dictionary<TextAnchor, TextAlignmentOptions>()
        {
            {TextAnchor.LowerLeft, TextAlignmentOptions.BottomLeft },
            {TextAnchor.LowerCenter, TextAlignmentOptions.Bottom },
            {TextAnchor.LowerRight, TextAlignmentOptions.BottomRight },
            {TextAnchor.MiddleLeft, TextAlignmentOptions.MidlineLeft },
            {TextAnchor.MiddleCenter, TextAlignmentOptions.Midline },
            {TextAnchor.MiddleRight, TextAlignmentOptions.MidlineRight },
            {TextAnchor.UpperLeft, TextAlignmentOptions.TopLeft },
            {TextAnchor.UpperCenter, TextAlignmentOptions.Top },
            {TextAnchor.UpperRight, TextAlignmentOptions.TopRight },
        };

        public static TextMeshProUGUI CreateText(string name, Transform parent, ChartOptions.ChartTextOptions option, TMP_FontAsset generalFont, TextAnchor anchor = TextAnchor.MiddleCenter, bool setMax = false)
        {
            TextMeshProUGUI t = null;
            if (option.customizedText == null)
            {
                GameObject go = new GameObject(name);
                go.transform.SetParent(parent, false);
                go.layer = parent.gameObject.layer;
                go.AddComponent<RectTransform>();
                t = go.AddComponent<TextMeshProUGUI>();
                t.raycastTarget = false;
                t.enableWordWrapping = false;
                t.overflowMode = TextOverflowModes.Overflow;
                t.alignment = TMProAlignmentDict[anchor];
                t.color = option.color;
                t.font = option.font == null ? generalFont : option.font;
                t.fontSize = option.fontSize;
                if (setMax) SetRectTransformMax(t.rectTransform);
            }
            else
            {
                t = GameObject.Instantiate(option.customizedText, parent);
                t.gameObject.name = name;
                t.gameObject.layer = parent.gameObject.layer;
                t.gameObject.SetActive(true);
                t.raycastTarget = false;
                t.enableWordWrapping = false;
                t.overflowMode = TextOverflowModes.Overflow;
                t.alignment = TMProAlignmentDict[anchor];
                t.rectTransform.localPosition = Vector3.zero;
                t.rectTransform.localRotation = Quaternion.identity;
                t.rectTransform.localScale = Vector3.one;
                t.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                if (setMax) SetRectTransformMax(t.rectTransform);
                else
                {
                    t.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    t.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                }
            }
            return t;
        }

        public static void TruncateText(TextMeshProUGUI t, float maxWidth)
        {float w = t.fontSize * 2;
            var info = t.textInfo.characterInfo;
            for (int i = 0; i < info.Length; ++i)
            {
                if (t.GetPreferredValues(t.text.Substring(0, i)).x + w > maxWidth)
                {
                    t.text = t.text.Substring(0, i) + "...";
                    break;
                }
            }
        }

        public static TextAlignmentOptions ConvertAlignment(TextAnchor anchor)
        {
            return TMProAlignmentDict[anchor];
        }
#else
        public static Text CreateText(string name, Transform parent, ChartOptions.ChartTextOptions option, Font generalFont, TextAnchor anchor = TextAnchor.MiddleCenter, bool setMax = false)
        {
            Text t = null;
            if (option.customizedText == null)
            {
                GameObject go = new GameObject(name);
                go.transform.SetParent(parent, false);
                go.layer = parent.gameObject.layer;
                go.AddComponent<RectTransform>();
                t = go.AddComponent<Text>();
                t.raycastTarget = false;
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.verticalOverflow = VerticalWrapMode.Overflow;
                t.alignment = anchor;
                t.color = option.color;
                t.font = option.font == null ? generalFont : option.font;
                t.fontSize = option.fontSize;
                if (setMax) SetRectTransformMax(t.rectTransform);
            }
            else
            {
                t = GameObject.Instantiate(option.customizedText, parent);
                t.gameObject.name = name;
                t.gameObject.layer = parent.gameObject.layer;
                t.gameObject.SetActive(true);
                t.raycastTarget = false;
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.verticalOverflow = VerticalWrapMode.Overflow;
                t.alignment = anchor;
                t.rectTransform.localPosition = Vector3.zero;
                t.rectTransform.localRotation = Quaternion.identity;
                t.rectTransform.localScale = Vector3.one;
                t.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                if (setMax) SetRectTransformMax(t.rectTransform);
                else
                {
                    t.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    t.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                }
            }
            return t;
        }

        //public static float FindTextWidth(Text t)
        //{
        //    float width = 0.0f;
        //    t.font.RequestCharactersInTexture(t.text, t.fontSize);
        //    CharacterInfo charInfo;
        //    if (t.font.dynamic)
        //    {
        //        for (int i = 0; i < t.text.Length; ++i)
        //        {
        //            t.font.GetCharacterInfo(t.text[i], out charInfo, t.fontSize);
        //            width += charInfo.advance;
        //        }
        //    }
        //    else
        //    {
        //        for (int i = 0; i < t.text.Length; ++i)
        //        {
        //            t.font.GetCharacterInfo(t.text[i], out charInfo, t.font.fontSize);
        //            width += charInfo.advance * ((float)t.fontSize / t.font.fontSize);
        //        }
        //    }
        //    return width;
        //}

        public static void TruncateText(Text t, float maxWidth)
        {
            float width = 0.0f;
            t.font.RequestCharactersInTexture(t.text, t.fontSize);
            CharacterInfo charInfo;
            float w = t.fontSize * 2;
            if (t.font.dynamic)
            {
                for (int i = 0; i < t.text.Length - 3; ++i)
                {
                    if (width + w > maxWidth)
                    {
                        t.text = t.text.Substring(0, i) + "...";
                        break;
                    }
                    t.font.GetCharacterInfo(t.text[i], out charInfo, t.fontSize);
                    width += charInfo.advance;
                }
            }
            else
            {
                for (int i = 0; i < t.text.Length - 3; ++i)
                {
                    if (width + w > maxWidth)
                    {
                        t.text = t.text.Substring(0, i) + "...";
                        break;
                    }
                    t.font.GetCharacterInfo(t.text[i], out charInfo, t.font.fontSize);
                    width += charInfo.advance * ((float)t.fontSize / t.font.fontSize);
                }
            }
        }

        public static TextAnchor ConvertAlignment(TextAnchor anchor)
        {
            return anchor;
        }
#endif

        //=========================================================
        // String format
        //=========================================================

        public static string FindFloatDisplayFormat(float value)
        {
            string format;
            value = Mathf.Abs(value);
            if (value >= 100.0f) format = "N0";
            else if (value >= 10.0f) format = "N1";
            else if (value >= 1.0f) format = "N2";
            else format = "N" + (FindFloatDisplayPrecision(value) + 2).ToString();
            return format;
        }

        public static int FindFloatDisplayPrecision(float f)
        {
            if (f == 0.0f) return 0;

            string s = f.ToString("f5");
            int i;
            for (i = 2; i < s.Length; ++i)
            {
                if (s[i] != '0') break;
            }

            return i - 1;
        }

        public static int FindDoubleDisplayPrecision(double f)
        {
            if (f == 0.0f) return 0;

            string s = f.ToString("f10");
            int i;
            for (i = 2; i < s.Length; ++i)
            {
                if (s[i] != '0') break;
            }

            return i - 1;
        }

        public static int FindIntegerLength(int i)
        {
            return i.ToString().Length;
        }

        //=========================================================
        //Caculation
        //=========================================================

        public static Vector2 BerzierCurve(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return Mathf.Pow(1 - t, 3) * p0 + 3 * Mathf.Pow(1 - t, 2) * t * p1 + 3 * (1 - t) * t * t * p2 + Mathf.Pow(t, 3) * p3;
        }

        public static Vector2 LineIntersection(Vector2[] points) //p0-p2, p1-p3
        {
            float t = (points[1].x - points[0].x) * (points[0].y - points[3].y) - (points[0].x - points[3].x) * (points[1].y - points[0].y);
            t /= (points[1].x - points[2].x) * (points[0].y - points[3].y) - (points[0].x - points[3].x) * (points[1].y - points[2].y);
            Vector2 pIntersect = points[1] + (points[2] - points[1]) * t;
            return pIntersect;
        }
    }
}
