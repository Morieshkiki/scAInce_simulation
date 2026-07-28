using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TUD_UI_Header : MonoBehaviour
{
    [SerializeField] UIButton quitBtn;

    private void OnEnable()
    {
        quitBtn.onClick += OnQuitClicked;
    }

    private void OnDisable()
    {
        quitBtn.onClick -= OnQuitClicked;
    }

    void OnQuitClicked(UIButton btn)
    {
        Application.Quit();
    }
}
