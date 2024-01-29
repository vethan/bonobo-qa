﻿using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public abstract class AbstractGameInstance : MonoBehaviour
{
    public SpriteRenderer[] walls;
    public Camera zoomCam;
    public Camera displayCam;
    public Material graphMaterial;
    public Material graphNodeMaterial;
    protected Transform graphRoot;
    protected Transform labelRoot;

    public bool selected { get; protected set; }
    public bool interesting { get; protected set; }
    public bool filtered;
    public float horizBorder = 0;
    public float vertBorder = 0;

    public abstract int InputCount { get; }
    public abstract int OutputCount { get; }

    public bool isPlayerControlled;
    // Start is called before the first frame update
    virtual protected void Awake()
    {
        zoomCam.enabled = false;
        displayCam.enabled = false;
        foreach (var wall in walls)
        {
            if (Mathf.Abs(wall.transform.localPosition.x) > horizBorder)
                horizBorder = Mathf.Abs(wall.transform.localPosition.x);

            if (Mathf.Abs(wall.transform.localPosition.y) > vertBorder)
            {
                vertBorder = Mathf.Abs(wall.transform.localPosition.y);
            }
        }
    }

    public void DisableGraph()
    {
        return;
        graphRoot.gameObject.SetActive(false);
    }

    private void OnDrawGizmos()
    {
        if (displayCam == null || !displayCam.enabled)
            return;

        Gizmos.DrawIcon(transform.position, "watch.png",false);



    }

    public bool GameDone { get; protected set; } = false;
    public abstract string GameName { get; }


    public void AlignCameras()
    {
        var trueVert = transform.TransformVector(horizBorder, vertBorder, 1).y;
        zoomCam.orthographicSize = 2 * trueVert * 0.55f;
        zoomCam.depth = 2;
        zoomCam.eventMask = ~zoomCam.cullingMask;

        displayCam.orthographicSize = 2 * trueVert * 0.55f;
        displayCam.depth = 1;
    }

    protected virtual void Start()
    {

        AlignCameras();
    }

    public void ToggleSelect()
    {
        selected = !selected;
    }

    private void OnMouseDown()
    {
        if (isPlayerControlled)
            return;
        zoomCam.enabled = true;
        if(labelRoot != null)
            labelRoot.gameObject.SetActive(true);
    }

    private void OnMouseUp()
    {
        if (isPlayerControlled)
            return;
        zoomCam.enabled = false;
        if (labelRoot != null)
            labelRoot.gameObject.SetActive(false);
    }

    protected abstract string GetOutputLabel(int index);
    protected abstract string GetInputLabel(int index);
    public abstract float CalculateFitness();
    public abstract void FullReset();
    public abstract void SetEvolvedBrain(IBlackBox blackBox, NeatGenome genome);
    
    public abstract Dictionary<string, float> GetGameStats();

    
    protected virtual void Update()
    {

        Color wallColor = Color.white;
        if (selected)
        {
            wallColor = Color.blue;
        }

        if (filtered)
        {
            wallColor = wallColor * 0.5f;
        }

        foreach (SpriteRenderer render in walls)
        {
            render.color = wallColor;
        }
    }

    internal virtual void SetGraph(Graph graph)
    {
        return;
        if (graphRoot != null)
        {
            return; //DONT REMAKE THE GRAPH
            //Destroy(graphRoot.gameObject);
            
        }


        graphRoot = new GameObject("GraphRoot").transform;
        graphRoot.parent = transform;
        graphRoot.localPosition = new Vector3(0, 0, 20);
        graphRoot.localScale = Vector3.one;
        float vertAmount = (vertBorder * 2) / (graph.layers.Count + 1);
        float vertStart = vertBorder - vertAmount;
        Dictionary<uint, Vector3> nodePositions = new Dictionary<uint, Vector3>();
        labelRoot = new GameObject("LabelRoot").transform;
        labelRoot.parent = graphRoot;
        labelRoot.localPosition = new Vector3(0, 0, 0);
        labelRoot.localScale = Vector3.one;
        for (int i = 0; i < graph.layers.Count; i++)
        {
            float horizAmount = (horizBorder * 2) / (graph.layers[i].Count + 1);
            float horizStart = horizAmount - horizBorder;

            for (int j = 0; j < graph.layers[i].Count; j++)
            {

                var node = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
                node.GetComponent<Renderer>().material = graphNodeMaterial;
                nodePositions[graph.layers[i][j]] = new Vector3(horizStart + j * horizAmount, vertStart - i * vertAmount);
                node.parent = graphRoot;
                node.localPosition = nodePositions[graph.layers[i][j]];
                node.localScale = Vector3.one;
                if (i == 0)
                {
                    var text = new GameObject("Label").transform;
                    text.parent = labelRoot;
                    text.localPosition = nodePositions[graph.layers[i][j]] + Vector3.up * (graph.layers[i].Count > 5 && j % 2 ==0? 1.25f : .75f);
                    text.localScale = Vector3.one * 0.5f;
                    var tmpro = text.gameObject.AddComponent<TMPro.TextMeshPro>();
                    tmpro.text = GetInputLabel(j);
                    tmpro.fontSize = 4;
                    tmpro.alignment = TextAlignmentOptions.Center;
                }
                if (i == graph.layers.Count - 1)
                {
                    var text = new GameObject("Label").transform;
                    text.parent = labelRoot;
                    text.localPosition = nodePositions[graph.layers[i][j]] + Vector3.up * -(graph.layers[i].Count > 5 &&  j % 2 == 0 ? 1.25f : .75f); 
                    text.localScale = Vector3.one * 0.5f;
                    var tmpro = text.gameObject.AddComponent<TMPro.TextMeshPro>();
                    tmpro.text = GetOutputLabel(j);
                    tmpro.fontSize = 4;
                    tmpro.alignment = TextAlignmentOptions.Center;

                }
            }
        }
        foreach (var connection in graph.connections)
        {
            if (!nodePositions.ContainsKey(connection.Item1) || !nodePositions.ContainsKey(connection.Item2))
                continue;
            var connectionLine = new GameObject("connection").AddComponent<LineRenderer>();
            connectionLine.transform.parent = graphRoot;
            connectionLine.transform.localPosition = Vector3.zero;
            connectionLine.transform.localScale = Vector3.one;
            connectionLine.positionCount = (2);
            connectionLine.startWidth = 0.4f;
            connectionLine.endWidth = 0.4f;
            connectionLine.material = graphMaterial;

            // we want the lines to use local space and not world space
            connectionLine.useWorldSpace = false;
            connectionLine.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            connectionLine.receiveShadows = false;
            connectionLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            //connectionLine.material.color = Color.red;
            connectionLine.SetPositions(new Vector3[] { nodePositions[connection.Item1], nodePositions[connection.Item2] });


        }
        labelRoot.gameObject.SetActive(zoomCam.enabled);
    }
}
