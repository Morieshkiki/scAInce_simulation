using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChartUtil
{
    public class LineChartShadeCurve : LineChartShade
    {
        protected override void GenerateMesh()
        {
            Vector2 size = rectTransform.rect.size;
            Vector2 m_offset = -size * 0.5f;

            Vector2[] points = new Vector2[4];

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

                    Vector2 ps1 = new Vector2(m_offset.x + size.x * data[startIndex].y, m_offset.y);
                    Vector2 p1 = ps1 + new Vector2(size.x * data[startIndex].x, 0.0f);
                    Vector2 t1 = new Vector2();
                    Vector2 ts1 = new Vector2();
                    for (int i = startIndex + 1; i <= endIndex; ++i)
                    {
                        if (!show[i] || i < start || i > end) continue;
                        Vector2 ps0 = ps1;
                        Vector2 p0 = p1;
                        Vector2 t0 = -t1;
                        Vector2 ts0 = -ts1;
                        ps1 = new Vector2(m_offset.x + size.x * data[i].y, m_offset.y + unit * i);
                        p1 = ps1 + new Vector2(size.x * data[i].x, 0.0f);
                        if (i + 1 < data.Length && show[i + 1])
                        {
                            Vector2 ps2 = new Vector2(m_offset.x + size.x * data[i + 1].y, m_offset.y + unit * (i + 1));
                            Vector2 p2 = ps2 + new Vector2(size.x * data[i + 1].x, 0.0f);
                            t1 = p0 - p2;
                            ts1 = ps0 - ps2;
                            t1 *= CURVATURE;
                            ts1 *= CURVATURE;
                        }
                        else
                        {
                            t1 = new Vector2();
                            ts1 = new Vector2();
                        }
                        if (!show[i - 1]) continue;

                        CreateBerzierCurve(p0, p1, t0, t1, ps0, ps1, ts0, ts1, points);
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

                    Vector2 ps1 = new Vector2(m_offset.x, m_offset.y + size.y * data[startIndex].y);
                    Vector2 p1 = ps1 + new Vector2(0.0f, size.y * data[startIndex].x);
                    Vector2 t1 = new Vector2();
                    Vector2 ts1 = new Vector2();
                    for (int i = startIndex + 1; i <= endIndex; ++i)
                    {
                        if (!show[i] || i < start || i > end) continue;
                        Vector2 ps0 = ps1;
                        Vector2 p0 = p1;
                        Vector2 t0 = -t1;
                        Vector2 ts0 = -ts1;
                        ps1 = new Vector2(m_offset.x + unit * i, m_offset.y + size.y * data[i].y);
                        p1 = ps1 + new Vector2(0.0f, size.y * data[i].x);
                        if (i + 1 < data.Length && show[i + 1])
                        {
                            Vector2 ps2 = new Vector2(m_offset.x + unit * (i + 1), m_offset.y + size.y * data[i + 1].y);
                            Vector2 p2 = ps2 + new Vector2(0.0f, size.y * data[i + 1].x);
                            t1 = p0 - p2;
                            ts1 = ps0 - ps2;
                            t1 *= CURVATURE;
                            ts1 *= CURVATURE;
                        }
                        else
                        {
                            t1 = new Vector2();
                            ts1 = new Vector2();
                        }
                        if (!show[i - 1]) continue;

                        CreateBerzierCurve(p0, p1, t0, t1, ps0, ps1, ts0, ts1, points);
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
                    
                    Vector2 ps1 = new Vector2(m_offset.x + size.x * data[startIndex].y, m_offset.y + size.y * dataX[startIndex] * unit);
                    Vector2 p1 = ps1 + new Vector2(size.x * data[startIndex].x, 0.0f);
                    Vector2 t1 = new Vector2();
                    Vector2 ts1 = new Vector2();
                    for (int i = startIndex + 1; i <= endIndex; ++i)
                    {
                        if (!show[i]) continue;
                        Vector2 ps0 = ps1;
                        Vector2 p0 = p1;
                        Vector2 t0 = -t1;
                        Vector2 ts0 = -ts1;
                        ps1 = new Vector2(m_offset.x + size.x * data[i].y, m_offset.y + size.y * dataX[i] * unit);
                        p1 = ps1 + new Vector2(size.x * data[i].x, 0.0f);
                        if (i + 1 < data.Length && show[i + 1])
                        {
                            Vector2 ps2 = new Vector2(m_offset.x + size.x * data[i + 1].y, m_offset.y + size.y * dataX[i + 1] * unit);
                            Vector2 p2 = ps2 + new Vector2(size.x * data[i + 1].x, 0.0f);
                            t1 = p0 - p2;
                            ts1 = ps0 - ps2;
                            t1 *= CURVATURE;
                            ts1 *= CURVATURE;
                        }
                        else
                        {
                            t1 = new Vector2();
                            ts1 = new Vector2();
                        }
                        if (!show[i - 1]) continue;

                        CreateBerzierCurve(p0, p1, t0, t1, ps0, ps1, ts0, ts1, points);
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

                    Vector2 ps1 = new Vector2(m_offset.x + size.x * dataX[startIndex] * unit, m_offset.y + size.y * data[startIndex].y);
                    Vector2 p1 = ps1 + new Vector2(0.0f, size.y * data[startIndex].x);
                    Vector2 t1 = new Vector2();
                    Vector2 ts1 = new Vector2();
                    for (int i = startIndex + 1; i <= endIndex; ++i)
                    {
                        if (!show[i]) continue;
                        Vector2 ps0 = ps1;
                        Vector2 p0 = p1;
                        Vector2 t0 = -t1;
                        Vector2 ts0 = -ts1;
                        ps1 = new Vector2(m_offset.x + size.x * dataX[i] * unit, m_offset.y + size.y * data[i].y);
                        p1 = ps1 + new Vector2(0.0f, size.y * data[i].x);
                        if (i + 1 < data.Length && show[i + 1])
                        {
                            Vector2 ps2 = new Vector2(m_offset.x + size.x * dataX[i + 1] * unit, m_offset.y + size.y * data[i + 1].y);
                            Vector2 p2 = ps2 + new Vector2(0.0f, size.y * data[i + 1].x);
                            t1 = p0 - p2;
                            ts1 = ps0 - ps2;
                            t1 *= CURVATURE;
                            ts1 *= CURVATURE;
                        }
                        else
                        {
                            t1 = new Vector2();
                            ts1 = new Vector2();
                        }
                        if (!show[i - 1]) continue;

                        CreateBerzierCurve(p0, p1, t0, t1, ps0, ps1, ts0, ts1, points);
                    }
                }
            }
        }
        
        void CreateBerzierCurve(Vector2 p0, Vector2 p1, Vector2 t0, Vector2 t1, Vector2 ps0, Vector2 ps1, Vector2 ts0, Vector2 ts1, Vector2[] points)
        {
            int stepCount = Mathf.CeilToInt((p1 - p0).magnitude / STEP_SIZE);
            float stepSize = 1.0f / stepCount;
            Vector2 p = p0;
            Vector2 pStart = ps0;
            for (int j = 1; j <= stepCount; ++j)
            {
                float t = stepSize * j;
                Vector2 pLast = p;
                Vector2 pStartLast = pStart;
                p = ChartHelper.BerzierCurve(t, p, p0 + t0, p1 + t1, p1);
                pStart = ChartHelper.BerzierCurve(t, pStart, ps0 + ts0, ps1 + ts1, ps1);

                points[0] = pStartLast;
                points[1] = pLast;
                points[2] = p;
                points[3] = pStart;

                AddPolygon(points, color);
            }
        }

        void AddPolygon(Vector2[] points, Color color)
        {
            int index = vertices.Count;
            if (Vector2.Dot(points[1] - points[0], points[2] - points[3]) >= 0.0f)
            {
                UIVertex[] v = new UIVertex[4];
                v[0].position = points[0];
                v[1].position = points[1];
                v[2].position = points[2];
                v[3].position = points[3];
                v[0].color = color;
                v[1].color = color;
                v[2].color = color;
                v[3].color = color;
                v[0].uv0 = Vector2.zero;
                v[1].uv0 = Vector2.zero;
                v[2].uv0 = Vector2.zero;
                v[3].uv0 = Vector2.zero;
                vertices.AddRange(v);

                int[] tri = new int[] { index, index + 1, index + 2, index + 2, index + 3, index };
                indices.AddRange(tri);
            }
            else
            {
                UIVertex[] v = new UIVertex[5];
                v[0].position = points[0];
                v[1].position = points[1];
                v[2].position = points[2];
                v[3].position = points[3];
                v[4].position = ChartHelper.LineIntersection(points); ;
                v[0].color = color;
                v[1].color = color;
                v[2].color = color;
                v[3].color = color;
                v[4].color = color;
                v[0].uv0 = Vector2.zero;
                v[1].uv0 = Vector2.zero;
                v[2].uv0 = Vector2.zero;
                v[3].uv0 = Vector2.zero;
                v[4].uv0 = Vector2.zero;
                vertices.AddRange(v);

                int[] tri = new int[] { index, index + 1, index + 4, index + 4, index + 3, index + 2 };
                indices.AddRange(tri);
            }
        }
    }
}