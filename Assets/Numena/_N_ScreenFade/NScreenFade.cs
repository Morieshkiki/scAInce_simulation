using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Numena.Utilities
{
    public static class NScreenFade
    {
        static Color _color;
        public static Color color { get { return _color; } set { SetColor(value); } }
        public static int layer = 1;

        static FadeHelper fadeHelper;

        static Mesh quad;
        static Material mat;

        static void SetColor(Color value)
        {
            _color = value;

            if (quad == null)
            {
                quad = new Mesh();
                quad.SetVertices(new Vector3[] { new Vector3(-1, -1, 0.5f), new Vector3(-1, 1, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(1, -1, 0.5f) });
                quad.SetTriangles(new int[] { 0, 1, 2, 2, 3, 0 }, 0);
                quad.bounds = new Bounds(Vector3.zero, new Vector3(float.MaxValue, float.MaxValue, float.MaxValue));//disable culling
                quad.UploadMeshData(false);
            }
            if (mat == null)
            {
                Shader shader = Shader.Find("Hidden/NScreenFadeShader");
                mat = new Material(shader);
            }

            mat.SetColor("_Color", _color);

            if (color.a > 0)
            {
                if (fadeHelper == null)
                {
                    fadeHelper = new GameObject("[FadeHelper]").AddComponent<FadeHelper>();
                    Object.DontDestroyOnLoad(fadeHelper);
                }
            }
            else
            {
                if (fadeHelper != null)
                {
                    Object.Destroy(fadeHelper.gameObject);
                    fadeHelper = null;
                }
            }
        }

        class FadeHelper : MonoBehaviour
        {
            private void LateUpdate()
            {
                Graphics.DrawMesh(quad, Matrix4x4.identity, mat, layer);
            }
        }
    }
}
