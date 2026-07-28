using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChartUtil
{
    public class PieChartCircle : ChartGraphic
    {
        public float angle;
        public float center;
        public float innerSize = 0.0f;
        public float separation = 0.0f;

        [HideInInspector] public Vector2 direction;
        Vector2 centerCosSin;
        float radius, radiusInner;
        Vector2[] cossinPie;
        Vector2[] cossinPieInner;

        public override void RefreshBuffer()
        {
            if (angle < 0.0f) angle = 0.0f;
            centerCosSin = new Vector2(Mathf.Cos(center * Mathf.Deg2Rad), Mathf.Sin(center * Mathf.Deg2Rad));
            direction = RotateCW(Vector2.up, centerCosSin);

            innerSize = Mathf.Clamp01(innerSize);
            radius = rectTransform.rect.size.x < rectTransform.rect.size.y ? rectTransform.rect.size.x * 0.5f : rectTransform.rect.size.y * 0.5f;
            radiusInner = radius * innerSize;
            separation = Mathf.Clamp(separation, 0.0f, radius);

            //outer
            float angleSep = Mathf.Asin(separation / radius) * Mathf.Rad2Deg * 2;
            if (angle < angleSep) { isDirty = true; inited = false; return; }
            float anglePie = angle - angleSep;
            int side = Mathf.RoundToInt(anglePie / 360.0f * CosSin.Length);
            cossinPie = GetCosSin(side, 90.0f - anglePie * 0.5f, anglePie, true);

            //inner
            float radiusSep = angle > 180.0f ? separation : separation / Mathf.Sin(angle * 0.5f * Mathf.Deg2Rad);
            if (radiusInner > radiusSep)
            {
                float angleSepInner = Mathf.Asin(separation / radiusInner) * Mathf.Rad2Deg * 2;
                float anglePieInner = angle - angleSepInner;
                cossinPieInner = GetCosSin(side, 90.0f - anglePieInner * 0.5f, anglePieInner, true);
            }
            else
            {
                radiusInner = radiusSep;
                if (angle > 180.0f)
                {
                    float angleInner = angle - 180.0f;
                    cossinPieInner = GetCosSin(side, 90.0f - angleInner * 0.5f, angleInner, true);
                }
                else
                {
                    cossinPieInner = new Vector2[side + 1];
                    for (int j = 0; j < cossinPieInner.Length; ++j) cossinPieInner[j] = new Vector2(0.0f, 1.0f);
                }
            }

            isDirty = false;
            inited = true;
        }

        protected override void GenerateMesh()
        {
            Vector2[] points = new Vector2[4];
            Vector2[] uvs = new Vector2[] {
                new Vector2(0.0f, 0.0f),
                new Vector2(0.0f, 1.0f)
            };

            for (int j = 0; j < cossinPie.Length - 1; ++j)
            {
                points[0] = cossinPieInner[j] * radiusInner;
                points[1] = cossinPie[j] * radius;
                points[2] = cossinPie[j + 1] * radius;
                points[3] = cossinPieInner[j + 1] * radiusInner;

                points[0] = RotateCW(points[0], centerCosSin);
                points[1] = RotateCW(points[1], centerCosSin);
                points[2] = RotateCW(points[2], centerCosSin);
                points[3] = RotateCW(points[3], centerCosSin);

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
            v[2].uv0 = uvs[1];
            v[3].uv0 = uvs[0];
            vertices.AddRange(v);

            int[] tri = new int[] { index, index + 1, index + 2, index + 2, index + 3, index };
            indices.AddRange(tri);
        }
    }
}