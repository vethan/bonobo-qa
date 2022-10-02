using Assets.DropFeetGame.Replays;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

public class ReplayRecorder : MonoBehaviour
{
    // Start is called before the first frame update
    public DropFeetGameInstance gameInstance;
    public bool autoEndReplay = false;
    Replay currentReplay;
    public int secondPerSession = 15;
    float timer = 0;
    int sessionNumber = 0;
    DateTime sessionStartTime;
    public string filePrefix = "";
    static float replayTargetFPS = 60;
    float replayMaxTick = 1.0f / replayTargetFPS;
    private void Awake()
    {
        sessionStartTime = DateTime.Now;
    }

    void OnEnable()
    {
        
        if(gameInstance == null)
        {
            var instances = FindObjectsOfType<DropFeetGameInstance>();
            if(instances.Length == 1)
            {
                gameInstance = instances[0];
            }
            else
            {
                gameInstance = GetComponent<DropFeetGameInstance>();
            }

            if(gameInstance == null)
            {
                this.enabled = false;
                return;
            }

            gameInstance.OnNewRound += NewRoundHappened;
        }
        InitialiseReplay();
    }

    private void NewRoundHappened()
    {
        if (timer < secondPerSession)
            return;


        WriteReplay();
        InitialiseReplay(gameInstance.leftScore, gameInstance.rightScore);
    }

    private void OnDisable()
    {
        if (gameInstance == null)
            return;
        WriteReplay();
        InitialiseReplay(gameInstance.leftScore,gameInstance.rightScore);
    }

    public Stream GetReplayStream()
    {
        var memoryStream = new MemoryStream();
        currentReplay.Save(memoryStream);
        memoryStream.Seek(0,SeekOrigin.Begin);
        return memoryStream;
    }
    private void WriteReplay()
    {
        String filename = String.Format(filePrefix+"replay{0:yyyy-dd-M--HH-mm-ss}Session{1}.bytes", sessionStartTime, sessionNumber++);
        currentReplay.Save(filename);
    }

    public void InitialiseReplay(int leftStartScore = 0, int rightStartScore = 0)
    {
        currentReplay = new Replay(leftStartScore, rightStartScore);
        timer = 0;
    }

    public void CreateEntry()
    {
        ReplayPlayerInfo leftInfo = GenerateInfo(gameInstance.leftPlayer);
        ReplayPlayerInfo rightInfo = GenerateInfo(gameInstance.rightPlayer);

        ReplayEntry replayEntry = new ReplayEntry()
        {
            leftPlayerData = leftInfo,
            rightPlayerData = rightInfo,
            time = timer,
            leftScore = gameInstance.leftScore,
            rightScore = gameInstance.rightScore
        };
        currentReplay.entries.Enqueue(replayEntry);
    }

    float framerateTimer;
    // Update is called once per frame
    void Update()
    {
        framerateTimer += Time.unscaledDeltaTime;
        if(framerateTimer > replayMaxTick)
        {
            CreateEntry();
            framerateTimer -= replayMaxTick;
        }
        
        timer += Time.unscaledDeltaTime;
        
    }

    private ReplayPlayerInfo GenerateInfo(PlayerCharacter player)
    {
        return new ReplayPlayerInfo() { dropping = player.dropping, onFloor = player.isOnFloor, position = player.transform.localPosition};
    }
}
