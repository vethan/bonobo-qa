using Assets.DropFeetGame.Replays;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class ReplayPlayer : AbstractGameInstance
{
    public Transform leftScoreSprite;
    public Transform rightScoreSprite;
    public ReplayCharacter leftPlayer;
    public ReplayCharacter rightPlayer;

    public Replay.SerializationStyle serializationStyle = Replay.SerializationStyle.ProtoBufNet;
    public string replayFilePath;
    Replay replay;
    float timer;

    public override int InputCount { get { return 10; } }
    public override int OutputCount { get { return 2; } }
    public bool autoLoad = false;

    // Start is called before the first frame update
    override protected void Awake()
    {
        try
        {
            base.Awake();
            if (autoLoad)
                LoadReplay(replayFilePath, serializationStyle);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Awake Failed", this);
        }
    }

    

    public void LoadReplay(string filePath, Replay.SerializationStyle serializationStyle)
    {
        LoadReplay(Replay.ImportFromFile(filePath, serializationStyle));
    }

    public void LoadReplay(Replay r)
    {
        replay = r.Clone();
        timer = 0;
    }

    void SetPlayer(ReplayCharacter character, ReplayPlayerInfo previousInfo ,ReplayPlayerInfo info, float t)
    {
        character.dropping = previousInfo.dropping;
        character.isOnFloor = previousInfo.onFloor;
        character.transform.localPosition = Vector3.Lerp(previousInfo.position, info.position,t);
    }
    ReplayEntry previousEntry;
    // Update is called once per frame
    override protected void Update()
    {
        base.Update();
        if(replay != null && replay.entries.Count > 1)
        {            
            var entry = replay.entries.Peek();
            while (replay.entries.Count > 1 && timer > entry.time)
            {
                //Debug.Log("Trying");
                replay.entries.Dequeue();
                //Update Score
                Vector3 val = leftScoreSprite.transform.localScale;
                val.x = 0.25f * Mathf.Clamp(entry.leftScore - replay.leftStartScore, 0, 20);
                leftScoreSprite.transform.localScale = val;

                val = rightScoreSprite.transform.localScale;
                val.x = 0.5f * Mathf.Clamp(entry.rightScore - replay.rightStartScore, 0, 20);
                rightScoreSprite.transform.localScale = val;
                interesting = entry.leftScore - replay.leftStartScore > entry.rightScore - replay.rightStartScore;


                previousEntry = entry;
                entry = replay.entries.Peek();
            }

            float t = timer - previousEntry.time / (entry.time - previousEntry.time);

            if(float.IsNaN(t) || float.IsInfinity(t))
            {
                t = 0;
            }
            SetPlayer(leftPlayer, previousEntry.leftPlayerData, entry.leftPlayerData,t);
            SetPlayer(rightPlayer, previousEntry.rightPlayerData, entry.rightPlayerData,t);
        }
        timer += Time.deltaTime;
    }

    internal override void SetGraph(Graph graph)
    {
        
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

    public override float CalculateFitness()
    {
        return 5;
    }

    public override void FullReset()
    {
        //TODO: Change the loaded replays to random new ones;
    }

    public override void SetEvolvedBrain(IBlackBox blackBox, NeatGenome a)
    {

    }
}
