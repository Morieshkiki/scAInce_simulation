using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class DUI_Graph : MonoBehaviour
{
    [SerializeField] string _dataID = "SomeObject.SomeData";
    public string dataID => _dataID;
    [SerializeField] string _title = "Untitled";
    public string title => _title;
    [SerializeField] string _shortTitle = "NA";
    public string shortTitle => _shortTitle;
    [SerializeField] string _units = ""; //example: kg
    public string units => _units;
    [SerializeField] Color _color = Color.black;
    public Color color => _color;

    [SerializeField] float yMin = 0, yMax = 100;

    [SerializeField] int _factor = 1;
    public int factor => _factor;

    protected virtual void OnEnable()
    {
        DUI_GraphManager.AddGraph(this);
    }

    protected virtual void OnDisable()
    {
        DUI_GraphManager.RemoveGraph(this);
    }
}
