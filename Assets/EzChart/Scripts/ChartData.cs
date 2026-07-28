using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartUtil
{
    [System.Serializable]
    public class Data
    {
        public bool show = true;
        public float value = 0.0f;
        public float x;
        //public float y;

        public Data()
        {

        }

        public Data(float value)
        {
            this.value = value;
        }

        public Data(float value, float x)
        {
            this.value = value;
            this.x = x;
        }

        public Data(float value, bool show)
        {
            this.value = value;
            this.show = show;
        }

        public Data(float value, float x, bool show)
        {
            this.value = value;
            this.show = show;
            this.x = x;
        }
    }

    [System.Serializable]
    public class Series
    {
        public string name = "";
        public bool show = true;
        public int colorIndex = -1;
        public List<Data> data = new List<Data>();
    }

    public class ChartData : MonoBehaviour
    {
        public List<Series> series = new List<Series>();
        public List<string> categories = new List<string>();
    }
}