using SharpNeat.Phenomes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvolvedPlayer : MonoBehaviour
{
    IBlackBox brain;
    Rigidbody2D ball;
    Rigidbody2D opponent;
    double[] inputSignals = new double[6];
    GameInstance parent;
    Rigidbody2D rb;
    float MaxMovementSpeed = 15.0f;
    Vector3 startingPosition;
    // Start is called before the first frame update
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        parent = transform.parent.GetComponent<GameInstance>();
        ball = parent.ball;
        opponent = parent.GetComponentInChildren<EnemyAIController>().GetComponent<Rigidbody2D>();

    }

    private void OnEnable()
    {
        startingPosition = rb.transform.localPosition;
    }

    public void Reset()
    {
        rb.transform.localPosition = startingPosition;
    }

    public void SetBrain(IBlackBox newBrain)
    {
        brain = newBrain;
    }

    void OriginalInputs()
    {
        //First two: Normalised direction to ball

        var temp = ball.position - rb.position;
        var dist = temp.magnitude;
        temp.Normalize();
        inputSignals[0] = temp.x;
        inputSignals[1] = temp.y;
        //NExt one: distance to ball

        //Second two, Normalised ball velocity
        temp = ball.velocity.normalized;
        inputSignals[2] = temp.x;
        inputSignals[3] = temp.y;

        //Next: Direction to enemy
        temp = opponent.position - rb.position;
        dist = temp.magnitude;
        temp.Normalize();
        inputSignals[4] = temp.x;
        inputSignals[5] = temp.y;
        brain.InputSignalArray.CopyFrom(inputSignals, 0);

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        /*
        //Calculate Y Range:
        float yWidth = 4.5f;
        float yStart = -4.5f;

        //XRange
        float xWidth = 9;
        float xStart = -9;

        var tempPos = ball.transform.localPosition;
        inputSignals[0] = (tempPos.x) / xWidth;
        inputSignals[1] = (tempPos.y) / yWidth;
        // Debug.Log(inputSignals[0]);
        tempPos = transform.localPosition;
        inputSignals[2] = (tempPos.x) / xWidth;
        inputSignals[3] = (tempPos.y) / yWidth;

        tempPos = opponent.transform.localPosition;
        inputSignals[4] = (tempPos.x) / xWidth;
        inputSignals[5] = (tempPos.y) / yWidth;

        //tempPos = ball.velocity.normalized;
        //inputSignals[6] = tempPos.x;
        //inputSignals[7] = tempPos.y;
        brain.InputSignalArray.CopyFrom(inputSignals, 0);*/

        OriginalInputs();
        brain.Activate();
        OriginalInputs();
        brain.Activate();

        float xDirection = Mathf.Clamp((float)(brain.OutputSignalArray[0]-0.5) * 2,-1,1);
        float yDirection = Mathf.Clamp((float)(brain.OutputSignalArray[1]-0.5) * 2,-1,1);
        if(Mathf.Abs(xDirection) < 0.2f)
        {
            xDirection = 0;
        }
        if (Mathf.Abs(yDirection) < 0.2f)
        {
            yDirection = 0;
        }
        Vector2 targetTranslation = (Vector2)(parent.transform.localToWorldMatrix * new Vector3(xDirection,yDirection));
        rb.velocity = ((targetTranslation* MaxMovementSpeed));
    }
}
