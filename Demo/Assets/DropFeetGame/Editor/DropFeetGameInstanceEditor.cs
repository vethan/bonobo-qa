using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DropFeetGameInstance))]
public class DropFeetGameInstanceEditor : Editor
{
    SerializedProperty lookAtPoint;

    void OnEnable()
    {
        lookAtPoint = serializedObject.FindProperty("lookAtPoint");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();
        
        var instance = (DropFeetGameInstance)serializedObject.targetObject;
        if (instance.genome == null)
            return;
        EditorGUILayout.LabelField("Species: " + instance.genome.SpecieIdx);
        EditorGUILayout.LabelField("Fitness: " + instance.genome.EvaluationInfo.Fitness);
        EditorGUILayout.LabelField("Birth Generation: " + instance.genome.BirthGeneration);
    }
}
