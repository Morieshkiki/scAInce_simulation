using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChartUtil
{
    public class LineChartLineCurve : LineChartLine
    {
        protected override void GenerateMesh()
        {
            Vector2 size = rectTransform.rect.size;
            Vector2 m_offset = -size * 0.5f;

            Vector2[] points = new Vector2[4];
            Vector2[] uvs = new Vector2[] {
                new Vector2(0.0f, 0.0f),
                new Vector2(1.0f, 0.0f)};

            if (start < 0) start = 0;
            if (end < 0) end = data.Length - 1;
            if (startIndex < 0) startIndex = 0;
            if (endIndex < 0) endIndex = data.Length - 1;
            if (dataX == null)
            {
                if (inverted)
                {
                    float unit = size.y / (end - start + 1);
                    if (reverse)
                    {
                        unit *= -1;
                        m_offset.y *= -1;
                    }
                    m_offset.y += unit * (0.5f - start);

                    Vector2 p1 = new Vector2(m_offset.x + size.x * (data[startIndex].y + data[startIndex].x), m_offset.y + unit * startIndex);
                    Vector2 t1 = new Vector2();
                    for (int i = startIndex + 1; i <= endIndex; ++i)
                    {
                        if (!show[i] || i < start || i > end) continue;
                        Vector2 p0 = p1;
                        Vector2 t0 = -t1;
                        p1 = new Vector2(m_offset.x + size.x * (data[i].y + data[i].x), m_offset.y + unit * i);
                        if (i + 1 < data.Length && show[i + 1])
                        {
                            Vector2 p2 = new Vector2(m_offset.x + size.x * (data[i + 1].y + data[i + 1].x), m_offset.y + unit * (i + 1));
                            t1 = p0 - p2;
                            t1 *= CURVATURE;
                        }
                        else
                        {
                            t1 = new Vector2();
                        }
                        if (!show[i - 1]) continue;

                        CreateBerzierCurve(p0, p1, t0, t1, points, uvs);
                    }
                }
                else
                {
                    float unit = size.x / (end - start + 1);
                    if (reverse)
                    {
                        unit *= -1;
                        m_offset.x *= -1;
                    }
                    m_offset.x += unit * (0.5f - start);

                    Vector2 p1 = new Vector2(m_offset.x + unit * startIndex, m_offset.y + size.y * (data[startIndex].y + data[startIndex].x));
                    Vector2 t1 = new Vector2();
                    for (int i = startIndex + 1; i <= endIndex; ++i)
                    {
                        if (!show[i] || i < start || i > end) continue;
                        Vector2 p0 = p1;
                        Vector2 t0 = -t1;
                        p1 = new Vector2(m_offset.x + unit * i, m_offset.y + size.y * (data[i].y + data[i].x));
                        if (i + 1 < data.Length && show[i + 1])
                        {
                            Vector2 p2 = new Vector2(m_offset.x + unit * (i + 1), m_offset.y + size.y * (data[i + 1].y + data[i + 1].x));
                            t1 = p0 - p2;
                            t1 *= CURVATURE;
                        }
                        else
                        {
                            t1 = new Vector2();
                        }
                        if (!show[i - 1]) continue;

                        CreateBerzierCurve(p0, p1, t0, t1, points, uvs);
                    }
                }
            }
            else
            {
                if (inverted)
                {
                    float unit = 1.0f;
                    if (reverse)
                    {
                        unit *= -1;
                        m_offset.y *= -1;
                    }

                    Vector2 p1 = new Vector2(m_offset.x + size.x * (data[startIndex].y + data[startIndex].x), m_offset.y + size.y * dataX[startIndex] * unit);
                    Vector2 t1 = new Vector2();
                    for (int i = startIndex + 1; i <= endIndex; ++i)
                    {
                        if (!show[i]) continue;
                        Vector2 p0 = p1;
                        Vector2 t0 = -t1;
                        p1 = new Vector2(m_offset.x + size.x * (data[i].y + data[i].x), m_offset.y + size.y * dataX[i] * unit);
                        if (i + 1 < data.Length && show[i + 1])
                        {
                            Vector2 p2 = new Vector2(m_offset.x + size.x * (data[i + 1].y + data[i + 1].x), m_offset.y + size.y * dataX[i + 1] * unit);
                            t1 = p0 - p2;
                            t1 *= CURVATURE;
                        }
                        else
                        {
                            t1 = new Vector2();
                        }
                        if (!show[i - 1]) continue;

                        CreateBerzierCurve(p0, p1, t0, t1, points, uvs);
                    }
                }
                else
                {
                    float unit = 1.0f;
                    if (reverse)
                    {
                        unit *= -1;
                        m_offset.x *= -1;
                    }

                    Vector2 p1 = new Vector2(m_offset.x + size.x * dataX[startIndex] * unit, m_offset.y + size.y * (data[startIndex].y + data[startIndex].x));
                    Vector2 t1 = new Vector2();
                    for (int i = startIndex + 1; i <= endIndex; ++i)
                    {
                        if (!show[i]) continue;
                        Vector2 p0 = p1;
                        Vector2 t0 = -t1;
                        p1 = new Vector2(m_offset.x + size.x * dataX[i] * unit, m_offset.y + size.y * (data[i].y + data[i].x));
                        if (i + 1 < data.Length && show[i + 1])
                        {
                            Vector2 p2 = new Vector2(m_offset.x + size.x * dataX[i + 1] * unit, m_offset.y + size.y * (data[i + 1].y + data[i + 1].x));
                            t1 = p0 - p2;
                            t1 *= CURVATURE;
                        }
                        else
                        {
                            t1 = new Vector2();
                        }
                        if (!show[i - 1]) continue;

                        CreateBerzierCurve(p0, p1, t0, t1, points, uvs);
                    }
                }
            }
        }

        void CreateBerzierCurve(Vector2 p0, Vector2 p1, Vector2 t0, Vector2 t1, Vector2[] points, Vector2[] uvs)
        {
            int stepCount = Mathf.CeilToInt((p1 - p0).magnitude / STEP_SIZE);
            float stepSize = 1.0f / stepCount;
            Vector2 p = p0;
            for (int j = 1; j <= stepCount; ++j)
            {
                float t = stepSize * j;
                Vector2 pLast = p;
                p = ChartHelper.BerzierCurve(t, p, p0 + t0, p1 + t1, p1);

                Vector2 dir = p - pLast;
                points = CreateRect(dir, width * 0.5f);

                AddPolygon(pLast, points, uvs, color, j, stepCount);
            }
        }

        void AddPolygon(Vector2 pLast, Vector2[] points, Vector2[] uvs, Color color, int curveIndex, int stepCount)
        {
            int index = vertices.Count;
            UIVertex[] v = new UIVertex[4];
            v[0].position = points[0] + pLast;
            v[1].position = points[1] + pLast;
            v[2].position = points[2] + pLast;
            v[3].position = points[3] + pLast;
            v[0].color = color;
            v[1].color = color;
            v[2].color = color;
            v[3].color = color;
            v[0].uv0 = uvs[0];
            v[1].uv0 = uvs[0];
            v[2].uv0 = uvs[1];
            v[3].uv0 = uvs[1];
            vertices.AddRange(v);

            int[] tri = new int[] { index, index + 1, index + 2, index + 2, index + 3, index };
            indices.AddRange(tri);
            if (curveIndex < stepCount)
            {
                tri = new int[] { index + 1, index + 4, index + 7, index + 7, index + 2, index + 1 };
                indices.AddRange(tri);
            }
        }
    }
}