using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ChartUtil.ChartEditor
{
    [CustomEditor(typeof(DataGenerator))]
    [CanEditMultipleObjects]
    public class DataGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Generate Random Data"))
            {
                ChartData[] chartData = new ChartData[targets.Length];
                for (int i = 0; i < targets.Length; ++i) chartData[i] = ((DataGenerator)targets[i]).chartData;
                Undo.RecordObjects(chartData, "Generate random data");

                for (int i = 0; i < targets.Length; ++i)
                {
                    ((DataGenerator)targets[i]).GenerateRandomData();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(chartData[i]);
                }
            }
        }
    }
}