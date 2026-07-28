using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChartUtil
{
    public abstract class ChartGraphic : MaskableGraphic
    {
        static Vector2[] m_cossin = null;
        public static Vector2[] CosSin
        {
            get
            {
                if (m_cossin == null) m_cossin = GetCosSin(128);
                return m_cossin;
            }
        }

        protected const float STEP_SIZE = 10.0f;
        protected const float CURVATURE = 0.1f;

        [SerializeField] Texture m_Texture;

        [HideInInspector] public bool isDirty = true;
        [HideInInspector] public bool inited = false;

        protected List<UIVertex> vertices = null;
        protected List<int> indices = null;

        public static Vector2[] GetCosSin(float side, float offset = 0.0f, float total = 360.0f, bool last = true)
        {
            Vector2[] cs = last ? new Vector2[Mathf.CeilToInt(side) + 1] : new Vector2[Mathf.CeilToInt(side)];
            float unit = (total / side) * Mathf.Deg2Rad;
            offset *= Mathf.Deg2Rad;
            for (int i = 0; i < cs.Length; ++i)
            {
                float angle = offset + unit * i;
                cs[i].x = Mathf.Cos(angle);
                cs[i].y = Mathf.Sin(angle);
            }
            return cs;
        }

        public static Vector2 RotateCW(Vector2 p, Vector2 cs)
        {
            Vector2 pp = new Vector2();
            pp.x = p.x * cs.x + p.y * cs.y;
            pp.y = -p.x * cs.y + p.y * cs.x;
            return pp;
        }

        public static Vector2 RotateCCW(Vector2 p, Vector2 cs)
        {
            Vector2 pp = new Vector2();
            pp.x = p.x * cs.x - p.y * cs.y;
            pp.y = p.x * cs.y + p.y * cs.x;
            return pp;
        }

        public static Vector2[] CreateRect(Vector2 dir, float dis)
        {
            Vector2[] points = new Vector2[4];

            Vector2 v = Vector3.Cross(dir, Vector3.forward);
            v.Normalize();

            points[0] = v * dis;
            points[1] = v * dis + dir;
            points[2] = -v * dis + dir;
            points[3] = -v * dis;

            return points;
        }

        public override Texture mainTexture
        {
            get { return m_Texture == null ? s_WhiteTexture : m_Texture; }
        }

        public Texture texture
        {
            get
            {
                return m_Texture;
            }
            set
            {
                if (m_Texture == value) return;
                m_Texture = value;
                SetVerticesDirty();
                SetMaterialDirty();
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (GetComponent<CanvasRenderer>() == null)
                gameObject.AddComponent<CanvasRenderer>();
            raycastTarget = false;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            isDirty = true;
        }
#endif

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (isDirty) RefreshBuffer();
            if (!inited) return;
            vertices = new List<UIVertex>();
            indices = new List<int>();
            GenerateMesh();
            vh.AddUIVertexStream(vertices, indices);
            vertices = null;
            indices = null;
        }
        
        public virtual void RefreshBuffer() { isDirty = false; inited = true; }

        protected abstract void GenerateMesh();
    }
}