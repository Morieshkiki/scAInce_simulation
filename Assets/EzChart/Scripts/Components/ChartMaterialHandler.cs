using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChartUtil
{
    [ExecuteInEditMode]
    public class ChartMaterialHandler : MonoBehaviour
    {
        Material m_material = null;

        public void Load(string path)
        {
            m_material = new Material(Resources.Load<Material>(path));
            GetComponent<MaskableGraphic>().material = m_material;
        }

        private void OnDestroy()
        {
            if (m_material != null) ChartHelper.Destroy(m_material);
        }
    }
}