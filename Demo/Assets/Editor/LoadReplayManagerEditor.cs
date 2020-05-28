using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(LoadReplayManager))]
public class LoadReplayManagerEditor : Editor
{
    // Start is called before the first frame update
    public override void OnInspectorGUI()
    {
        var manager = ((LoadReplayManager)serializedObject.targetObject);
        if (manager.replaysToLoad!=null && manager.replaysToLoad.Length !=0)
        {
            GUILayout.TextArea("CurrentReplay: " + manager.replayNames[manager.focusIndex]);
        }
        base.OnInspectorGUI();
        if (GUILayout.Button("Select Folder"))
        {
            string path = LoadGenomeManagerEditor.AssetsRelativePath(EditorUtility.OpenFolderPanel("Select Replay Folder", "", ""));

            if (path.Length != 0)
            {
                string[] files = Directory.GetFiles(path);
                List<TextAsset> replayFiles = new List<TextAsset>();
                var assets = AssetDatabase.FindAssets("t:TextAsset", new[] { path });
                List<string> fileNemse = new List<string>();
                foreach (var asset in assets)
                {
                    replayFiles.Add(AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(asset)));
                    fileNemse.Add(AssetDatabase.GUIDToAssetPath(asset));
                }
                ((LoadReplayManager)serializedObject.targetObject).replaysToLoad = replayFiles.ToArray();
                ((LoadReplayManager)serializedObject.targetObject).replayNames = fileNemse.ToArray();
                GameCreator creator = FindObjectOfType<GameCreator>();
                creator.gamesToShow = 4;
                creator.gamesToCreate = replayFiles.Count;
                creator.inspectionMode = true;
                serializedObject.Update();
                EditorUtility.SetDirty(creator);
            }
        }

    }
}

