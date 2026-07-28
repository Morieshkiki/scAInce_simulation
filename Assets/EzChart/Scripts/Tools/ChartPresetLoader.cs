using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartUtil
{
    [RequireComponent(typeof(Chart))]
    public class ChartPresetLoader : MonoBehaviour
    {
        [System.Serializable]
        public struct Preset
        {
            [Tooltip("Chart options preset to load")]
            public ChartOptionsPreset preset;
            [Tooltip("Profile controls what elements to be loaded from preset")]
            public ChartOptionsProfile profile;
        }

        public ChartOptions chartOptions;
        public Preset[] presets;

        private void Reset()
        {
            if (chartOptions == null)
            {
                chartOptions = GetComponent<ChartOptions>();
                if (chartOptions == null && GetComponent<Chart>() != null) chartOptions = GetComponent<Chart>().chartOptions;
            }
        }

        public void LoadPresets()
        {
            if (chartOptions == null) return;

            for (int i = 0; i < presets.Length; ++i)
            {
                ChartOptionsProfile profile = presets[i].profile;
                ChartOptionsPreset preset = presets[i].preset;
                if (preset == null || profile == null) continue;

                profile.LoadPreset(preset, ref chartOptions);
            }
        }
    }
}