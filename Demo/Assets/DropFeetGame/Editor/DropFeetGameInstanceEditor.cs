using SharpNeat.Genomes.Neat;
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
        
        var instance = (DropFeetGameInstance)serializedObject.targetObject;
        serializedObject.Update();

        if (instance.genome == null)
            return;
        EditorGUILayout.LabelField("Species: " + instance.genome.SpecieIdx);
        EditorGUILayout.LabelField("Fitness: " + instance.genome.EvaluationInfo.Fitness);
        EditorGUILayout.LabelField("Birth Generation: " + instance.genome.BirthGeneration);
        
        if(GUILayout.Button("Save Genome"))
        {
            string filename = string.Format("Genome{0:yyyy-dd-M--HH-mm-ss}Species{1}.xml", System.DateTime.Now, instance.genome.SpecieIdx);

            string path = EditorUtility.SaveFilePanel("Save Genome File","", filename , "xml");

            if (path.Length != 0)
            {
                var xmlDoc = NeatGenomeXmlIO.SaveComplete(instance.genome, false);
                xmlDoc.Save(path);
            }
        }

    }
}
