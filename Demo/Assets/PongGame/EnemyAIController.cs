using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAIController : MonoBehaviour
{
    GameInstance myGame;
    Rigidbody2D Puck;
    bool isFirstTimeInOpponentsHalf = true;
    float offsetYFromTarget = 0;
    float MaxMovementSpeed = 30.0f;
    private Rigidbody2D rb;
    private Vector2 startingPosition;
    private Vector2 targetPosition;
    private Vector2 rawTargetPosition;

    // Start is called before the first frame update
    void OnEnable()
    {
        rb = GetComponent<Rigidbody2D>();
        startingPosition = rb.transform.localPosition;
        myGame = GetComponentInParent<GameInstance>();
        Puck = myGame.ball;
    }
    public void Reset()
    {
        rb.transform.localPosition = startingPosition;
    }
    // Update is called once per frame
    private void FixedUpdate()
    {
        float movementSpeed;

        if (Puck.transform.localPosition.x < 0)
        {
            if (isFirstTimeInOpponentsHalf)
            {
                isFirstTimeInOpponentsHalf = false;
                offsetYFromTarget = Random.Range(-2f, 2f);
            }

            movementSpeed = MaxMovementSpeed * Random.Range(0.1f, 0.3f) * myGame.transform.lossyScale.x;
            rawTargetPosition = new Vector2(startingPosition.x,
                Mathf.Clamp(Puck.transform.localPosition.y + offsetYFromTarget, -3.5f, 3.5f));
            targetPosition = (myGame.transform.localToWorldMatrix * rawTargetPosition) ;
            targetPosition += (Vector2)(myGame.transform.position);
        }
        else
        {
            isFirstTimeInOpponentsHalf = true;

            movementSpeed = Random.Range(MaxMovementSpeed * 0.4f, MaxMovementSpeed) * myGame.transform.lossyScale.x;
            rawTargetPosition = new Vector2(Mathf.Clamp(Puck.transform.localPosition.x,1.0f,7.5f),
                                        Mathf.Clamp(Puck.transform.localPosition.y,-3.5f,3.5f));
            targetPosition = myGame.transform.localToWorldMatrix * rawTargetPosition;
            targetPosition += (Vector2)(myGame.transform.position);
        }

        rb.MovePosition(Vector2.MoveTowards(rb.position, targetPosition,
                movementSpeed * Time.fixedDeltaTime));
    }
}
