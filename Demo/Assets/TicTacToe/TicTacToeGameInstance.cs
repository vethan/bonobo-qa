using System.Collections.Generic;
using System.Linq;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using TMPro;
using UnityEngine;

public class TicTacToeGameInstance : AbstractGameInstance
{
    public override string GameName => "TicTacToe";
    private IBlackBox brain;
    private NeatGenome genome;
    private int[] board = new int[9];
    private int fitness;
    private int gameCount;
    public TMPro.TMP_Text text;

    public override int InputCount
    {
        get { return 9; }
    }

    public override int OutputCount
    {
        get { return 9; }
    }
    
    protected override string GetInputLabel(int index)
    {
        return GetOutputLabel(index - 1);
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

    int CheckBoardForWin()
    {
        if (board[0] != 0 && ((board[0] == board[1] && board[0] == board[2]) ||
                              (board[0] == board[3] && board[0] == board[6])))
        {
            return board[0];
        }

        if (board[4] != 0 && ((board[4] == board[0] && board[4] == board[8]) ||
                              (board[4] == board[2] && board[4] == board[6]) ||
                              (board[4] == board[1] && board[4] == board[7]) ||
                              (board[4] == board[3] && board[4] == board[5])))
        {
            return board[4];
        }

        if (board[8] != 0 && ((board[8] == board[6] && board[8] == board[7]) ||
                              (board[8] == board[2] && board[8] == board[5])))
        {
            return board[8];
        }

        return 0;
    }

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
        int totalGames = 10;
        interesting = fitness == 100; 
        if (gameCount >= totalGames)
        {
            GameDone = fitness!=100;
            return;
        }

        ;
        int winState = CheckBoardForWin();
        if (winState != 0)
        {
            if (winState == 1)
            {
                fitness += 10;
            }

            gameCount++;
            if (gameCount < totalGames)
            {
                ResetBoard();
            }

            return;
        }

        bool isFull = checkBoardFull();
        if (isFull)
        {
            gameCount++;
            if (gameCount < totalGames)
            {
                ResetBoard();
            }

            fitness += 2;
            return;
        }

        if (turnFlipper)
        {
            AgentTurn();
        }
        else
        {
            SemiSmartTurn();
        }

        turnFlipper = !turnFlipper;
    }

    private void AgentTurn()
    {
        brain.InputSignalArray.CopyFrom(board.Select(x => (double) x).ToArray(), 0);
        brain.Activate();
        double highest = double.MinValue;
        int highestIndex = -1;
        for (int i = 0; i < OutputCount; i++)
        {
            if (board[i] != 0)
            {
                continue;
            }

            if (brain.OutputSignalArray[i] > highest)
            {
                highest = brain.OutputSignalArray[i];
                highestIndex = i;
            }
        }

        board[highestIndex] = 1;
    }

    private void RandomTurn()
    {
        int free = 0;
        foreach (int i in board)
        {
            if (i == 0)
                free++;
        }

        int chosen = Random.Range(0, free);
        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] == 0)
            {
                if (chosen == 0)
                {
                    board[i] = -1;
                    return;
                }

                chosen -= 1;
            }
        }
    }

    private int[,] winningLines = new int[8, 3]
    {
        {0, 1, 2},
        {0, 3, 6},
        {0, 4, 8},
        {3, 4, 5},
        {1, 4, 7},
        {2, 4, 6},
        {2, 5, 8},
        {6, 7, 8},
    };

    bool PlayInGameFor(int side)
    {
        for (int i = 0; i < 8; i++)
        {
            int rowTot = 0;
            int notFilled = -1;
            for (int j = 0; j < 3; j++)
            {
                if (board[winningLines[i, j]] == side)
                {
                    rowTot++;
                }
                else if (board[winningLines[i, j]] == 0)
                {
                    notFilled = j;
                }
            }

            if (rowTot == 2 && notFilled >= 0)
            {
                board[winningLines[i, notFilled]] = -1;
                return true;
            }
        }

        return false;
    }

    private bool PlayWinningMove()
    {
        return PlayInGameFor(-1);
    }

    private bool PlayDefensiveMove()
    {
        return PlayInGameFor(1);
    }

    private void SemiSmartTurn()
    {
        // Win if I can
        if (PlayWinningMove())
            return;

        //Stop opponent from winning
        if (PlayDefensiveMove())
            return;


        RandomTurn();
    }

    public override float CalculateFitness()
    {
        return fitness;
    }

    public override void FullReset()
    {
        fitness = 0;
        gameCount = 0;
        GameDone = false;
        ResetBoard();
    }

    public override void SetEvolvedBrain(IBlackBox blackBox, NeatGenome genome)
    {
        brain = blackBox;
        this.genome = genome;
    }

    //TODO: Generate some stats to TTT
    public override Dictionary<string, float> GetGameStats()
    {
        return new Dictionary<string, float>();
    }

    // Update is called once per frame
    void Update()
    {
        
        string boardString = "";
        for (int i = 0; i < 9; i++)
        {
            if (i % 3 == 0 && i != 0)
            {
                boardString += "\n";
            }

            if (board[i] == 0)
                boardString += "-";
            if (board[i] == 1)
                boardString += "X";
            if (board[i] == -1)
                boardString += "0";
        }

        text.text = boardString;
    }
}