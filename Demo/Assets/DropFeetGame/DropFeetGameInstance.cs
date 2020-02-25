using System.Collections;
using SharpNeat.Phenomes;
using UnityEngine;

public class DropFeetGameInstance : AbstractGameInstance
{
    public Transform leftScoreSprite;
    public Transform rightScoreSprite;
    public PlayerCharacter leftPlayer;
    public PlayerCharacter rightPlayer;
    public Transform floor;
    EvolvedDropFeetController evolvedPlayer;
    public override int InputCount { get { return 10; } }
    public override int OutputCount { get { return 2; } }

    int leftScore;
    int rightScore;
    
    Coroutine killCoroutine = null;

    void UpdateScoreImages()
    {
        Vector3 val = leftScoreSprite.transform.localScale;
        val.x = 0.25f * Mathf.Clamp(leftScore, 0, 20);
        leftScoreSprite.transform.localScale = val;

        val = rightScoreSprite.transform.localScale;
        val.x = 0.5f * Mathf.Clamp(rightScore, 0, 20);
        rightScoreSprite.transform.localScale = val;
        interesting = leftScore > rightScore;
    }

    // Start is called before the first frame update
    override protected void Awake()
    {
        base.Awake();
        evolvedPlayer = GetComponentInChildren<EvolvedDropFeetController>();
        leftPlayer.OnKill += HandleOnKill;
        rightPlayer.OnKill += HandleOnKill;
    }


    void ResetPositions()
    {
        leftPlayer.SnapToFloorPosition(-4);
        rightPlayer.SnapToFloorPosition(4);
        isPausedForKill = false;
    }
    public bool isPausedForKill  { get; private set;}
    
    IEnumerator PauseThenReset()
    {
        yield return new WaitForSeconds(0.5f);
        yield return new WaitForFixedUpdate();
        ResetPositions();
    }

    void HandleOnKill(PlayerCharacter scoringPlayer, PlayerCharacter.KillType killType)
    {
        if(isPausedForKill)
        {
            return;
        }
        switch(killType)
        {
            case PlayerCharacter.KillType.DoubleKill:
                //Debug.Log("Double Kill");
                ++leftScore;
                ++rightScore;
                break;
            case PlayerCharacter.KillType.Headshot:
                //Debug.Log("Headshot");
                if (scoringPlayer == leftPlayer)
                {
                    leftScore += 2;
                }
                else
                {
                    rightScore += 2;
                }
                break;
            case PlayerCharacter.KillType.Normal:
                //Debug.Log("Nice Kill");
                if (scoringPlayer == leftPlayer)
                {
                    ++leftScore;
                }
                else
                {
                    ++rightScore;
                }
                break;
        }
        isPausedForKill = true;
        UpdateScoreImages();
        killCoroutine = StartCoroutine(PauseThenReset());
    }

    protected override string GetOutputLabel(int index)
    {
        switch (index)
        {
            case 0:
                return "Jump";
            case 1:
                return "DiveKick/Hop Back";
        }
        return "Unknown";
    }

    protected override string GetInputLabel(int index)
    {
        switch (index)
        {
            case 0:
                return "Bias";
            case 1:
                return "Opponent X Direction";
            case 2:
                return "Opponent Y Direction";
            case 3:
                return "Opponent X Velocity";
            case 4:
                return "Opponent Y Velocity";
            case 5:
                return "Opponent Foot X";
            case 6:
                return "Opponent Foot Y";
            case 7:
                return "Opponent Diving";
            case 8:
                return "Self Diving";
            case 9:
                return "Opponent On Floor";
            case 10:
                return "Self On Floor";
        }
        return "Unknown";
    }

    public override float CalculateFitness()
    {
        float fit = 0;
        if(!evolvedPlayer.everDrop && !evolvedPlayer.everFeet)
        {
            fit = -10;
        }

        if((evolvedPlayer.everFeet && !evolvedPlayer.everDrop) || (!evolvedPlayer.everFeet && evolvedPlayer.everDrop))
        {
            fit = 5;
        }
        else if (evolvedPlayer.everDrop && evolvedPlayer.everFeet)
        {
            fit = 30;
        }
        
        return (2 * leftScore) - rightScore + fit;
    }

    public override void FullReset()
    {
        leftScore = 0;
        rightScore = 0;
        selected = false;
        UpdateScoreImages();
        if (killCoroutine != null)
            StopCoroutine(killCoroutine);
        evolvedPlayer.Reset();
        ResetPositions();
    }

    public override void SetEvolvedBrain(IBlackBox blackBox)
    {
        if (evolvedPlayer != null)
            evolvedPlayer.SetBrain(blackBox);
    }
}
