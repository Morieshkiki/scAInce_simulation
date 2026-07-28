using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ChartUtil.ChartEditor
{
    [CustomEditor(typeof(ChartData))]
    public class ChartDataEditor : Editor
    {
        const int MAX_COUNT = 100;
        static bool show = false;

        private void Awake()
        {
            show = false;
        }

        public override void OnInspectorGUI()
        {
            ChartData data = (ChartData)target;
            int counter = 0;
            for (int i = 0; i < data.series.Count; ++i)
            {
                counter += data.series[i].data.Count;
            }
            if (show || counter < MAX_COUNT)
            {
                DrawDefaultInspector();
            }
            else
            {
                EditorGUILayout.HelpBox("Chart Data contains more than 100 values", MessageType.Warning);
                if (GUILayout.Button("Display Chart Data"))
                {
                    show = true;
                }
            }
        }
    }
}