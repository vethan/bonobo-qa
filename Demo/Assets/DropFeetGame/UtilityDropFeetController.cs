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

        float opponentRelativeDistance = transform.InverseTransformPoint(opponent.rigid.position).x;
        float t = opponentRelativeDistance / (self.GetAttackVector().x - opponent.velocity.x);
        hitPoint = self.GetLocalPhysicsPosition() + t * self.GetAttackVector();
        
        if (opponent.isOnFloor)
        {
            bool hitPointValid = hitPoint.y > self.floorHeight
                && hitPoint.y  > opponent.GetLocalPhysicsPosition().y + 0.3f * characterHeight
                && hitPoint.y < opponent.GetLocalPhysicsPosition().y + characterHeight * .8f;
            return (hitPointValid && t > 0 && t < 0.2f);
        }
        else
        {
            Vector2 opponentHit = CalculateRelativeJumpCurvePoint(opponent, t);
            opponentHit = transform.parent.InverseTransformPoint(opponent.transform.TransformPoint(opponentHit));
            return opponentHit.y > self.floorHeight- 0.7f 
                && hitPoint.y > self.floorHeight  -0.7f
                && hitPoint.y > opponentHit.y + 0.2f * characterHeight
                && hitPoint.y <opponentHit.y + characterHeight* .8f;
        }
    }

    float DiveKickUtility()
    {
        if (self.isOnFloor || self.dropping)
            return 0;


        return PredictOpponentPositionWhenAttacking(out Vector2 hitPoint) ? 1 : 0;// DirectAttackUtility();
    }

    float NormalisedMyVerticalDistance(float max)
    {
        return Mathf.Clamp01((self.GetLocalPhysicsPosition().y - self.floorHeight) / max);
    }

    float NormalisedOpponentVerticalDistance(float max)
    {
        return Mathf.Clamp01((opponent.GetLocalPhysicsPosition().y - opponent.floorHeight) / max);
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


        Gizmos.DrawLine(transform.position, transform.position + (transform.parent.TransformVector(self.GetAttackVector()) * .02f* transform.lossyScale.x));



        if (opponent.isOnFloor)
            return;
        Vector2 prevPoint = opponent.transform.TransformPoint(CalculateRelativeJumpCurvePoint(opponent, ((0 - 5) * 0.05f)) + Vector2.up * characterHeight * 0.5f);
        for (int i = 1; i<11;i++)
        {
            Vector2 currPoint = opponent.transform.TransformPoint(CalculateRelativeJumpCurvePoint(opponent, ((i - 5) * 0.05f)) + Vector2.up * characterHeight * 0.5f);
            Gizmos.DrawLine(prevPoint, currPoint);
            prevPoint = currPoint;
        }
        float opponentRelativeDistance = transform.InverseTransformPoint(opponent.rigid.position).x;

        float intersectT = opponentRelativeDistance / (self.GetAttackVector().x - opponent.velocity.x); ;
        Gizmos.DrawSphere(transform.parent.TransformPoint(self.GetLocalPhysicsPosition() + intersectT * self.GetAttackVector()),1);

        
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

        return ((IsOpponentAttackPositionInFront() ? 1:0) + 2*CalculateJumpDodgeUtility(DodgeArea.Lower)) /3;

        return 1;
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

    Vector2 CalculateRelativeJumpCurvePoint(PlayerCharacter target, float relativeTime)
    {       
        float g = target.dropping? 0: -PlayerCharacter.gravity.y;
        float vx = target.velocity.x;
        float vy = target.velocity.y;

        float xDisplacement = relativeTime * vx;
        float yDisplacement = vy * relativeTime + .5f * g * relativeTime * relativeTime;

        return new Vector2(xDisplacement, yDisplacement);
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
