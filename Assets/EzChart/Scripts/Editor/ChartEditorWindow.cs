using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ChartUtil.ChartEditor
{
    public class EzChartWindow : EditorWindow
    {
        public ChartPresetLoader.Preset[] presets;
        static Vector2 scrollPos;
        static bool showFilter = true;
        static bool c_all = true;
        static bool c_pie = true;
        static bool c_bar = true;
        static bool c_line = true;
        static bool c_rose = true;
        static bool c_radar = true;
        static bool c_gauge = true;
        static bool c_solidGauge = true;

        [MenuItem("Window/EzChart")]
        static void Init()
        {
            EzChartWindow window = (EzChartWindow)GetWindow(typeof(EzChartWindow), false, "EzChart");
            window.Show();
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false);
            
            EditorGUILayout.LabelField("Chart Preview");

            if (GUILayout.Button("Update Preview For Active Charts"))
            {
                Chart[] charts = FindObjectsOfType<Chart>();
                foreach (Chart chart in charts)
                {
                    if (chart.gameObject.scene.name == null) continue;
                    try { chart.UpdateChart(); }
                    catch { continue; }
                }
            }

            if (GUILayout.Button("Update Preview For All Charts"))
            {
                Chart[] charts = Resources.FindObjectsOfTypeAll<Chart>();
                foreach (Chart chart in charts)
                {
                    if (chart.gameObject.scene.name == null) continue;
                    try { chart.UpdateChart(); }
                    catch { continue; }
                }
            }

            if (GUILayout.Button("Clear Preview For All Charts"))
            {
                Chart[] charts = Resources.FindObjectsOfTypeAll<Chart>();
                foreach (Chart chart in charts)
                {
                    if (chart.gameObject.scene.name == null) continue;
                    try { chart.Clear(); }
                    catch { continue; }
                }
            }
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Chart Preset");

            ScriptableObject target = this;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty presetsProperty = so.FindProperty("presets");
            EditorGUILayout.PropertyField(presetsProperty, true);
            so.ApplyModifiedProperties();

            showFilter = EditorGUILayout.Foldout(showFilter, "Chart Type Filter");
            if (showFilter)
            {
                EditorGUILayout.BeginHorizontal();
                bool l_all = c_all;
                c_all = GUILayout.Toggle(c_all, "All Types");
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                c_pie = GUILayout.Toggle(c_pie || c_all, "Pie Chart");
                c_bar = GUILayout.Toggle(c_bar || c_all, "Bar Chart");
                c_line = GUILayout.Toggle(c_line || c_all, "Line Chart");
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                c_rose = GUILayout.Toggle(c_rose || c_all, "Rose Chart");
                c_radar = GUILayout.Toggle(c_radar || c_all, "Radar Chart");
                c_gauge = GUILayout.Toggle(c_gauge || c_all, "Gauge");
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                c_solidGauge = GUILayout.Toggle(c_solidGauge || c_all, "Solid Gauge");
                GUILayout.Label("      ");
                GUILayout.Label("      ");
                EditorGUILayout.EndHorizontal();
                bool all = c_pie && c_bar && c_line && c_rose && c_radar && c_gauge && c_solidGauge;
                if (l_all ^ c_all)
                {
                    if (all)
                    {
                        if (!c_all) c_pie = c_bar = c_line = c_rose = c_radar = c_gauge = c_solidGauge = c_all;
                    }
                    else
                    {
                        c_all = false;
                    }
                }
                else { c_all = all; }
            }

            if (GUILayout.Button("Load Preset For Active Charts") && presets.Length > 0)
            {
                Chart[] charts = FindObjectsOfType<Chart>();
                ChartOptions[] chartOptions = new ChartOptions[charts.Length];
                for (int i = 0; i < charts.Length; ++i) chartOptions[i] = charts[i].chartOptions;
                Undo.RecordObjects(chartOptions, "Load chart presets");

                foreach (Chart chart in charts)
                {
                    if (chart.gameObject.scene.name == null) continue;
                    if (chart.chartOptions == null || !CheckChartType(chart)) continue;
                    try { LoadChartPreset(chart, presets); }
                    catch { continue; }
                }
            }

            if (GUILayout.Button("Load Preset For All Charts") && presets.Length > 0)
            {
                Chart[] charts = Resources.FindObjectsOfTypeAll<Chart>();
                ChartOptions[] chartOptions = new ChartOptions[charts.Length];
                for (int i = 0; i < charts.Length; ++i) chartOptions[i] = charts[i].chartOptions;
                Undo.RecordObjects(chartOptions, "Load chart presets");

                foreach (Chart chart in charts)
                {
                    if (chart.gameObject.scene.name == null) continue;
                    if (chart.chartOptions == null || !CheckChartType(chart)) continue;
                    try { LoadChartPreset(chart, presets); }
                    catch { continue; }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        static void LoadChartPreset(Chart chart, ChartPresetLoader.Preset[] presets)
        {
            for (int i = 0; i < presets.Length; ++i)
            {
                ChartOptionsProfile profile = presets[i].profile;
                ChartOptionsPreset preset = presets[i].preset;
                if (preset == null || profile == null) continue;
                profile.LoadPreset(preset, ref chart.chartOptions);
                PrefabUtility.RecordPrefabInstancePropertyModifications(chart.chartOptions);
            }
        }

        static bool CheckChartType(Chart chart)
        {
            return
                (chart.chartType == ChartType.PieChart && c_pie) ||
                (chart.chartType == ChartType.BarChart && c_bar) ||
                (chart.chartType == ChartType.LineChart && c_line) ||
                (chart.chartType == ChartType.RoseChart && c_rose) ||
                (chart.chartType == ChartType.RadarChart && c_radar) ||
                (chart.chartType == ChartType.Gauge && c_gauge) ||
                (chart.chartType == ChartType.SolidGauge && c_solidGauge);
        }
    }
}