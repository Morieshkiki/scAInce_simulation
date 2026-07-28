using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// todo: make sure graph is in correct state when enabling
/// </summary>
public class TUD_UI_Footer : MonoBehaviour
{
    [SerializeField] UIToggleButton menuBtn;
    [SerializeField] UIToggleButton graphBtnHR, graphBtnPupil, graphBtnEye, mapBtn;
    [SerializeField] DUI_Graph graphHR, graphPupilL, graphPupilR;
    [SerializeField] GameObject gazeOverlay;

    [SerializeField] GameObject menuPanel, map;

    private void OnEnable()
    {
        menuBtn.onClick += OnMenuClicked;

        graphBtnHR.onClick += OnGraphClicked;
        graphBtnEye.onClick += OnGraphClicked;
        graphBtnPupil.onClick += OnGraphClicked;
        mapBtn.onClick += OnMapClicked;
    }

    private void OnDisable()
    {
        menuBtn.onClick -= OnMenuClicked;

        graphBtnHR.onClick -= OnGraphClicked;
        graphBtnEye.onClick -= OnGraphClicked;
        graphBtnPupil.onClick -= OnGraphClicked;
        mapBtn.onClick -= OnMapClicked;
    }

    void OnGraphClicked(UIButton btn)
    {
        if(btn == graphBtnHR)
        {
            ToggleGraph(graphBtnHR, graphHR);
            //StandaloneFileBrowser.SaveFilePanel("Save File", "", "", "");
        }
        else if(btn == graphBtnPupil)
        {
            ToggleGraph(graphBtnPupil, graphPupilL, !graphPupilL.enabled);
            ToggleGraph(graphBtnPupil, graphPupilR, graphPupilL.enabled);
        }
        else if(btn == graphBtnEye)
        {
            gazeOverlay.SetActive(!gazeOverlay.activeSelf);
            graphBtnEye.state = gazeOverlay.activeSelf;
        }
    }

    void OnMenuClicked(UIButton btn)
    {
        ToggleMenu();
    }

    void ToggleMenu()
    {
        ToggleMenu(!menuPanel.activeSelf);
    }
    void ToggleMenu(bool active)
    {
        menuPanel.SetActive(active);
        menuBtn.state = active;
    }

    void OnMapClicked(UIButton btn)
    {
        ToggleMap();
    }

    void ToggleMap()
    {
        ToggleMap(!map.activeSelf);
    }
    void ToggleMap(bool active)
    {
        map.SetActive(active);
        mapBtn.state = active;
    }

    void ToggleGraph(UIToggleButton button, DUI_Graph graph)
    {
        ToggleGraph(button, graph, !graph.enabled);
    }

    void ToggleGraph(UIToggleButton button, DUI_Graph graph, bool on)
    {
        button.state = on;
        graph.enabled = on;
    }
}
