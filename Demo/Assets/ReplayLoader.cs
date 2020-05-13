using Assets.DropFeetGame.Replays;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ReplayLoader : MonoBehaviour
{
    public bool convert;

    public bool onlyFirst;

    ReplayMemoryStore memoryStore;
    List<string> replays = new List<string>();
    int importedIndex;
    public UnityEngine.UI.Text loaderText;
    // Start is called before the first frame update
    void Start()
    {
        memoryStore = FindObjectOfType<ReplayMemoryStore>();
        GameObject.DontDestroyOnLoad(memoryStore.gameObject);
        string replayBasePath = Path.Combine(Application.streamingAssetsPath, "Replays");
        GetReplayPaths(replayBasePath);
    }

    void GetReplayPaths(string directory)
    {
        foreach (string s in Directory.GetFiles(directory))
        {
            if (s.EndsWith(".meta"))
                continue;

            replays.Add(s);
        }

        foreach (string subDirectory in Directory.GetDirectories(directory))
        {
            GetReplayPaths(subDirectory);
        }
    }
    // Update is called once per frame
    void Update()
    {
        if(importedIndex < replays.Count && !(onlyFirst && importedIndex > 0))
        {
            try
            {
                if (convert)
                {
                    Replay replay = Replay.ImportFromFile(replays[importedIndex], Replay.SerializationStyle.DotNet);
                    replay.Save("NEW" + Path.GetFileName(replays[importedIndex]), Replay.SerializationStyle.ProtoBufNet);
                }
                else
                {
                    memoryStore.replays.Add(Replay.ImportFromFile(replays[importedIndex], Replay.SerializationStyle.ProtoBufNet));
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
            }
            finally
            {
                importedIndex++;
                loaderText.text = importedIndex + "/" + replays.Count;
            }
            
        }
        if(importedIndex == replays.Count)
        {
            this.enabled = false;
            UnityEngine.SceneManagement.SceneManager.LoadScene("ReplayGrid");
        }
    }
}
