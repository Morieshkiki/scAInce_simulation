using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChartUtil
{
    public class LineChartPoint : ChartGraphic
    {
        public Vector2[] data = null; //ratio, starting
        public float[] dataX = null;    //ratio
        public bool[] show = null;
        public float diameter = 2.0f;
        public bool inverted = false;
        public bool reverse = false;
        public float start = -1, end = -1;
        public int startIndex = -1, endIndex = -1;

        public override void RefreshBuffer()
        {
            if (data == null || data.Length == 0 ||
                show == null || show.Length != data.Length ||
                (dataX != null && dataX.Length != data.Length))
            { isDirty = true; inited = false; return; }

            isDirty = false;
            inited = true;
        }

        protected override void GenerateMesh()
        {
            Vector2 size = rectTransform.rect.size;
            Vector2 m_offset = -size * 0.5f;
            float radius = diameter * 0.5f;

            Vector2[] points = new Vector2[4];
            Vector2[] uvs = new Vector2[] {
                new Vector2(0.0f, 0.0f),
                new Vector2(0.0f, 1.0f),
                new Vector2(1.0f, 1.0f),
                new Vector2(1.0f, 0.0f)
            };

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

                    for (int i = startIndex; i <= endIndex; ++i)
                    {
                        if (!show[i] || i < start || i > end) continue;
                        float y = m_offset.y + unit * i;
                        float x = m_offset.x + size.x * (data[i].y + data[i].x);

                        points[0] = new Vector2(x - radius, y - radius);
                        points[1] = new Vector2(x - radius, y + radius);
                        points[2] = new Vector2(x + radius, y + radius);
                        points[3] = new Vector2(x + radius, y - radius);

                        AddPolygon(points, uvs, color);
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

                    for (int i = startIndex; i <= endIndex; ++i)
                    {
                        if (!show[i] || i < start || i > end) continue;
                        float x = m_offset.x + unit * i;
                        float y = m_offset.y + size.y * (data[i].y + data[i].x);

                        points[0] = new Vector2(x - radius, y - radius);
                        points[1] = new Vector2(x - radius, y + radius);
                        points[2] = new Vector2(x + radius, y + radius);
                        points[3] = new Vector2(x + radius, y - radius);

                        AddPolygon(points, uvs, color);
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

                    for (int i = startIndex; i <= endIndex; ++i)
                    {
                        if (!show[i]) continue;
                        float y = m_offset.y + size.y * dataX[i] * unit;
                        float x = m_offset.x + size.x * (data[i].y + data[i].x);

                        points[0] = new Vector2(x - radius, y - radius);
                        points[1] = new Vector2(x - radius, y + radius);
                        points[2] = new Vector2(x + radius, y + radius);
                        points[3] = new Vector2(x + radius, y - radius);

                        AddPolygon(points, uvs, color);
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

                    for (int i = startIndex; i <= endIndex; ++i)
                    {
                        if (!show[i]) continue;
                        float x = m_offset.x + size.x * dataX[i] * unit;
                        float y = m_offset.y + size.y * (data[i].y + data[i].x);

                        points[0] = new Vector2(x - radius, y - radius);
                        points[1] = new Vector2(x - radius, y + radius);
                        points[2] = new Vector2(x + radius, y + radius);
                        points[3] = new Vector2(x + radius, y - radius);

                        AddPolygon(points, uvs, color);
                    }
                }
            }
        }

        void AddPolygon(Vector2[] points, Vector2[] uvs, Color color)
        {
            int index = vertices.Count;
            UIVertex[] v = new UIVertex[4];
            v[0].position = points[0];
            v[1].position = points[1];
            v[2].position = points[2];
            v[3].position = points[3];
            v[0].color = color;
            v[1].color = color;
            v[2].color = color;
            v[3].color = color;
            v[0].uv0 = uvs[0];
            v[1].uv0 = uvs[1];
            v[2].uv0 = uvs[2];
            v[3].uv0 = uvs[3];
            vertices.AddRange(v);

            int[] tri = new int[] { index, index + 1, index + 2, index + 2, index + 3, index };
            indices.AddRange(tri);
        }
    }
}