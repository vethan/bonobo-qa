using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UtilityDropFeetController : AuthoredAIDropFeetController
{
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

    float DirectAttackUtility()
    {
        if (!MyAttackWillHit())
            return 0;

        else
            return 1 - (NormalisedOpponentHorizontalDistance(12) * 0.5f);

    }

    float NormalisedOpponentHorizontalDistance(float max)
    {
        return Mathf.Clamp01((self.GetLocalPhysicsPosition().x - opponent.GetLocalPhysicsPosition().x) / max);
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

        float temp = Mathf.Abs(opponent.GetLocalPhysicsPosition().x-self.GetLocalPhysicsPosition().x) /Mathf.Cos(angle);

        return r.GetPoint(temp);
    }

    float CalculateJumpDodgeUtility()
    {
        if (!opponent.dropping)
        {
            return 0.25f;
        }

        Vector2 attackPoint = CalculateOpponentAttackPosition();
        if(attackPoint.y > self.GetLocalPhysicsPosition().y &&  attackPoint.y < self.GetLocalPhysicsPosition().y + characterHeight)
        {
            return 1;
        }

        return 0.25f;
    }

    float JumpUtility()
    {
        if (!self.isOnFloor)
            return 0;

        return (ParabolicOpponentHorizontaldistance(12) + 1 * CalculateJumpDodgeUtility()) / 2;

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

        return ((CalulateRetreatSpaceUtility() * 1.0f) + 0.0f) * (1-ParabolicOpponentHorizontaldistance(12) + 3*CalculateJumpDodgeUtility())/4 ;
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
