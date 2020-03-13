using Assets.DropFeetGame.Replays;
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

    public string replayFilePath;
    Replay replay;
    float timer;

    public override int InputCount { get { return 10; } }
    public override int OutputCount { get { return 2; } }

    // Start is called before the first frame update
    override protected void Awake()
    {
        LoadReplay(replayFilePath);
    }

    public void LoadReplay(string filePath)
    {
        var file = File.Open(filePath, FileMode.Open);
        BinaryFormatter formatter = new BinaryFormatter();
        SurrogateSelector surrogateSelector = new SurrogateSelector();
        Vector3SerializationSurrogate vector3SS = new Vector3SerializationSurrogate();

        surrogateSelector.AddSurrogate(typeof(Vector3), new StreamingContext(StreamingContextStates.All), vector3SS);
        formatter.SurrogateSelector = surrogateSelector;
        
        try
        {
            replay = formatter.Deserialize(file) as Replay;
            timer = 0;
        }
        catch (SerializationException e)
        {
            Debug.Log("Failed to deserialize. Reason: " + e.Message);
            throw;
        }
        finally
        {
            file.Close();
        }
    }

    void SetPlayer(ReplayCharacter character, ReplayPlayerInfo info)
    {
        character.dropping = info.dropping;
        character.isOnFloor = info.onFloor;
        character.transform.localPosition = info.position;
    }

    // Update is called once per frame
    void Update()
    {

        if(replay != null && replay.entries.Count > 0)
        {
            
            var entry = replay.entries.Peek();
            if (timer > entry.time)
            {
                //Debug.Log("Trying");
                replay.entries.Dequeue();
                //Update Score
                Vector3 val = leftScoreSprite.transform.localScale;
                val.x = 0.25f * Mathf.Clamp(entry.leftScore, 0, 20);
                leftScoreSprite.transform.localScale = val;

                val = rightScoreSprite.transform.localScale;
                val.x = 0.5f * Mathf.Clamp(entry.rightScore, 0, 20);
                rightScoreSprite.transform.localScale = val;
                interesting = entry.leftScore > entry.rightScore;

                SetPlayer(leftPlayer, entry.leftPlayerData);
                SetPlayer(rightPlayer, entry.rightPlayerData);

                
            }
        }
        timer += Time.deltaTime;
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

    public override void SetEvolvedBrain(IBlackBox blackBox)
    {

    }
}
