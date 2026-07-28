using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIToggleButton : UIButton
{
    [SerializeField] GameObject[] elementsOn, elementsOff;
    [SerializeField] bool _state = false;
    public bool state { get => _state; set { SetState(value); } }

    void SetState(bool value)
    {
        if (_state == value)
            return;

        _state = value;

        foreach (GameObject go in elementsOff)
            if (go != null && go.transform.IsChildOf(transform))
                go.SetActive(!value);
        foreach (GameObject go in elementsOn)
            if (go != null && go.transform.IsChildOf(transform))
                go.SetActive(value);
    }
}
