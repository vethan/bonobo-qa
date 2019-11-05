using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Goal : MonoBehaviour
{
    public enum GoalSide { Left,Right}
    public System.Action<GameObject> OnCollision =(_) => { };
    public GoalSide ownerSide = GoalSide.Left;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        OnCollision(collision.gameObject);
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
