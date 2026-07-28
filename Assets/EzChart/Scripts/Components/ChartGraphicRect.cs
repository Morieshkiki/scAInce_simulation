using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChartUtil
{
    public class ChartGraphicRect : ChartGraphic
    {
        public float num = 4;
        public int interval = 1;
        public float width = 1.0f;
        public bool inverted = false;
        public bool mid = false;

        protected override void GenerateMesh()
        {
            if (num < 1) num = 1;
            if (interval < 1) interval = 1;
            Vector2 size = rectTransform.rect.size;
            Vector2 offset = -size * 0.5f;

            if (inverted)
            {
                offset.y -= width * 0.5f;
                float spacing = size.y / num;
                if (mid) offset.y += spacing * 0.5f;

                Vector2[] points = new Vector2[4];
                float n = mid ? num : num + 1;
                for (int i = 0; i < n; i += interval)
                {
                    float pos = offset.y + spacing * i;

                    points[0] = new Vector2(offset.x, pos);
                    points[1] = new Vector2(offset.x, pos + width);
                    points[2] = new Vector2(-offset.x, pos + width);
                    points[3] = new Vector2(-offset.x, pos);

                    AddPolygon(points, color);
                }
            }
            else
            {
                offset.x -= width * 0.5f;
                float spacing = size.x / num;
                if (mid) offset.x += spacing * 0.5f;

                Vector2[] points = new Vector2[4];
                float n = mid ? num : num + 1;
                for (int i = 0; i < n; i += interval)
                {
                    float pos = offset.x + spacing * i;

                    points[0] = new Vector2(pos, offset.y);
                    points[1] = new Vector2(pos, -offset.y);
                    points[2] = new Vector2(pos + width, -offset.y);
                    points[3] = new Vector2(pos + width, offset.y);

                    AddPolygon(points, color);
                }
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