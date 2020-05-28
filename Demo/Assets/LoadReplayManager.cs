using Assets.DropFeetGame.Replays;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadReplayManager : MonoBehaviour
{
    public TextAsset[] replaysToLoad;
    public string[] replayNames;
    GameCreator creator;

    
    List<Replay> replays;

    public int focusIndex = 0;
    // Start is called before the first frame update
    void Awake()
    {
        replays = new List<Replay>();

        foreach(var replay in replaysToLoad)
        {
            replays.Add(Replay.ImportFromTextAsset(replay, Replay.SerializationStyle.ProtoBufNet));
        }        

        creator = FindObjectOfType<GameCreator>();
        creator.focusGameIndexOverride = FocusGame;
        creator.focusNextOverride = NextGame;
        creator.focusPrevOverride = PrevGame;
        creator.OnNewGeneration.AddListener(NewGen);
    }

    private void NewGen()
    {
        for (int i = 0; i < creator.gamesToCreate; i++)
        {
            AbstractGameInstance game = creator.GetGame(i);
            var player = game.GetComponent<ReplayPlayer>();
            player.LoadReplay(replays[i]);
        }
    }

    int FocusGame()
    {
        return focusIndex;
    }


    void NextGame()
    {
        focusIndex = (focusIndex + 1) % creator.gamesToCreate;
        AbstractGameInstance[] instances = FindObjectsOfType<AbstractGameInstance>();
        foreach (var instance in instances)
        {
            instance.FullReset();
        }
    }

    void PrevGame()
    {
        focusIndex = (creator.gamesToCreate + focusIndex - 1) % creator.gamesToCreate;

        AbstractGameInstance[] instances = FindObjectsOfType<AbstractGameInstance>();
        foreach (var instance in instances)
        {
            instance.FullReset();
        }
    }
}
