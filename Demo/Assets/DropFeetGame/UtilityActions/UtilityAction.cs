using UnityEngine;

public partial class UtilityDropFeetController : AuthoredAIDropFeetController
{
    abstract class UtilityAction
    {
        protected UtilityDropFeetController parent;

        public UtilityAction(UtilityDropFeetController parent)
        {
            this.parent = parent;
        }

        public abstract float GetStartUtility();

        public virtual float GetContinueUtility()
        {
            return GetStartUtility();
        }

        public virtual void BeginAction()
        {

        }

        public virtual void EndAction()
        {

        }

        public abstract void UpdateButtonStatus(out bool shouldJump, out bool shouldKick);
    }

    class DoNothingAction : UtilityAction
    {

        public DoNothingAction(UtilityDropFeetController parent) : base(parent)
        {
        }

        public override float GetStartUtility()
        {
            return parent.DoNothingUtility();
        }

        public override void UpdateButtonStatus(out bool shouldJump, out bool shouldKick)
        {
            shouldJump = false;
            shouldKick = false;
        }
    }

    class DivekickAction : UtilityAction
    {

        public DivekickAction(UtilityDropFeetController parent) : base(parent)
        {
        }

        public override float GetStartUtility()
        {
            return parent.DiveKickUtility();
        }

        public override void UpdateButtonStatus(out bool shouldJump, out bool shouldKick)
        {
            shouldJump = false;
            shouldKick = true;
        }
    }


    class JumpAction : UtilityAction
    {

        public JumpAction(UtilityDropFeetController parent) : base(parent)
        {
        }

        public override float GetStartUtility()
        {
            return parent.JumpUtility();
        }

        public override void UpdateButtonStatus(out bool shouldJump, out bool shouldKick)
        {
            shouldJump = true;
            shouldKick = false;
        }
    }


    class JumpBackAction : UtilityAction
    {

        public JumpBackAction(UtilityDropFeetController parent) : base(parent)
        {
        }

        public override float GetStartUtility()
        {
            return parent.JumpBackUtility();
        }

        public override void UpdateButtonStatus(out bool shouldJump, out bool shouldKick)
        {
            shouldJump = false;
            shouldKick = true;
        }
    }

    class RandomlyAdvanceAction : UtilityAction
    {
        float timeScale = -1;
        bool justSelected = false;
        bool isEmptyJump = false;
        float jumpTargetHeightDiff = 0;
        const float maxHeight = 1.342005f;
        const float minHeight = -4.489993f;
        float maxRandomAttackHeight = (maxHeight-minHeight);// *.4f;
        
        public RandomlyAdvanceAction(UtilityDropFeetController parent) : base(parent)
        {
        }

        void SetNewtimescale(float minSecs, float maxSecs)
        {
            timeScale = ((float)parent.pseudoRandom.NextDouble() * (maxSecs-minSecs)) + minSecs;
        }
        public override void BeginAction()
        {
            justSelected = true;
            isEmptyJump = parent.pseudoRandom.NextDouble() > 0.75;
            jumpTargetHeightDiff = Mathf.Lerp(-maxRandomAttackHeight, maxRandomAttackHeight, (float)parent.pseudoRandom.NextDouble());
            //Debug.Log("Target Height: " + jumpTargetHeightDiff);
        }

        public override void EndAction()
        {
            SetNewtimescale(.3f, 3);
        }

        public override float GetStartUtility()
        {
            if(timeScale == -1)
            {
                EndAction();
            }
             
            return Mathf.Min(parent.self.lastInAir / timeScale, 0.9f);
        }

        public override float GetContinueUtility()
        {
            return isEmptyJump || parent.self.dropping || (!justSelected && parent.self.isOnFloor) ? 0 : parent.DoNothingUtility() + 0.03f;
        }


        public override void UpdateButtonStatus(out bool shouldJump, out bool shouldKick)
        {
            bool attackDistanceReached = ((jumpTargetHeightDiff >= 0 && parent.self.GetLocalPhysicsPosition().y > minHeight+ jumpTargetHeightDiff) ||
                (jumpTargetHeightDiff < 0 && parent.self.velocity.y < 0 && parent.self.GetLocalPhysicsPosition().y < minHeight+ Mathf.Abs(jumpTargetHeightDiff)));

            if (justSelected)
            {
                shouldJump = true;
                shouldKick = false;
                justSelected = false;
            }
            else if (!isEmptyJump && attackDistanceReached)
            {
                shouldJump = false;
                shouldKick = true;
            }            
            else
            {
                shouldJump = false;
                shouldKick = false;
            }
        }
    }


}