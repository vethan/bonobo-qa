using System.Collections.Generic;
using System.Linq;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using TMPro;
using UnityEngine;

public class ChaserGameInstance : AbstractGameInstance
{
    private IBlackBox brain;
    private NeatGenome genome;
    public ChaserBrain player;
    public ChaserPickup originalPickup;

    private int timesCollected = 0;

    public override int InputCount
    {
        get { return 7; }
    }

    public override int OutputCount
    {
        get { return 4; }
    }

    // Start is called before the first frame update
    public override string GameName => "Chaser";

    protected override void Start()
    {
        base.Start();

        originalPickup.SetColor(Color.cyan);
        pseudoRandom = new System.Random(84902);

        originalPickup.OnCollision = BlueCollision;
    }

    protected override string GetInputLabel(int index)
    {
        return GetOutputLabel(index - 1);
    }

    Vector2 GetRandomPosition(bool isLeft)
    {
        return new Vector2((isLeft ? -1 : 1) * horizBorder * 0.7f,
            ((float)(pseudoRandom.NextDouble() * vertBorder * 2) - vertBorder) * 0.7f);
    }

    private bool leftSideSpawn = false;

    void MovePickups()
    {
        originalPickup.WarpTo(GetRandomPosition(leftSideSpawn));
    }


    void BlueCollision(GameObject other)
    {
        if (other != player.gameObject)
            return;
        timesCollected++;
        //print("COLLIDE BLUE: " + timesCollected);
        leftSideSpawn = !leftSideSpawn;
        MovePickups();
        player.SavePosition();
    }

    protected override string GetOutputLabel(int index)
    {
        switch (index)
        {
            case -1:
                return "Bias";
            case 0:
                return "Top Left";
            case 1:
                return "Top Middle";
            case 2:
                return "Top Right";
            case 3:
                return "Center Left";
            case 4:
                return "Center Middle";
            case 5:
                return "Center Right";
            case 6:
                return "Bottom Left";
            case 7:
                return "Bottom Middle";
            case 8:
                return "Bottom Right";
        }

        return "unknown";
    }


    private int timesCaptured;
    private System.Random pseudoRandom;

    private void FixedUpdate()
    {
        if (brain == null)
            return;
    }

    public override float CalculateFitness()
    {
        // Debug.Log(player.distanceAdded);
        
        return player.distanceAdded + (timesCollected * 7000) - (timesCaptured * 200);
    }

    public override void FullReset()
    {
        timesCollected = 0;
        timesCaptured = 0;
        player.Reset();
        player.myEnemy.Reset();
        leftSideSpawn = false;
        pseudoRandom = new System.Random(84902);

        originalPickup.WarpTo(GetRandomPosition(leftSideSpawn));
    }

    public override void SetEvolvedBrain(IBlackBox blackBox, NeatGenome genome)
    {
        brain = blackBox;
        this.genome = genome;
        player.SetBrain(brain);
    }

    //TODO: Generate some stats for Chaser
    public override Dictionary<string, float> GetGameStats()
    {
        var results =
            new Dictionary<string, float>()
            {
                { "Times Collected", timesCollected },
                { "Distance Measure", player.distanceAdded }
            };
        results.MergeInPlace(player.myEnemy.GetStats());
        return results;
    }

    // Update is called once per frame
    void Update()
    {
    }
}