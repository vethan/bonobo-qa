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


    public override bool FixedUpdateController()
    {
        return true;
    }
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
        return OpponentAttackWillHit(out RaycastHit2D result);
    }

    protected bool OpponentAttackWillHit(out RaycastHit2D hitResult)
    {
        var attackVector = opponent.GetAttackVector();
        ContactFilter2D contactFilter2D = new ContactFilter2D() { layerMask = meOnlyLayerMask, useLayerMask = true, useTriggers = false };
        RaycastHit2D[] hit = new RaycastHit2D[1];
        Debug.DrawRay(opponentFoot.transform.position, attackVector);
        float radius = opponentFoot.transform.localScale.x * ((CircleCollider2D)opponentFoot).radius * 0.85f;
        int hits = Physics2D.CircleCast(opponentFoot.transform.position, radius, attackVector, contactFilter2D, hit, (self.transform.position - opponentFoot.transform.position).magnitude * 2f);
        hitResult = hit[0];
        return hits > 0 && hit[0].transform == transform;
    }

    protected bool MyAttackWillHit(out RaycastHit2D hitResult)
    {
        var attackVector = self.GetAttackVector();
        ContactFilter2D contactFilter2D = new ContactFilter2D() { layerMask = opponentOnlyLayerMask, useLayerMask = true, useTriggers = false };
        RaycastHit2D[] hit = new RaycastHit2D[1];
        Debug.DrawRay(myFoot.transform.position, attackVector);
        float radius = myFoot.transform.localScale.x * ((CircleCollider2D)myFoot).radius * 0.85f;
        int hits = Physics2D.CircleCast(myFoot.transform.position, radius, attackVector, contactFilter2D, hit,(myFoot.transform.position-opponent.transform.position).magnitude*2f);
        hitResult = hit[0];
        return hits > 0 && hit[0].transform == opponent.transform;
    }

    protected bool MyAttackWillHit()
    {
        return MyAttackWillHit(out RaycastHit2D result);
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
