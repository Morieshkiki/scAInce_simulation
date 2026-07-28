using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class PrefabReplacementTool : EditorWindow
{
    private bool inculdeAllInstances;
    private bool retainPrefabChild;
    private bool retainNames;
    private Object prefab;
    private Color textColor;

    [MenuItem("Numena/Tools/Prefab Replacement Tool")]
    public static void OpenWindow()
    {
        GetWindow<PrefabReplacementTool>("Prefab Replacement Tool");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        prefab = EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), true);
        inculdeAllInstances = EditorGUILayout.Toggle(new GUIContent("Include All Instances", "Replace all instances of selected prefab"), inculdeAllInstances);
        retainPrefabChild = EditorGUILayout.Toggle(new GUIContent("Retain Prefab Childs", "Retain added gameobject inside of prefab"), retainPrefabChild);
        retainNames = EditorGUILayout.Toggle("Retain Names", retainNames);
        if (GUILayout.Button("Replace"))
        {
            if (prefab != null)
                Replace(prefab as GameObject, Selection.gameObjects.Where(e => !PrefabUtility.IsAnyPrefabInstanceRoot(e) || e.GetInstanceID() < 0).ToList());
            else
                Debug.LogWarning("Prefab can't be null");
        }

        GUILayout.Space(20);
        GUILayout.Label("Select objects in the scene to replace, it can be regular or prefab object");
        textColor = GUI.contentColor;
        GUI.contentColor = Color.yellow;
        GUILayout.Label("Note:");
        GUILayout.Label("- Only select object from the scene");
        if (inculdeAllInstances)
            GUILayout.Label("- Selecting prefab object will replace all instance of the prefab in the scene.");

        GUILayout.Space(10);
        GUI.contentColor = textColor;
        GUILayout.Label("Selected objects:");

        StringBuilder sBuilder = new StringBuilder();
        foreach (var e in Selection.gameObjects)
        {
            if (PrefabUtility.IsAnyPrefabInstanceRoot(e) && e.GetInstanceID() > 0)
                continue;
            sBuilder.Append("-").Append(e.name);
            if (PrefabUtility.IsOutermostPrefabInstanceRoot(e))
                sBuilder.Append(" (prefab)");
            GUILayout.Label(sBuilder.ToString());
            sBuilder.Clear();
        }
    }

    private async void Replace(GameObject source, List<GameObject> selected)
    {
        List<int> scannedPrefabs = new List<int>();
        List<GameObject> replacementList = new List<GameObject>();
        int step = 0;
        await Task.Yield();
        while (step < selected.Count)
        {
            var objTransform = selected[step].transform;

            List<Transform> prefabChildren = new List<Transform>();
            if (inculdeAllInstances
                && PrefabUtility.IsAnyPrefabInstanceRoot(selected[step])
                && !scannedPrefabs.Contains(GetPrefabAssetId(selected[step])))
            {
                var assetId = GetPrefabAssetId(selected[step]);
                var objects = Object.FindObjectsOfType<GameObject>();
                for (int i = 0; i < objects.Length; i++)
                {
                    if (!PrefabUtility.IsAnyPrefabInstanceRoot(objects[i]) || selected.Contains(objects[i]))
                        continue;

                    if (IsObjectFromSamePrefab(objects[i], assetId))
                        selected.Add(objects[i]);

                    EditorUtility.DisplayProgressBar("Replacing Prefab", "Finding prefab instances...", i / (float)objects.Length);
                }
                scannedPrefabs.Add(assetId);

                if (retainPrefabChild)
                    foreach (var aObj in PrefabUtility.GetAddedGameObjects(selected[step]))
                    {
                        if (aObj.instanceGameObject.transform.parent != objTransform)
                            continue;

                        Undo.RegisterFullObjectHierarchyUndo(aObj.instanceGameObject, "Undo Child Operation");
                        aObj.instanceGameObject.transform.SetParent(objTransform.parent);
                        prefabChildren.Add(aObj.instanceGameObject.transform);
                    }
            }

            var replacement = PrefabUtility.InstantiatePrefab(source, objTransform.parent) as GameObject;
            replacement.transform.localPosition = objTransform.localPosition;
            replacement.transform.localRotation = objTransform.localRotation;
            replacement.transform.localScale = objTransform.localScale;
            replacement.transform.SetSiblingIndex(objTransform.GetSiblingIndex());
            if (retainNames)
                replacement.name = objTransform.name;
            replacementList.Add(replacement);
            Undo.RegisterCreatedObjectUndo(replacement, "Undo Replacement");
            foreach (var child in prefabChildren)
            {
                child.SetParent(replacement.transform);
            }
            Undo.DestroyObjectImmediate(selected[step]);

            EditorUtility.DisplayProgressBar("Replacing Prefab", "Instantiating replacement...", step / (float)selected.Count);
            step++;
        }

        EditorUtility.ClearProgressBar();
        Selection.objects = replacementList.ToArray();
    }

    private int GetPrefabAssetId(GameObject obj)
    {
        return PrefabUtility.GetCorrespondingObjectFromOriginalSource(obj).GetInstanceID();
    }

    private bool IsObjectFromSamePrefab(GameObject obj, int assetId)
    {
        return PrefabUtility.GetCorrespondingObjectFromOriginalSource(obj).GetInstanceID() == assetId;
    }
}
