using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartUtil
{
    public abstract class ChartGrid
    {
        [HideInInspector] public ChartGeneralInfo chartInfo = null;
        [HideInInspector] public bool midGrid = false;
        [HideInInspector] public float unitWidth = 0.0f;

        public abstract float GetMouseValueY();
        public abstract float GetMouseValueX();
        public abstract void HighlightItem(int index);
        public abstract void UnhighlightItem(int index);
        public abstract void UpdateHighlight();
        public abstract void UpdateBackground();
        public abstract void UpdateGrid();
    }
}