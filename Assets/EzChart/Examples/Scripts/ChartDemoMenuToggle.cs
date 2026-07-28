using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChartUtil.Demo
{
    public class ChartDemoMenuToggle : MonoBehaviour
    {
        static Toggle lastToggle = null;
        Toggle mToggle;

        private void Awake()
        {
            mToggle = gameObject.GetComponent<Toggle>();
            if (mToggle.isOn) lastToggle = mToggle;
        }

        public void CheckLastToggle(bool isOn)
        {
            if (isOn)
            {
                if (lastToggle != null && lastToggle != mToggle) lastToggle.isOn = false;
                lastToggle = mToggle;
            }
            else
            {
                lastToggle = null;
            }
        }
    }
}