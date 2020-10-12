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
        float timer;
        bool justSelected = false;
        bool isEmptyJump = false;
        public RandomlyAdvanceAction(UtilityDropFeetController parent) : base(parent)
        {
        }

        void SetNewtimescale(float minSecs, float maxSecs)
        {
            timer = 0;
            timeScale = (Random.value * (maxSecs-minSecs)) + minSecs;
        }
        public override void BeginAction()
        {
            justSelected = true;
            SetNewtimescale(0.3f, 1.3f);
        }

        public override void EndAction()
        {
            SetNewtimescale(1, 5);
        }

        public override float GetStartUtility()
        {
            if(timeScale == -1)
            {
                SetNewtimescale(1, 4);
            }
            timer = Mathf.Min(timer + Time.deltaTime/timeScale,0.9f);
            isEmptyJump = Random.value > 0.5;
            return timer;
        }

        public override float GetContinueUtility()
        {

            return isEmptyJump || (!justSelected && parent.self.isOnFloor) ? 0 : parent.DoNothingUtility() + 0.03f;
        }


        public override void UpdateButtonStatus(out bool shouldJump, out bool shouldKick)
        {
            timer += Time.deltaTime;
            if (justSelected)
            {
                shouldJump = true;
                shouldKick = false;
                justSelected = false;
            }
            else if (!isEmptyJump && timer > timeScale)
            {
                shouldJump = false;
                shouldKick = true;
                isEmptyJump = true;
            }            
            else
            {
                shouldJump = false;
                shouldKick = false;
            }
        }
    }


}