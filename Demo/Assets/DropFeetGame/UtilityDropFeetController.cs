using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UtilityDropFeetController : AuthoredAIDropFeetController
{
    public bool DebugMode = false;
    struct UtilityOption
    {
        public Action actionType;
        public float value;
    }
    float characterHeight = 2.3f;
    enum Action
    {
        DoNothing,
        DiveKick,
        Jump,
        JumpBack
    }

    float DoNothingUtility()
    {
        return .5f;
    }

    bool IsLocalXInFront(float positionToCheck)
    {
        return Mathf.Sign(self.GetLocalPhysicsPosition().x - opponent.GetLocalPhysicsPosition().x) == Mathf.Sign(self.GetLocalPhysicsPosition().x - positionToCheck);
    }

    bool PredictOpponentPositionWhenAttacking(out Vector2 hitPoint)
    {
        if (opponent.dropping)
        {
            float myC = 0;
            float myM = 0;
            if(self.GetAttackVector().x < 0)
            {
                myC = self.GetLocalPhysicsPosition().y - self.GetLocalPhysicsPosition().x;
                myM = 1;
            }
            else
            {
                myC = self.GetLocalPhysicsPosition().y + self.GetLocalPhysicsPosition().x;
                myM = -1;
            }

            float oppC = 0;
            float oppM = 0;
            if (opponent.GetAttackVector().x < 0)
            {
                oppC = opponent.GetLocalPhysicsPosition().y - opponent.GetLocalPhysicsPosition().x;
                oppM = 1;
            }
            else
            {
                oppC = opponent.GetLocalPhysicsPosition().y + opponent.GetLocalPhysicsPosition().x;
                oppM = -1;
            }

            float diff = oppM - myM;
            float cDiff = myC - oppC;
            if(diff == 0)
            {
                hitPoint = Vector2.zero;
                return false;
            }
            float x = cDiff / diff;
            hitPoint = new Vector2(x, myM * x + myC);
            return false;// IsLocalXInFront(hitPoint.x);
        }
        else if(!opponent.isOnFloor)
        {
            float initialXDiff = opponent.GetLocalPhysicsPosition().x - self.GetLocalPhysicsPosition().x;
            float initialYDiff = opponent.GetLocalPhysicsPosition().y - self.GetLocalPhysicsPosition().y;
            float themVelY = opponent.velocity.y;
            float yVelDiff = opponent.velocity.y - self.GetAttackVector().y;
            float xVelDiff = opponent.velocity.x - self.GetAttackVector().x;
            float insideRoot = Mathf.Pow(yVelDiff, 2) /
                (0.25f * Mathf.Pow(PlayerCharacter.gravity.y, 2) * Mathf.Pow(themVelY, 2)) - 4 / (0.5f * PlayerCharacter.gravity.y * themVelY);
            if(insideRoot <0)
            {
                Debug.Log("Nodice");
                hitPoint = Vector2.zero;
                return false;
            }
            float one = yVelDiff / (0.5f * PlayerCharacter.gravity.y * themVelY) + 0.5f * Mathf.Sqrt(insideRoot);
            float two = yVelDiff / (0.5f * PlayerCharacter.gravity.y * themVelY) - 0.5f * Mathf.Sqrt(insideRoot);
            
            if(Mathf.Abs(initialXDiff /xVelDiff - one) < float.Epsilon)
            {
                Debug.Log("zoba");
                hitPoint = new Vector2(0, one);
                return true;
            }
            else if (Mathf.Abs(initialXDiff / xVelDiff - two) < float.Epsilon)
            {
                Debug.Log("ASdf");
                hitPoint = new Vector2(0, two);
                return true;
            } else
            {
                Debug.Log("One: " + one + ":: two:" + two + ":: "+ initialXDiff / xVelDiff);
            }

            hitPoint = Vector2.zero;

            return false;
        }
        hitPoint = Vector2.zero;

        return false;

        //opponent.velocity; PlayerCharacter.gravity; self.GetAttackVector();
    }

    float DiveKickUtility()
    {
        if (self.isOnFloor || self.dropping)
            return 0;


        return DirectAttackUtility();
    }

    float NormalisedMyVerticalDistance(float max)
    {
        return Mathf.Clamp01((self.GetLocalPhysicsPosition().y - self.floorHeight) / max);
    }

    float NormalisedOpponentVerticalDistance(float max)
    {
        return Mathf.Clamp01((opponent.GetLocalPhysicsPosition().y - opponent.floorHeight) / max);
    }


    float CalculateAttackHitPointUtility(Vector2 worldHitPoint)
    {
        float heightModifier = 1;
        float startModifier = 0;
        Vector2 hitPoint = (Vector2)transform.parent.InverseTransformPoint(worldHitPoint);
        DodgeArea areaCovered = opponent.isOnFloor || opponent.velocity.y > 0 ? DodgeArea.Upper : DodgeArea.Lower;
        switch (areaCovered)
        {
            case DodgeArea.Upper:
                heightModifier = 0.6f;
                startModifier = characterHeight * 0.5f;
                break;
            case DodgeArea.Lower:
                heightModifier = 0.6f;
                startModifier = characterHeight * .2f;
                break;
        }
        if (hitPoint.y > opponent.GetLocalPhysicsPosition().y + startModifier && hitPoint.y < opponent.GetLocalPhysicsPosition().y + startModifier + (characterHeight * heightModifier))
        {
            return 1;
        }

        return 0.25f;
    }

    float LikelyhoodOfAttackLanding()
    {
        return (1 - (NormalisedOpponentHorizontalDistance(5) * 0.4f)) *  (.5f+(NormalisedOpponentVerticalDistance(2)*.5f));
    }

    float DirectAttackUtility()
    {
        if (!MyAttackWillHit(out RaycastHit2D raycastHit2))
        {
            if (DebugMode)
            {
                Debug.Log("GonnaMiss");
            }
            return 0;
        }
        else
        {
            //float utility = CalculateAttackHitPointUtility(raycastHit2.point);
            float utility = LikelyhoodOfAttackLanding() * CalculateAttackHitPointUtility(raycastHit2.point) ;
            if(DebugMode)
            {
                Debug.Log("LikelyhoodOfAttack Utility: " + LikelyhoodOfAttackLanding() + "::utility: " + utility);
            }
            return utility;
        }

    }

    float NormalisedOpponentHorizontalDistance(float max)
    {
        return Mathf.Clamp01(Mathf.Abs(self.GetLocalPhysicsPosition().x - opponent.GetLocalPhysicsPosition().x) / max);
    }

    float ParabolicOpponentHorizontaldistance(float max)
    {
        float normalisedDistance = NormalisedOpponentHorizontalDistance(max);
        return Mathf.Clamp01(1 + 1.333333f * normalisedDistance - 22.66667f * Mathf.Pow(normalisedDistance, 2) + 42.66667f * Mathf.Pow(normalisedDistance, 3) - 21.33333f * Mathf.Pow(normalisedDistance, 4));
    }

    float GetOpponentOnFloorUtility(float isValue, float isntValue)
    {
        return opponent.isOnFloor ? isValue : isntValue;
    }

    bool IsOpponentAttackPositionInFront()
    {
        if(!opponent.dropping)
        {
            return false;
        }
        Vector2 position = CalculateWhereOpponentKickHitsFloor();

        return (Mathf.Sign(self.rigid.position.x - position.x) == Mathf.Sign(self.transform.position.x - opponent.rigid.position.x));

    }

    Vector2 CalculateOpponentAttackPosition()
    {
        var attackVector = opponent.GetAttackVector();
        Ray2D r = new Ray2D(opponentFoot.transform.position, attackVector);

        float angle = Mathf.Atan(attackVector.x / attackVector.y);

        float temp = Mathf.Abs(opponent.rigid.position.x-self.rigid.position.x) /Mathf.Cos(angle);

        return r.GetPoint(temp);
    }

    enum DodgeArea { Upper,Lower,All}
    float CalculateJumpDodgeUtility(DodgeArea areaCovered = DodgeArea.All)
    {
        if (!opponent.dropping)
        {
            return 0.25f;
        }
        Vector2 attackPoint;
        if(OpponentAttackWillHit(out RaycastHit2D raycastHit2))
        {
            attackPoint = raycastHit2.point;
        }
        else
        {
            attackPoint = CalculateOpponentAttackPosition();

        }
        attackPoint = (Vector2)transform.parent.InverseTransformPoint(attackPoint);
        float heightModifier = 1;
        float startModifier = 0;
        switch(areaCovered)
        {
            case DodgeArea.Lower:
                heightModifier = 0.5f;
                break;
            case DodgeArea.Upper:
                heightModifier = 0.5f;
                startModifier = characterHeight * heightModifier;
                break;
        }
        if(attackPoint.y > self.GetLocalPhysicsPosition().y + startModifier &&  attackPoint.y < self.GetLocalPhysicsPosition().y + startModifier + (characterHeight * heightModifier))
        {
            return 1;
        }

        return 0.25f;
    }

    private void OnDrawGizmos()
    {
        if (opponent == null)
            return;

        if(PredictOpponentPositionWhenAttacking(out Vector2 hitPoint))
        {
            hitPoint = transform.parent.TransformPoint(hitPoint);
            //Vector2 attackPoint = CalculateWhereOpponentKickHitsFloor();
            Gizmos.DrawSphere(hitPoint, 1);
        }
        
    }

    Vector2 CalculateWhereOpponentKickHitsFloor()
    {

        var attackVector = opponent.GetAttackVector();

        float ratio = attackVector.x / attackVector.y;
        float difference = opponent.rigid.position.y - opponent.floor.transform.position.y;


        float xDistance = ratio * difference;


        return new Vector2(opponent.rigid.position.x - xDistance, opponent.floor.transform.position.y);

    }

    float JumpUtility()
    {
        if (!self.isOnFloor)
            return 0;

        return ((IsOpponentAttackPositionInFront() ? 1:0) + CalculateJumpDodgeUtility(DodgeArea.Lower)) /2;

        //return 0;
    }

    float CalculateNormalisedBackDistanceToEdge()
    {
        float gameWidth = gameInstance.horizBorder * 2;
        float position = (self.GetLocalPhysicsPosition().x + gameInstance.horizBorder) / gameWidth;

        if(self.GetLocalPhysicsPosition().x > opponent.GetLocalPhysicsPosition().x)
        {
            return position;
        }


        return 1- position ;
    }

    float CalulateRetreatSpaceUtility()
    {
        float normalisedDistance = CalculateNormalisedBackDistanceToEdge();
        return Mathf.Clamp01(-0.02123093f + (0.95f - -0.02123093f) / (1 + Mathf.Pow(normalisedDistance / 0.8571179f, 24.65307f)));
        //return Mathf.Clamp01(1 - 1.1055563f * normalisedDistance - 6.605556f * Mathf.Pow(normalisedDistance, 2) +  14.2777f * Mathf.Pow(normalisedDistance, 3) - 21.33333f * Mathf.Pow(normalisedDistance, 4));
    }

    float JumpBackUtility() {
        if (!self.isOnFloor)
            return 0;

        return ((CalulateRetreatSpaceUtility() * 1.0f) + 0.0f) * (1-ParabolicOpponentHorizontaldistance(12) + 3*CalculateJumpDodgeUtility(DodgeArea.Upper))/4 ;
    }

    // Update is called once per frame
    public override void UpdateButtons()
    {
        Time.timeScale = 0.3f;
        List<UtilityOption> options = new List<UtilityOption>();
        options.Add(new UtilityOption() { actionType = Action.DiveKick, value = DiveKickUtility() });
        options.Add(new UtilityOption() { actionType = Action.Jump, value = JumpUtility() });
        options.Add(new UtilityOption() { actionType = Action.DoNothing, value = DoNothingUtility() });
        options.Add(new UtilityOption() { actionType = Action.JumpBack, value = JumpBackUtility() });

        options.Sort((a, b) => b.value.CompareTo(a.value));

        UtilityOption selected = options[0];

        switch(selected.actionType)
        {
            case Action.DiveKick:
            case Action.JumpBack:
                shouldDrop = false;
                shouldFeet = true;
                break;
            case Action.Jump:
                shouldDrop = true;
                shouldFeet = false;
                break;
            default:
                shouldDrop = false;
                shouldFeet = false;
                break;
        }

    }

}
