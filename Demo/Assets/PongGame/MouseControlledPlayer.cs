using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseControlledPlayer : MonoBehaviour
{
    Camera main;
    Rigidbody2D body;
    float maxSpeed = 20.0f;
    GameInstance myGame;

    // Start is called before the first frame update
    void Start()
    {
        myGame = GetComponentInParent<GameInstance>();

        main = FindObjectOfType<Camera>();
        body = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        var viewPoint = main.ScreenToViewportPoint(Input.mousePosition);
        Vector3 mousePosition = new Vector3(Mathf.Lerp(-7.5f,7.5f, viewPoint.x), Mathf.Lerp(-3.5f, 3.5f, viewPoint.y));
        mousePosition.z = 0;

        mousePosition = (myGame.transform.localToWorldMatrix * mousePosition);
        mousePosition += (myGame.transform.position);

        //transform.position = mousePosition;
        body.MovePosition(Vector2.MoveTowards(transform.position,mousePosition,Time.fixedDeltaTime * maxSpeed * transform.parent.lossyScale.x));
    }
}
