using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(DC_Recorder))]
public class DC_Controller_Editor : Editor
{
    Texture2D recordTex, saveTex, stopRecTex, deleteTex;
    DC_Recorder recorder;

    Vector2 fileListScrollPos;

    Color iconColor, iconDefaultColor;

    static string lastPath = "";//keep last save path to make saving more convenient

    private void OnEnable()
    {
        //get textures by asset guid
        TryLoadGUITex(ref recordTex, "2ed693417a4bae941af2c860bf8cdc43");
        TryLoadGUITex(ref saveTex, "ded4bd3d28c260548b2509609c16a568");
        TryLoadGUITex(ref stopRecTex, "6980aa127ba6c0743840f54f3293349a");
        TryLoadGUITex(ref deleteTex, "3af5f4964649cd642a287ffb2fe2fa54");
        recorder = target as DC_Recorder;

        iconColor = EditorGUIUtility.isProSkin ? new Color(0.8f,0.8f,0.8f) : new Color(0.4f,0.4f,0.4f);
        iconDefaultColor = GUI.contentColor;

        if (string.IsNullOrEmpty(lastPath))
            lastPath = Application.dataPath;
    }

    public override void OnInspectorGUI()
    {
        DrawHeader();
        DrawControls();

        if (recorder.isRecording)
            DrawRecordInfo();
        else
            DrawFileList();
        //DrawDefaultInspector();
    }

    void DrawHeader()
    {
    }

    void DrawControls()
    {
        bool playing = Application.isPlaying;
        EditorGUILayout.BeginHorizontal();


        if (GUILayout.Button(recorder.isRecording? new GUIContent(" Stop",stopRecTex,"stop recording") : new GUIContent(" Rec",recordTex,"start recording"), GUILayout.ExpandWidth(false)))
            recorder.ToggleRecord();

        GUILayout.FlexibleSpace();
        GUILayout.Label(DC_Manager.allCollectors.Count.ToString() + " data sources.", GUILayout.ExpandWidth(true));

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
    }

    void DrawRecordInfo()
    {
        GUI.contentColor = new Color(0.8f, 0, 0);

        if (Application.isPlaying == false)
        {
            GUILayout.Label("Recording will begin when entering play-mode.");
        }
        else
        {
            string recTime = System.TimeSpan.FromSeconds(Time.time - recorder.startTime).ToString("hh':'mm':'ss");

            GUILayout.Label("Start time: " + recorder.startTime.ToString("hh:mm:ss"));
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Time: " + recTime);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Frame: " + (Time.frameCount - recorder.startFrame).ToString());
            EditorGUILayout.EndHorizontal();
        }

        GUI.contentColor = iconDefaultColor;
    }

    void DrawFileList()
    {
        bool deleteAll = false;
        bool saveAll = false;
        int deleteSingle = -1;
        int saveSingle = -1;

        int fileCount = 5;

        //header >>>>>
        EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Recent records:");
            GUILayout.FlexibleSpace();

            if (fileCount == 0)
                GUI.enabled = false;

            GUI.contentColor = iconColor;
            deleteAll = GUILayout.Button(new GUIContent(deleteTex, "remove all temporary records (clear list)"), GUILayout.Height(20), GUILayout.ExpandWidth(false));
            saveAll = GUILayout.Button(new GUIContent(saveTex, "save all temporary records to a folder"), GUILayout.Height(20), GUILayout.ExpandWidth(false));
            GUI.contentColor = iconDefaultColor;

            GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        //<<<<< header
        //list >>>>>
        if (fileCount == 0)
        {
            GUILayout.Label("no recent records");
        }
        else
        {
            fileListScrollPos = EditorGUILayout.BeginScrollView(fileListScrollPos, GUILayout.Height(Mathf.Clamp(fileCount * 23, 60, 300))); //23 is line height
            for (int ct = 0; ct < fileCount; ct++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Test File 1.csv");
                GUILayout.FlexibleSpace();

                GUI.contentColor = iconColor;
                if (GUILayout.Button(new GUIContent(deleteTex, "remove this record"), GUILayout.Height(20), GUILayout.ExpandWidth(false)))
                    deleteSingle = ct;

                if (GUILayout.Button(new GUIContent(saveTex, "save this record"), GUILayout.Height(20), GUILayout.ExpandWidth(false)))
                    saveSingle = ct;
                GUI.contentColor = iconDefaultColor;

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
        //<<<<< list

        //react to buttons after UI has finished drawing
        if (deleteAll)
        {
            int result = EditorUtility.DisplayDialogComplex("Delete all Records?", "Are you sure you want to delete all temporary record files?", "Yes", "No", "Cancel");
           // if(result == 0)
                //recorder.DeleteAllRecords();
        }else if (saveAll)
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Folder", lastPath, "");
            if ( ! string.IsNullOrEmpty(selectedPath))
            {
                lastPath = selectedPath;
                //recorder.SaveAllRecords(selectedPath);
            }
        }
        else if(deleteSingle >= 0)
        {
            //recorder.DeleteRecord(deleteSingle);
        }else if(saveSingle >= 0)
        {
            string selectedPath = EditorUtility.SaveFilePanel("Save as", lastPath, "Untitled", "csv");
            if ( ! string.IsNullOrEmpty(selectedPath))
            {
                lastPath = new FileInfo(selectedPath).Directory.FullName;
                //recorder.SaveRecord(saveSingle, selectedPath);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="targetTexture">target variable for loaded texture</param>
    /// <param name="dontReplace">If true, texture will only be loaded if the variable is null.</param>
    bool TryLoadGUITex(ref Texture2D targetTexture, string guid, bool dontReplace = true)
    {
        if (targetTexture != null && dontReplace)
            return true;
        
        Texture2D obj = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid));
        if(obj == null)
        {
            Debug.LogError("DC_Controller_Editor: Unable to find texture - " + guid);
            return false;
        }
        else
        {
            targetTexture = obj;
            return true;
        }
    }
}
