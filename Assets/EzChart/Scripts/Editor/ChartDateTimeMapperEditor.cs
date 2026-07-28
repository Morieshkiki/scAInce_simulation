using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ChartUtil.ChartEditor
{
    [CustomEditor(typeof(ChartLinearAxisDateTimeMapper))]
    [CanEditMultipleObjects]
    public class ChartDateTimeMapperEditor : Editor
    {
        static bool showSetting = true;
        static int year1, month1, day1;
        static int year2, month2, day2;

        private void Awake()
        {
            ChartLinearAxisDateTimeMapper mapper = (ChartLinearAxisDateTimeMapper)target;
            System.DateTime d1 = new System.DateTime(mapper.ticksA);
            year1 = d1.Year;
            month1 = d1.Month;
            day1 = d1.Day;
            System.DateTime d2 = new System.DateTime(mapper.ticksB);
            year2 = d2.Year;
            month2 = d2.Month;
            day2 = d2.Day;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            showSetting = EditorGUILayout.Foldout(showSetting, "Quick Ticks Setting");
            if (showSetting)
            {
                year1 = EditorGUILayout.IntField("Year A", year1);
                month1 = EditorGUILayout.IntField("Month A", month1);
                day1 = EditorGUILayout.IntField("Day A", day1);
                if (GUILayout.Button("Apply Ticks A"))
                {
                    Undo.RecordObjects(targets, "Apply Ticks A");
                    for (int i = 0; i < targets.Length; ++i)
                    {
                        System.DateTime d = new System.DateTime(year1, month1, day1);
                        ChartLinearAxisDateTimeMapper mapper = (ChartLinearAxisDateTimeMapper)targets[i];
                        mapper.ticksA = d.Ticks;
                    }
                }

                year2 = EditorGUILayout.IntField("Year B", year2);
                month2 = EditorGUILayout.IntField("Month B", month2);
                day2 = EditorGUILayout.IntField("Day B", day2);
                if (GUILayout.Button("Apply Ticks B"))
                {
                    Undo.RecordObjects(targets, "Apply Ticks B");
                    for (int i = 0; i < targets.Length; ++i)
                    {
                        System.DateTime d = new System.DateTime(year2, month2, day2);
                        ChartLinearAxisDateTimeMapper mapper = (ChartLinearAxisDateTimeMapper)targets[i];
                        mapper.ticksB = d.Ticks;
                    }
                }
            }
        }
    }
}