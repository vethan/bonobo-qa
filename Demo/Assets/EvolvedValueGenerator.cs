using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvolvedValueGenerator : MonoBehaviour
{
    DropFeetGameInstance gameInstance;

    public double[] inputSignals = new double[7];
    PlayerCharacter opponent;
    PlayerCharacter self;
    Collider2D opponentFoot;
    Collider2D myFoot;
    bool shouldDrop;
    bool shouldFeet;
    Rigidbody2D rb;
    Rigidbody2D opponentRigid;
    // Start is called before the first frame update
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        gameInstance = GetComponentInParent<DropFeetGameInstance>();
        var players = gameInstance.GetComponentsInChildren<PlayerCharacter>();

        foreach (var player in players)
        {
            if (player.gameObject == gameObject)
            {
                self = player;
                foreach (var collider in player.GetComponentsInChildren<Collider2D>(true))
                {
                    if (collider.tag == "Foot")
                    {
                        myFoot = collider;
                    }
                }
                continue;
            }
            opponent = player;
            opponentRigid = opponent.GetComponent<Rigidbody2D>();
            foreach (var collider in opponent.GetComponentsInChildren<Collider2D>(true))
            {
                if (collider.tag == "Foot")
                {
                    opponentFoot = collider;
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        var temp = opponent.GetLocalPhysicsPosition() - self.GetLocalPhysicsPosition();
        //var dist = temp.magnitude;
        temp.Normalize();
        inputSignals[0] = temp.x / 0.5f + 0.5f;
        inputSignals[1] = temp.y / 0.5f + 0.5f;

        inputSignals[2] = opponent.dropping ? 0 : 1;
        inputSignals[3] = self.dropping ? 0 : 1;

        inputSignals[4] = opponent.isOnFloor ? 0 : 1;
        inputSignals[5] = self.isOnFloor ? 0 : 1;


        inputSignals[6] = (self.GetLocalPhysicsPosition().y + gameInstance.vertBorder) / gameInstance.vertBorder * 2;
    }
}
