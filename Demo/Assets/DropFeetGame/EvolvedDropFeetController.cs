using SharpNeat.Phenomes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvolvedDropFeetController : AbstractDropFeetController
{
    DropFeetGameInstance gameInstance;
    PlayerCharacter opponent;
    PlayerCharacter self;
    Collider2D opponentFoot;
    Collider2D myFoot;
    bool shouldDrop;
    bool shouldFeet;
    Rigidbody2D rb;
    Rigidbody2D opponentRigid;
    IBlackBox brain;

    public bool everDrop { get; private set; }
    public bool everFeet { get; private set; }

    double[] inputSignals = new double[10];

    public void Reset()
    {
        everDrop = false;
        everFeet = false;
    }

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


    public void SetBrain(IBlackBox newBrain)
    {
        brain = newBrain;
        everDrop = false;
        everFeet = false;
    }

    public override bool DropButtonDown()
    {
        return shouldDrop;
    }

    public override bool FeetButtonDown()
    {
        return shouldFeet;
    }

    void OriginalInputs()
    {

        var temp = opponentRigid.position - rb.position;
        var dist = temp.magnitude;
        temp.Normalize();
        inputSignals[0] = temp.x;
        inputSignals[1] = temp.y;

        temp = opponent.velocity.normalized;
        inputSignals[2] = temp.x;
        inputSignals[3] = temp.y;

        temp = (Vector2)opponentFoot.transform.position- rb.position;
        dist = temp.magnitude;
        temp.Normalize();
        inputSignals[4] = temp.x;
        inputSignals[5] = temp.y;

        inputSignals[6] = opponent.dropping ? 0 : 1;
        inputSignals[7] = self.dropping ? 0 : 1;

        inputSignals[8] = opponent.isOnFloor ? 0 : 1;
        inputSignals[9] = self.isOnFloor ? 0 : 1;
        brain.InputSignalArray.CopyFrom(inputSignals, 0);
    }

    public override void UpdateButtons()
    {
        OriginalInputs();
        brain.Activate();
        shouldDrop = brain.OutputSignalArray[0] > 0.5f;
        shouldFeet = brain.OutputSignalArray[1] > 0.5f;
        everFeet |= self.dropping;
        everDrop |= shouldDrop;
        
    }
}
