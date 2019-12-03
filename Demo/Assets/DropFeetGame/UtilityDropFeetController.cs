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

    float CalculateAttackHitPointUtility(Vector2 worldHitPoint)
    {
        float heightModifier = 1;
        float startModifier = 0;
        Vector2 hitPoint = (Vector2)transform.parent.InverseTransformPoint(worldHitPoint);
        DodgeArea areaCovered = opponent.isOnFloor || opponent.velocity.y > 0 || opponent.dropping ? DodgeArea.Upper : DodgeArea.Lower;
        switch (areaCovered)
        {
            case DodgeArea.Upper:
                heightModifier = 0.6f;
                startModifier = characterHeight * 0.5f;
                break;
            case DodgeArea.Lower:
                heightModifier = 0.6f;
                startModifier =  0;
                break;
        }
        if (hitPoint.y > opponent.GetLocalPhysicsPosition().y + startModifier && hitPoint.y < opponent.GetLocalPhysicsPosition().y + startModifier + (characterHeight * heightModifier))
        {
            return 1;
        }

        return 0.25f;
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
            float utility = CalculateAttackHitPointUtility(raycastHit2.point);
            //float utility = ((1 - (NormalisedOpponentHorizontalDistance(8) * 0.6f)) + 2* CalculateAttackHitPointUtility(raycastHit2.point)) /3;
            if(DebugMode)
            {
                Debug.Log("Attack Utility: " + utility);
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

        Vector2 attackPoint = CalculateOpponentAttackPosition();
        Gizmos.DrawSphere(attackPoint, 20);
    }

    float JumpUtility()
    {
        if (!self.isOnFloor)
            return 0;

        return (ParabolicOpponentHorizontaldistance(12) + 2 * CalculateJumpDodgeUtility(DodgeArea.Lower)) / 3;

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
