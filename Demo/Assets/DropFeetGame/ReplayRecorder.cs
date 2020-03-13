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

    Replay currentReplay;
    float timer = 0;
    int sessionNumber = 0;
    void OnEnable()
    {
        if(gameInstance == null)
        {
            var instances = FindObjectsOfType<DropFeetGameInstance>();
            if(instances.Length > 1)
            {
                this.enabled = false;
                return;
            }
            gameInstance = instances[0];
        }
        InitialiseReplay();
    }

    private void OnDisable()
    {
        if (gameInstance == null)
            return;
        WriteReplay();
    }

    private void WriteReplay()
    {
        if(!Directory.Exists(Application.persistentDataPath))
        {
            Directory.CreateDirectory(Application.persistentDataPath);
        }

        String filename = String.Format("replay{0:yyyy-dd-M--HH-mm-ss}Session{1}", DateTime.Now, sessionNumber++);
        FileStream fileStream = File.Create(filename);

        BinaryFormatter formatter = new BinaryFormatter();
        SurrogateSelector surrogateSelector = new SurrogateSelector();
        Vector3SerializationSurrogate vector3SS = new Vector3SerializationSurrogate();

        surrogateSelector.AddSurrogate(typeof(Vector3), new StreamingContext(StreamingContextStates.All), vector3SS);
        formatter.SurrogateSelector = surrogateSelector;
        try
        {
            formatter.Serialize(fileStream, currentReplay);
        }
        catch (SerializationException e)
        {
            Console.WriteLine("Failed to serialize. Reason: " + e.Message);
            throw;
        }
        finally
        {
            fileStream.Close();
        }
    }

    public void InitialiseReplay()
    {
        currentReplay = new Replay();
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
    // Update is called once per frame
    void Update()
    {
        CreateEntry();
        timer += Time.unscaledDeltaTime;
        
    }

    private ReplayPlayerInfo GenerateInfo(PlayerCharacter player)
    {
        return new ReplayPlayerInfo() { dropping = player.dropping, onFloor = player.isOnFloor, position = player.transform.localPosition};
    }
}
