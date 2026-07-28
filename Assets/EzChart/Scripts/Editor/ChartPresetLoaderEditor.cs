using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ChartUtil.ChartEditor
{
    [CustomEditor(typeof(ChartPresetLoader))]
    [CanEditMultipleObjects]
    public class ChartPresetLoaderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Load Presets"))
            {
                ChartOptions[] chartOptions = new ChartOptions[targets.Length];
                for (int i = 0; i < targets.Length; ++i) chartOptions[i] = ((ChartPresetLoader)targets[i]).chartOptions;
                Undo.RecordObjects(chartOptions, "Load chart presets");

                for (int i = 0; i < targets.Length; ++i)
                {
                    ((ChartPresetLoader)targets[i]).LoadPresets();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(chartOptions[i]);
                }
            }
        }
    }
}