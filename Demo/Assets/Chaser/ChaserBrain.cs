using System;
using System.Collections;
using System.Collections.Generic;
using SharpNeat.Phenomes;
using UnityEngine;

public class ChaserBrain : MonoBehaviour
{
    Camera main;
    Rigidbody2D body;
    float maxSpeed = 10.0f;
    ChaserGameInstance myGame;
    private AbstractChaserController controller = null;
    public ChaserEnemy myEnemy;
    private Vector2 localPhysicsPos;

    private Vector2 originalPosition;

    public float distanceFromOrb = 0;

    private void Awake()
    {
        myGame = GetComponentInParent<ChaserGameInstance>();

        main = FindObjectOfType<Camera>();
        body = GetComponent<Rigidbody2D>();
        originalPosition = new Vector2(-6.559999f, 0);
        lastSafePosition = originalPosition;
        mTrans = transform;
        mParentTrans = transform.parent;
    }

    // Start is called before the first frame update
    void Start()
    {
        controller ??= new KeyboardChaserController();
    }

    public Vector2 GetLocalPhysicsPosition()
    {
        return localPhysicsPos;
    }



    void FixedUpdate()
    {
        controller.UpdateButtons();
        var position = body.position;
        var lossyScale = mParentTrans.lossyScale;
        body.MovePosition(Vector2.MoveTowards(position,
            position + new Vector2(controller.GetXAxis(), controller.GetYAxis()) * 100,
            Time.fixedDeltaTime * maxSpeed * lossyScale.x));
        localPhysicsPos = mParentTrans.InverseTransformPoint(position);
        distanceFromOrb = (position - (Vector2)myGame.originalPickup.mTransform.position).magnitude / lossyScale.x;
        distanceAdded += 15 - distanceFromOrb;
    }

    public void SetBrain(IBlackBox brain)
    {
        if (controller is not EvolvedChaserController)
        {
            controller = new EvolvedChaserController(this, myEnemy, myGame);

        }
        ((EvolvedChaserController)controller).SetBrain(brain);
    }

    public float distanceAdded = 0;
    public void Reset()
    {
        distanceAdded = 0;
        mTrans.localPosition = originalPosition;
        lastSafePosition = originalPosition;
        if (controller is EvolvedChaserController brainCon)
        {
            brainCon.Reset();
        }
    }

    private Vector2 lastSafePosition;
    public Transform mTrans;
    private Transform mParentTrans;

    public void Respawn()
    {
        mTrans.localPosition = lastSafePosition;
    }

    public void SavePosition()
    {
        lastSafePosition = mTrans.localPosition;
    }
}