using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(LoadGenomeManager))]
public class LoadGenomeManagerEditor : Editor
{
    // Start is called before the first frame update
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Select Folder"))
        {
            string path = AssetsRelativePath( EditorUtility.OpenFolderPanel("Select Genome Folder", "", ""));

            if (path.Length != 0)
            {
                List<TextAsset> xmlFiles = new List<TextAsset>();
                var assets = AssetDatabase.FindAssets("t:TextAsset", new[] { path });
                foreach(var asset in assets)
                {
                    if(!(AssetDatabase.GUIDToAssetPath(asset).EndsWith("bytes") || AssetDatabase.GUIDToAssetPath(asset).EndsWith("xml")))
                    {
                        continue;
                    }
                    xmlFiles.Add(AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(asset)));
                }
                ((LoadGenomeManager)serializedObject.targetObject).genomesToLoad = xmlFiles.ToArray();
                GameCreator creator = FindObjectOfType<GameCreator>();
                creator.gamesToShow = 4;
                creator.gamesToCreate = xmlFiles.Count;
                creator.inspectionMode = true;
                serializedObject.Update();
                EditorUtility.SetDirty(creator);

            }
        }
        
    }

    public static string AssetsRelativePath(string absolutePath)
    {
        if (absolutePath.StartsWith(Application.dataPath))
        {
            return "Assets" + absolutePath.Substring(Application.dataPath.Length);
        }
        else
        {
            throw new System.ArgumentException("Full path does not contain the current project's Assets folder", "absolutePath");
        }
    }
}
