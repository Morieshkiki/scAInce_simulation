using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TUD_UI_SceneControls : MonoBehaviour
{

    [SerializeField] SceneBtnInfo[] sceneButtons;

    [System.Serializable] struct SceneBtnInfo
    {
        public UIButton btn;
        public int sceneID;
    }

    private void OnEnable()
    {
        foreach(SceneBtnInfo info in sceneButtons)
        {
            if (info.btn == null)
                continue;
            info.btn.onClick += OnSceneButtonClicked;
        }
    }

    private void OnDisable()
    {
        foreach (SceneBtnInfo info in sceneButtons)
        {
            if (info.btn == null)
                continue;
            info.btn.onClick -= OnSceneButtonClicked;
        }
    }

    void OnSceneButtonClicked(UIButton btn)
    {
        int index = -1;
        for(int ct = 0; ct < sceneButtons.Length; ct++)
        {
            if(sceneButtons[ct].btn == btn)
            {
                index = ct;
                break;
            }
        }
        SceneManager.LoadScene(sceneButtons[index].sceneID);
    }
}
