using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ChartUtil.ChartEditor
{
    [CustomEditor(typeof(Chart))]
    [CanEditMultipleObjects]
    public class ChartEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (targets.Length == 1)
            {
                Chart chart = (Chart)targets[0];
                if (chart.gameObject.scene.name == null) return;

                if (chart.chartOptions == null)
                {
                    if (GUILayout.Button("Add Chart Options"))
                    {
                        chart.chartOptions = chart.GetComponent<ChartOptions>();
                        if (chart.chartOptions == null) chart.chartOptions = Undo.AddComponent<ChartOptions>(chart.gameObject);
                    }
                }

                if (chart.chartData == null)
                {
                    if (GUILayout.Button("Add Chart Data"))
                    {
                        chart.chartData = chart.GetComponent<ChartData>();
                        if (chart.chartData == null) chart.chartData = Undo.AddComponent<ChartData>(chart.gameObject);
                    }
                }

                if (chart.chartOptions == null || chart.chartData == null) return;
            }

            EditorGUILayout.HelpBox("Please clear chart preview if you need to save chart as a prefab/part of a prefab", MessageType.Info);

            if (GUILayout.Button("Preview"))
            {
                foreach (Chart chart in targets)
                {
                    if (chart.gameObject.scene.name == null) continue;
                    chart.UpdateChart();
                }
            }

            if (GUILayout.Button("Clear"))
            {
                foreach (Chart chart in targets)
                {
                    if (chart.gameObject.scene.name == null) continue;
                    chart.Clear();
                }
            }
        }

        //Add chart to right click menu item
//        [MenuItem("GameObject/UI/EzChart - Chart", false)]
//        static void CreateChart(MenuCommand menuCommand)
//        {
//            GameObject context = menuCommand.context as GameObject;
//            Canvas canv = FindObjectOfType<Canvas>();
//            if (canv == null)
//            {
//                canv = new GameObject("Canvas").AddComponent<Canvas>();
//                canv.renderMode = RenderMode.ScreenSpaceOverlay;
//                canv.gameObject.AddComponent<CanvasScaler>();
//                canv.gameObject.AddComponent<GraphicRaycaster>();
//                canv.gameObject.layer = LayerMask.NameToLayer("UI");
//                context = canv.gameObject;
//            }
//            var es = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
//            if (es == null)
//            {
//                es = new GameObject("EventSystem").AddComponent<UnityEngine.EventSystems.EventSystem>();
//                es.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
//            }
//            if (context == null || context.transform.GetComponentInParent<Canvas>() == null) context = canv.gameObject;

//            Chart chart = Helper.CreateEmptyRect("Chart", context.transform, true).gameObject.AddComponent<Chart>();
//            chart.chartOptions = chart.gameObject.AddComponent<ChartOptions>();
//            chart.chartData = chart.gameObject.AddComponent<ChartData>();
//            Undo.RegisterCreatedObjectUndo(chart.gameObject, "Create chart");
//            Selection.activeObject = chart.gameObject;

//#if CHART_TMPRO
//                    chart.chartOptions.plotOptions.generalFont = Resources.Load("Fonts & Materials/LiberationSans SDF", typeof(TMP_FontAsset)) as TMP_FontAsset;
//#else
//            chart.chartOptions.plotOptions.generalFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
//#endif
//            chart.chartOptions.legend.iconImage = Resources.Load<Sprite>("Chart_Circle_128x128");
//        }
    }
}