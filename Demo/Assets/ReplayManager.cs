using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
public class ReplayManager : MonoBehaviour
{
    ReplayMemoryStore memoryStore;
    // Start is called before the first frame update
    void Awake()
    {
        memoryStore = FindObjectOfType<ReplayMemoryStore>();
 
        //foreach(string path in replays)
        //{
        //    Debug.Log("Loaded Replay: " + path);
        //}
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
