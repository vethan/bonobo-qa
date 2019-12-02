using UnityEngine;

public class AuthoredAIDropFeetController : AbstractDropFeetController
{
    protected PlayerCharacter opponent;
    protected PlayerCharacter self;
    protected Collider2D opponentFoot;
    protected Collider2D myFoot;
    protected bool shouldDrop;
    protected bool shouldFeet;
    protected Rigidbody2D myRigid;
    protected Rigidbody2D opponentRigid;
    protected DropFeetGameInstance gameInstance;

    protected int meOnlyLayerMask;
    protected int opponentOnlyLayerMask;

    public override bool DropButtonDown()
    {
        return shouldDrop;
    }

    public override bool FeetButtonDown()
    {
        return shouldFeet;
    }

    // Start is called before the first frame update
    protected void Awake()
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

    protected bool OpponentAttackWillHit()
    {
        var attackVector = opponent.GetAttackVector();
        Debug.DrawRay(opponentFoot.transform.position, attackVector);
        return Physics2D.Raycast(opponentFoot.transform.position, attackVector, 50, meOnlyLayerMask);
    }

    protected bool MyAttackWillHit()
    {
        var attackVector = self.GetAttackVector();
        Debug.DrawRay(myFoot.transform.position, attackVector);
        return Physics2D.Raycast(myFoot.transform.position, attackVector, 50, opponentOnlyLayerMask);
    }

    public override void UpdateButtons()
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
