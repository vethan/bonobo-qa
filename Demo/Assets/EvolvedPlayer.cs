using SharpNeat.Phenomes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvolvedPlayer : MonoBehaviour
{
    IBlackBox brain;
    Rigidbody2D ball;
    Rigidbody2D opponent;
    double[] inputSignals = new double[8];
    GameInstance parent;
    Rigidbody2D rb;
    float MaxMovementSpeed = 15.0f;

    // Start is called before the first frame update
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        parent = transform.parent.GetComponent<GameInstance>();
        ball = parent.ball;
        opponent = parent.GetComponentInChildren<EnemyAIController>().GetComponent<Rigidbody2D>();
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
        inputSignals[2] = dist;
        //NExt one: distance to ball

        //Second two, Normalised ball velocity
        temp = ball.velocity.normalized;
        inputSignals[3] = temp.x;
        inputSignals[4] = temp.y;

        //Next: Direction to enemy
        temp = opponent.position - rb.position;
        temp.Normalize();
        inputSignals[5] = temp.x;
        inputSignals[6] = temp.y;
        brain.InputSignalArray.CopyFrom(inputSignals, 0);

    }

    // Update is called once per frame
    void FixedUpdate()
    {


        OriginalInputs();
        brain.Activate();

        float xDirection = Mathf.Clamp((float)(brain.OutputSignalArray[0]-0.5) * 2,-1,1);
        float yDirection = Mathf.Clamp((float)(brain.OutputSignalArray[1]-0.5) * 2,-1,1);

        Vector2 targetTranslation = (Vector2)(parent.transform.localToWorldMatrix * new Vector3(xDirection,yDirection));
        rb.MovePosition(rb.position + (targetTranslation* MaxMovementSpeed * Time.fixedDeltaTime));
    }
}
