using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScatterPlot : MonoBehaviour
{
    private Material baseMaterial;
    private List<Material> layerMats;

    private Stack<MeshRenderer> pointPool;

    private Stack<MeshRenderer> points;

    public int layer0Size;
    public int layer1Size;
    public int layer2Size;
    // Start is called before the first frame update
    void Awake()
    {
        pointPool = new Stack<MeshRenderer>();
        points = new Stack<MeshRenderer>();
        layerMats = new List<Material>();
        baseMaterial = new Material(Shader.Find("Unlit/Color"));

        var newMat = new Material(baseMaterial);
        newMat.color = Color.green;
        layerMats.Add(newMat);
        newMat = new Material(baseMaterial);
        newMat.color = Color.red;
        layerMats.Add(newMat);
        newMat = new Material(baseMaterial);
        newMat.color = Color.yellow;
        layerMats.Add(newMat);
    }

    MeshRenderer GetPoint()
    {
        if (pointPool.Count() > 0)
        {
            var point = pointPool.Pop();
            point.enabled = true;
            return point;
        }

        var newPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        newPoint.transform.parent = transform;
        Destroy(newPoint.GetComponent<Collider>());
        
        return newPoint.GetComponent<MeshRenderer>();
    }


    public void DrawLayer(Vector3[] layerPoints, int layer)
    {
        if (layer == 0)
        {
            layer0Size = layerPoints.Count();
        }
        if (layer == 1)
        {
            layer1Size = layerPoints.Count();
        }
        foreach (var point in layerPoints)
        {
            var sphere = GetPoint();
            sphere.material = layerMats[layer];
            sphere.transform.localPosition = point * 50;
            points.Push(sphere);
        }
    }

    public void ClearGraph()
    {
        while (points.Count() > 0)
        {
            var thing = points.Pop();
            thing.enabled = false;
            pointPool.Push(thing);
        }
    }
}