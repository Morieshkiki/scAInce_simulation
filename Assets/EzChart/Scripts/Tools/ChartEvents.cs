using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ChartUtil
{
    [System.Serializable]
    public class SeriesToggleEvent : UnityEvent<int, bool> { }
    [System.Serializable]
    public class ItemClickEvent : UnityEvent<int, int> { }
    [System.Serializable]
    public class ValueClickEvent : UnityEvent<float, float> { }

    [RequireComponent(typeof(Chart))]
    public class ChartEvents : MonoBehaviour
    {
        //[Tooltip("Triggered when series is toggled on/off. Arguments: series index, on/off")]
        [Header("Series Toggle Event (series index, on/off)")]
        public SeriesToggleEvent seriesToggleEvent = new SeriesToggleEvent();
        //[Tooltip("Triggered when chart items are clicked. Arguments: category/data index, series index (-1 means invalid value)")]
        [Header("Series Click Event (category/data index, series index)")]
        public ItemClickEvent itemClickEvent = new ItemClickEvent();
        //[Tooltip("Triggered when chart is clicked. Arguments: category index/x-value, y-value")]
        [Header("Value Click Event (category index/x-value, y-value)")]
        public ValueClickEvent valueClickEvent = new ValueClickEvent();
    }
}