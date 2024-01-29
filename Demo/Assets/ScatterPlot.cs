using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScatterPlot : MonoBehaviour
{
    private Stack<ScatterPlotPoint> pointPool;

    private Stack<ScatterPlotPoint> points;

    public int layer0Size;
    public int layer1Size;

    public int layer2Size;
    public int layer3Size;

    private bool showHotspots = false;

    private float nextFlipTime = 0;

    private float FlipFrequency = 5.0f;

    // Start is called before the first frame update
    void Awake()
    {
        pointPool = new Stack<ScatterPlotPoint>();
        points = new Stack<ScatterPlotPoint>();
        nextFlipTime = FlipFrequency + Time.unscaledTime;
    }

    ScatterPlotPoint GetPoint()
    {
        if (pointPool.Count() > 0)
        {
            var point = pointPool.Pop();
            point.enabled = true;
            return point;
        }

        var newPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        newPoint.transform.parent = transform;


        return newPoint.AddComponent<ScatterPlotPoint>();
    }

    ulong minCount = UInt64.MaxValue;
    ulong maxCount = 0;

    public void DrawLayer(Vector3[] layerPoints, GenomeMetric[] data, int layer)
    {
        if (layer == 0)
        {
            layer0Size = layerPoints.Count();
        }

        if (layer == 1)
        {
            layer1Size = layerPoints.Count();
        }

        if (layer == 2)
        {
            layer2Size = layerPoints.Count();
        }

        if (layer == 3)
        {
            layer3Size = layerPoints.Count();
        }

        //Do we need to update the min/max values
        if (layer != 1)
        {
            minCount = UInt64.MaxValue;
            maxCount = 0;
            for (int i = 0; i < layerPoints.Length; i++)
            {
                minCount = minCount > data[i].nearTally ? data[i].nearTally : minCount;
                maxCount = maxCount < data[i].nearTally ? data[i].nearTally : maxCount;
            }

            foreach (ScatterPlotPoint point in points)
            {
                if (point.layer == layer || point.layer == 1)
                {
                    continue;
                }

                minCount = minCount > point.tally ? point.tally : minCount;
                maxCount = maxCount < point.tally ? point.tally : maxCount;
            }

            if (showHotspots)
            {
                foreach (var plotPoint in points)
                {

                    plotPoint.ShowScaleColor(minCount, maxCount);

                }
            }
        }

        for (int i = 0; i < layerPoints.Length; i++)
        {
            var sphere = GetPoint();
            sphere.SetLayer(layer);
            sphere.transform.localPosition = layerPoints[i] * 50;
            sphere.SetGenome(data[i].genome);
            sphere.SetTally(data[i].nearTally);
            if (showHotspots)
            {
                sphere.ShowScaleColor(minCount,maxCount);
            }
            else
            {
                sphere.ShowLayerColor();
            }

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

    private void Update()
    {
        if (Time.unscaledTime > nextFlipTime)
        {
            nextFlipTime = FlipFrequency + Time.unscaledTime;
            showHotspots = !showHotspots;
            foreach (var plotPoint in points)
            {
                if (showHotspots)
                {
                    plotPoint.ShowScaleColor(minCount,maxCount);
                }
                else
                {
                    plotPoint.ShowLayerColor();
                }
            }
        }
    }
}