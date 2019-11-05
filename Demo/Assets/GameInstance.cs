using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameInstance : MonoBehaviour
{
    public SpriteRenderer[] walls;
    public Rigidbody2D ball;
    public Goal leftGoal;
    public Goal rightGoal;
    public Transform leftScoreSprite;
    public Transform rightScoreSprite;
    int left;
    int right;
    public Camera zoomCam;
    public EvolvedPlayer evolved;


    public bool selected { get; private set; }
    bool filtered;
    // Start is called before the first frame update
    void Start()
    {
        zoomCam = GetComponentInChildren<Camera>();
        zoomCam.orthographicSize =  Mathf.Abs(leftScoreSprite.position.y- rightScoreSprite.position.y) * 0.55f;
        zoomCam.enabled = false;
        zoomCam.depth = 2;
        zoomCam.eventMask = ~zoomCam.cullingMask;
        Reset();
        leftGoal.OnCollision += (go) =>
        {
            if (go == ball.gameObject)
            {
                right++;
                UpdateScoreImages();
                Reset(-1);
            }
        };

        rightGoal.OnCollision += (go) =>
        {
            if (go == ball.gameObject)
            {
                left++;
                UpdateScoreImages();
                Reset(1);
            }
        };
    }

    public void ToggleSelect()
    {
        selected = !selected;
    }

    private void OnMouseDown()
    {
        zoomCam.enabled = true;
    }

    private void OnMouseUp()
    {
        zoomCam.enabled = false;
    }


    private void Update()
    {
        Color wallColor = Color.white;
        if(selected)
        {
            wallColor = Color.blue;
        }

        if(filtered)
        {
            wallColor = wallColor * 0.5f;
        }

        foreach(SpriteRenderer render in walls)
        {
            render.color = wallColor;
        }
    }

    private void Reset(int direction = -1)
    {
        ball.position = transform.position;
        ball.velocity = Vector3.right * direction * 3f * transform.lossyScale.x;
    }


    void UpdateScoreImages()
    {
        Vector3 val = leftScoreSprite.transform.localScale;
        val.x = 0.25f * Mathf.Clamp(left, 0, 20);
        leftScoreSprite.transform.localScale = val;

        val = rightScoreSprite.transform.localScale;
        val.x = 0.5f * Mathf.Clamp(right, 0, 20);
        rightScoreSprite.transform.localScale = val;
    }

    internal void FullReset()
    {
        left = 0;
        right = 0;
        selected = false;
        UpdateScoreImages();
        Reset();
        GetComponentInChildren<EvolvedPlayer>().Reset();
        GetComponentInChildren<EnemyAIController>().Reset();

    }
}
