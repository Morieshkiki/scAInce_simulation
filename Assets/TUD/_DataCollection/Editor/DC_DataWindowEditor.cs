using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;

public class DC_DataWindowEditor : EditorWindow
{
    [MenuItem("TUD/Controls")]
    public static void ShowControls()
    {
        DC_DataWindowEditor wnd = GetWindow<DC_DataWindowEditor>();
        wnd.titleContent = new GUIContent("TUD Controls");
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;
        ListView collectorList = new ListView();
        root.Add(collectorList);

        // VisualElements objects can contain other VisualElement following a tree hierarchy.
        VisualElement label = new Label("Hello World! From C#");
        collectorList.makeItem = CreateNewCollectorListItem;
        collectorList.bindItem = BindCollectorListItem;
        collectorList.itemsSource = DC_Manager.allCollectors;
    }

    VisualElement CreateNewCollectorListItem()
    {
        return new Label();
    }

    void BindCollectorListItem(VisualElement item, int index)
    {
        (item as Label).text = DC_Manager.allCollectors[index].name; 
    }
}