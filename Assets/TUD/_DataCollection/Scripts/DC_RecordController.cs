using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class DC_RecordController : MonoBehaviour
{

    

    #region operations
    public void DeleteAllRecords()
    {
        DirectoryInfo dir = GetTempFilePath();
        FileInfo[] allFiles = dir.GetFiles("*.csv");
        for (int ct = 0; ct < allFiles.Length; ct++)
        {
            Debug.Log("ToDelete: " + allFiles[ct].FullName);
            //allFiles[ct].Delete();
        }
    }
    public void SaveAllRecords(string folderPath)
    {
        DirectoryInfo dir = GetTempFilePath();
        FileInfo[] allFiles = dir.GetFiles("*.csv");
        for (int ct = 0; ct < allFiles.Length; ct++)
        {
            allFiles[ct].CopyTo(Path.Combine(folderPath, allFiles[ct].Name),false);
        }
    }
    public void DeleteRecord(int index)
    {

    }
    public void SaveRecord(int index, string path)
    {

    }
    #endregion

    #region utility
    DirectoryInfo GetTempFilePath()
    {
        DirectoryInfo dir = new DirectoryInfo(Path.Combine(Application.persistentDataPath, "Temp_DC_Records"));
        if(dir.Exists == false)
            dir.Create();
        return dir;
    }

    #endregion

}
