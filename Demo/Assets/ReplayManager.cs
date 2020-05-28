using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
public class ReplayManager : MonoBehaviour
{
    ReplayMemoryStore memoryStore;
    GameCreator creator;


    int focusIndex = 0;
    // Start is called before the first frame update
    void Awake()
    {
        memoryStore = FindObjectOfType<ReplayMemoryStore>();
        creator = FindObjectOfType<GameCreator>();
        creator.focusGameIndexOverride = FocusGame;
        creator.focusNextOverride = NextGame;
        creator.focusPrevOverride = PrevGame;

        //foreach(string path in replays)
        //{
        //    Debug.Log("Loaded Replay: " + path);
        //}
    }

    int FocusGame()
    {
        return focusIndex;
    }


    void NextGame()
    {
        focusIndex = (focusIndex + 1) % creator.gamesToCreate;
    }

    void PrevGame()
    {
        focusIndex = (creator.gamesToCreate + focusIndex - 1) % creator.gamesToCreate;
    }

    public void UpdateReplays()
    {
        if (memoryStore == null)
            return;
        memoryStore.replays.Shuffle();
        var players = FindObjectsOfType<ReplayPlayer>();
        for (int i = 0; i < players.Length; i++)
        {
            players[i].LoadReplay(memoryStore.replays[i%memoryStore.replays.Count]);
        }
    }



    // Update is called once per frame
    void Update()
    {
        
    }
}
