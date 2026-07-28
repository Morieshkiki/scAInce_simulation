using UnityEngine;
using UnityEngine.UI;
using System;

[RequireComponent(typeof(Button))]
public class UIButton : MonoBehaviour
{
    public Action<UIButton> onClick;
    Button _targetButton;
    public Button targetButton { get { if (_targetButton == null) _targetButton = GetComponent<Button>(); return _targetButton; } }
    private void Awake()
    {
        targetButton.onClick.AddListener(OnClick);
    }
    private void OnDestroy()
    {
        targetButton.onClick.RemoveListener(OnClick);
    }

    void OnClick()
    {
        onClick?.Invoke(this);
    }
}
