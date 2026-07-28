using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChartUtil
{
    public class RadarChartPoint : ChartGraphic
    {
        public Vector2[] data = null; //ratio, starting
        public bool[] show = null;
        public float diameter = 2.0f;
        public bool reverse = false;

        [HideInInspector] public Vector2[] direction = null;

        public override void RefreshBuffer()
        {
            if (data == null || data.Length == 0 ||
                show == null || show.Length != data.Length)
            { isDirty = true; inited = false; return; }

            if (direction == null)
            {
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
            }

            isDirty = false;
            inited = true;
        }
        
        protected override void GenerateMesh()
        {
            float radiusP = diameter * 0.5f;
            float radius = rectTransform.rect.size.x < rectTransform.rect.size.y ? rectTransform.rect.size.x * 0.5f : rectTransform.rect.size.y * 0.5f;

            Vector2[] points = new Vector2[4];
            Vector2[] uvs = new Vector2[] {
                new Vector2(0.0f, 0.0f),
                new Vector2(0.0f, 1.0f),
                new Vector2(1.0f, 1.0f),
                new Vector2(1.0f, 0.0f)
            };

            for (int i = 0; i < data.Length; ++i)
            {
                if (!show[i]) continue;
                Vector2 p = direction[i] * radius * (data[i].y + data[i].x);

                points[0] = new Vector2(p.x - radiusP, p.y - radiusP);
                points[1] = new Vector2(p.x - radiusP, p.y + radiusP);
                points[2] = new Vector2(p.x + radiusP, p.y + radiusP);
                points[3] = new Vector2(p.x + radiusP, p.y - radiusP);

                AddPolygon(points, uvs, color);
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