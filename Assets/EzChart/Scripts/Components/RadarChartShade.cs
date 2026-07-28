using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChartUtil
{
    public class RadarChartShade : ChartGraphic
    {
        public Vector2[] data = null; //ratio, starting
        public bool[] show = null;
        public bool reverse = false;

        [HideInInspector] public Vector2[] direction = null;

        public override void RefreshBuffer()
        {
            if (data == null || data.Length == 0 ||
                show == null || show.Length != data.Length)
            { isDirty = true; inited = false; return; }

            direction = new Vector2[data.Length];
            float angleOffset = 360.0f / data.Length * 0.5f;
            Vector2[] cossin = GetCosSin(data.Length, angleOffset, 360.0f, false);
            if (reverse)
            {
                for (int i = 0; i < data.Length; ++i)
                {
                    if (!show[i]) continue;
                    direction[i] = RotateCCW(Vector2.up, cossin[i]);
                }
            }
            else
            {
                for (int i = 0; i < data.Length; ++i)
                {
                    if (!show[i]) continue;
                    direction[i] = RotateCW(Vector2.up, cossin[i]);
                }
            }

            isDirty = false;
            inited = true;
        }

        protected override void GenerateMesh()
        {
            float radius = rectTransform.rect.size.x < rectTransform.rect.size.y ? rectTransform.rect.size.x * 0.5f : rectTransform.rect.size.y * 0.5f;

            Vector2[] points = new Vector2[4];
            Vector2[] uvs = new Vector2[2];
            uvs[0] = new Vector2(0.0f, 0.0f);
            uvs[1] = new Vector2(1.0f, 0.0f);
            
            Vector2 pStart = direction[0] * radius * data[0].y;
            Vector2 p = direction[0] * radius * (data[0].y + data[0].x);
            for (int i = 1; i < data.Length; ++i)
            {
                if (!show[i]) continue;
                Vector2 lastStart = pStart;
                Vector2 lastPoint = p;
                pStart = direction[i] * radius * data[i].y;
                p = direction[i] * radius * (data[i].y + data[i].x);
                if (!show[i - 1]) continue;

                points[0] = lastStart;
                points[1] = lastPoint;
                points[2] = p;
                points[3] = pStart;

                AddPolygon(points, color);
            }

            {
                if (!show[0]) return;
                Vector2 lastStart = pStart;
                Vector2 lastPoint = p;
                pStart = direction[0] * radius * data[0].y;
                p = direction[0] * radius * (data[0].y + data[0].x);
                if (!show[data.Length - 1]) return;

                points[0] = lastStart;
                points[1] = lastPoint;
                points[2] = p;
                points[3] = pStart;

                AddPolygon(points, color);
            }
        }

        void AddPolygon(Vector2[] points, Color color)
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
            v[0].uv0 = Vector2.zero;
            v[1].uv0 = Vector2.zero;
            v[2].uv0 = Vector2.zero;
            v[3].uv0 = Vector2.zero;
            vertices.AddRange(v);

            int[] tri = new int[] { index, index + 1, index + 2, index + 2, index + 3, index };
            indices.AddRange(tri);
        }
    }
}