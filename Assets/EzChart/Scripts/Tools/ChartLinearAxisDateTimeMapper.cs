using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartUtil
{
    [RequireComponent(typeof(Chart))]
    public class ChartLinearAxisDateTimeMapper : MonoBehaviour
    {
        [Tooltip("X axis value A.")]
        public float valueA = 0.0f;
        [Tooltip("X axis value B.")]
        public float valueB = 100.0f;
        [Tooltip("DateTime ticks corresponding with axis value A.")]
        public long ticksA = 0;
        [Tooltip("DateTime ticks corresponding with axis value B.")]
        public long ticksB = 864000000000;
        [Tooltip("C# standard DateTime format string.")]
        public string dateTimeFormat;

        public long ValueToTicks(float value)
        {
            double r = (value - valueA) / (valueB - valueA);
            long ticks = (long)(ticksA * (1.0 - r) + ticksB * r);
            return ticks;
        }

        public float TicksToValue(long ticks)
        {
            double r = (ticks - ticksA) / (double)(ticksB - ticksA);
            float value = (float)(valueA * (1.0 - r) + valueB * r);
            return value;
        }
    }
}