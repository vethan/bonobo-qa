using System.Collections;
using System.Collections.Generic;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using UnityEngine;

public class DropFeetGameInstance : AbstractGameInstance
{
    public override string GameName => "DropFeet";
    public System.Action OnNewRound = () => { };
    public Transform leftScoreSprite;
    public Transform rightScoreSprite;
    public PlayerCharacter leftPlayer;
    public PlayerCharacter rightPlayer;
    public Transform floor;
    EvolvedDropFeetController evolvedPlayer;

    public override int InputCount
    {
        get { return 7; }
    }

    public override int OutputCount
    {
        get { return 2; }
    }

    public int leftScore { get; private set; }
    public int rightScore { get; private set; }

    Coroutine killCoroutine = null;
    public float fitness;

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
        Application.targetFrameRate = 60;
        base.Awake();
        evolvedPlayer = GetComponentInChildren<EvolvedDropFeetController>();
        _isevolvedPlayerNull = evolvedPlayer == null;
        leftPlayer.OnKill += HandleOnKill;
        rightPlayer.OnKill += HandleOnKill;
    }


    void ResetPositions()
    {
        leftPlayer.SnapToFloorPosition(-4);
        rightPlayer.SnapToFloorPosition(4);
        isPausedForKill = false;
        OnNewRound();
    }

    public bool isPausedForKill { get; private set; }

    IEnumerator PauseThenReset()
    {
        yield return new WaitForSeconds(0.5f);
        yield return new WaitForFixedUpdate();
        ResetPositions();
    }

    void HandleOnKill(PlayerCharacter scoringPlayer, PlayerCharacter.KillType killType)
    {
        if (isPausedForKill)
        {
            return;
        }

        switch (killType)
        {
            case PlayerCharacter.KillType.DoubleKill:
                leftPlayer.kicksHit++;
                rightPlayer.kicksHit++;
                //Debug.Log("Double Kill");
                ++leftScore;
                ++rightScore;
                break;
            case PlayerCharacter.KillType.Headshot:
                //Debug.Log("Headshot");
                scoringPlayer.kicksHit++;
                scoringPlayer.headshotsHit++;

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
                scoringPlayer.kicksHit++;
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
                return "Opponent Diving";
            case 4:
                return "Self Diving";
            case 5:
                return "Opponent On Floor";
            case 6:
                return "Self On Floor";
            case 7:
                return "Y position";
        }

        return "Unknown";
    }

    private void FixedUpdate()
    {
        rightEverDrop |= rightPlayer.dropping;
        rightEverFeet |= !rightPlayer.dropping && !rightPlayer.isOnFloor;
    }

    public override Dictionary<string, float> GetGameStats()
    {
        Dictionary<string, float> results = new Dictionary<string, float>()
        {
            {"leftScore", leftScore},
            {"rightScore", rightScore}
        };

        results.MergeInPlace(rightPlayer.GetPlayerStats("right")).MergeInPlace(leftPlayer.GetPlayerStats("left"));
        return results;
    }

    public override float CalculateFitness()
    {
        if (_isevolvedPlayerNull)
            return 0;
        float fit = 0;

        if (!evolvedPlayer.everDrop && !evolvedPlayer.everFeet)
        {
            fit = -10;
        }
        else if ((evolvedPlayer.everFeet && !evolvedPlayer.everDrop) ||
                 (!evolvedPlayer.everFeet && evolvedPlayer.everDrop))
        {
            fit = 5;
        }
        else if (evolvedPlayer.everDrop && evolvedPlayer.everFeet)
        {
            fit = 30;
        }

        /*

        if (!rightEverDrop && !rightEverFeet)
        {
            fit += -10;
        }
        else if ((rightEverDrop && !rightEverFeet) || (!rightEverDrop && rightEverFeet))
        {
            fit += 5;
        }
        else if (rightEverDrop && rightEverFeet)
        {
            fit += 30;
        }
        if(leftScore > 0)
        {
            fit += 500;
        }
        if(leftScore > rightScore)
        {
            fit += 500;
        }*/
        return 200 + ((10 * leftScore) - rightScore * 1 + fit);
    }

    protected override void Update()
    {
        base.Update();
        fitness = CalculateFitness();
    }

    public bool rightEverDrop;
    public bool rightEverFeet;

    public override void FullReset()
    {
        leftScore = 0;
        rightScore = 0;
        rightEverDrop = false;
        rightEverFeet = false;
        selected = false;
        UpdateScoreImages();
        if (killCoroutine != null)
            StopCoroutine(killCoroutine);
        evolvedPlayer.Reset();
        rightPlayer.ResetStats();
        leftPlayer.ResetStats();
        ResetPositions();
        var utilController = leftPlayer.GetComponent<UtilityDropFeetController>();
        if (utilController != null)
        {
            utilController.ResetRandomness();
        }

        utilController = rightPlayer.GetComponent<UtilityDropFeetController>();
        if (utilController != null)
        {
            utilController.ResetRandomness();
        }
    }

    public NeatGenome genome = null;
    private bool _isevolvedPlayerNull;

    public override void SetEvolvedBrain(IBlackBox blackBox, NeatGenome genome)
    {
        this.genome = genome;
        if (!_isevolvedPlayerNull)
            evolvedPlayer.SetBrain(blackBox);
    }
}