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
    private int[] board = new int[9];
    private int fitness;
    public ChaserBrain player;
    public ChaserPickup originalPickup;

    private ChaserPickup redPickup;
    private ChaserPickup bluePickup;

    public override int InputCount
    {
        get { return 9; }
    }

    public override int OutputCount
    {
        get { return 9; }
    }

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        redPickup = originalPickup;
        bluePickup = Instantiate(originalPickup, transform);

        bluePickup.SetColor(Color.cyan);
        redPickup.SetColor(Color.red);

        bluePickup.OnCollision += BlueCollision;
        redPickup.OnCollision += RedCollision;
    }

    protected override string GetInputLabel(int index)
    {
        return GetOutputLabel(index - 1);
    }

    Vector2 GetRandomPosition()
    {
        return new Vector2(Random.Range(-horizBorder,horizBorder) * 0.9f,Random.Range(-vertBorder,vertBorder)* 0.9f);
        
    }
    
    void MovePickups()
    {

        bluePickup.WarpTo(GetRandomPosition());
        redPickup.WarpTo(GetRandomPosition());
    }
    
    void RedCollision(GameObject other)
    {
        if (other != player.gameObject)
            return;

        print("COLLIDE RED");
        MovePickups();
    }

    void BlueCollision(GameObject other)
    {
        if (other != player.gameObject)
            return;

        print("COLLIDE BLUE");
        MovePickups();
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

    private int playerTurn = 0;

    bool checkBoardFull()
    {
        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] == 0)
                return false;
        }

        return true;
    }

    void ResetBoard()
    {
        for (int i = 0; i < board.Length; i++)
        {
            board[i] = 0;
        }
    }

    private bool turnFlipper;

    private void FixedUpdate()
    {
        if (brain == null)
            return;
    }

    public override float CalculateFitness()
    {
        return fitness;
    }

    public override void FullReset()
    {
        fitness = 0;
        GameDone = false;
        ResetBoard();
    }

    public override void SetEvolvedBrain(IBlackBox blackBox, NeatGenome genome)
    {
        brain = blackBox;
        this.genome = genome;
    }

    //TODO: Generate some stats for Chaser
    public override Dictionary<string, float> GetGameStats()
    {
        return new Dictionary<string, float>();
    }

    // Update is called once per frame
    void Update()
    {
    }
}