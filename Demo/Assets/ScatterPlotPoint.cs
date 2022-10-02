using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScatterPlotPoint : MonoBehaviour
{
    private MeshRenderer _renderer;

    private MaterialPropertyBlock block;

    public int layer { get; private set; }

    public ulong tally { get; private set; }

    private static Material baseMaterial;

    // Start is called before the first frame update
    void Awake()
    {
        if (baseMaterial == null)
        {
            baseMaterial = new Material(Shader.Find("Unlit/Color"));
        }

        block = new MaterialPropertyBlock();
        _renderer = GetComponent<MeshRenderer>();
        _renderer.sharedMaterial = baseMaterial;

        Destroy(GetComponent<Collider>());
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void ShowScaleColor(ulong min, ulong max)
    {
        float scale = min == max ? 1 : (tally - min) / (float) (max - min);
        _renderer.enabled = this.layer == 0 || this.layer == 3 || this.layer == 2;
        Color mainColor;
        if (scale < 0.5)
        {
            mainColor = Color.Lerp(Color.red, Color.yellow, scale * 2);
        }
        else
        {
            mainColor = Color.Lerp(Color.yellow, Color.green, (scale - 0.5f) * 2);
        }

        block.SetColor("_Color", mainColor);
        _renderer.SetPropertyBlock(block);
    }

    public void ShowLayerColor()
    {
        _renderer.enabled = true;
        Color mainColor = default;

        switch (layer)
        {
            case 0:
                mainColor = Color.Lerp(Color.green, Color.black, .7f);
                break;

            case 1:
                mainColor = Color.red;

                break;
            case 2:
                mainColor = Color.yellow;
                break;
            case 3:
                mainColor = Color.green;
                break;
        }

        block.SetColor("_Color", mainColor);
        _renderer.SetPropertyBlock(block);
    }

    public void SetLayer(int layer)
    {
        this.layer = layer;
    }

    public void SetTally(ulong newTally)
    {
        this.tally = newTally;
    }
}