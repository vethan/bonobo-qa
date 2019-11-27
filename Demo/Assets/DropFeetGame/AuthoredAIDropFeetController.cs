using UnityEngine;

public class AuthoredAIDropFeetController : AbstractDropFeetController
{
    DropFeetGameInstance gameInstance;
    PlayerCharacter opponent;
    PlayerCharacter self;
    Collider2D opponentFoot;
    Collider2D myFoot;
    bool shouldDrop;
    bool shouldFeet;
    Rigidbody2D myRigid;
    Rigidbody2D opponentRigid;

    int meOnlyLayerMask;
    int opponentOnlyLayerMask;

    public override bool DropButtonDown()
    {
        return shouldDrop;
    }

    public override bool FeetButtonDown()
    {
        return shouldFeet;
    }

    // Start is called before the first frame update
    void Awake()
    {
        myRigid = GetComponent<Rigidbody2D>();
        gameInstance = GetComponentInParent<DropFeetGameInstance>();
        var players = gameInstance.GetComponentsInChildren<PlayerCharacter>();
        
        foreach (var player in players)
        {
            if (player.gameObject == gameObject)
            {
                self = player;
                meOnlyLayerMask = LayerMask.GetMask(LayerMask.LayerToName(player.gameObject.layer));
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
            opponentOnlyLayerMask = LayerMask.GetMask(LayerMask.LayerToName(player.gameObject.layer));
            foreach (var collider in opponent.GetComponentsInChildren<Collider2D>(true))
            {
                if(collider.tag == "Foot")
                {
                    opponentFoot = collider;
                }
            }
        }
    }

    bool OpponentAttackWillHit()
    {
        var attackVector = opponent.GetAttackVector();
        Debug.DrawRay(opponentFoot.transform.position, attackVector);
        return Physics2D.Raycast(opponentFoot.transform.position, attackVector, 50, meOnlyLayerMask);
    }

    bool MyAttackWillHit()
    {
        var attackVector = self.GetAttackVector();
        Debug.DrawRay(myFoot.transform.position, attackVector);
        return Physics2D.Raycast(myFoot.transform.position, attackVector, 50, opponentOnlyLayerMask);
    }

    // Update is called once per frame
    void Update()
    {
        shouldDrop = false;
        shouldFeet = false;

        if(self.dropping)
        {
            return;
        }

        if(self.isOnFloor)
        {
            //
            if (opponent.isOnFloor)
            {
                shouldDrop = true;
                return;
            }
            else
            {
                if(OpponentAttackWillHit())
                {
                    shouldFeet = true;
                }
            }
        }
        else
        {
            if (MyAttackWillHit())
            {
                shouldFeet = true;
            }
        }
    }
}
