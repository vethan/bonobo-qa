using Assets.DropFeetGame.Replays;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public class ReplayAnalysis
{
    struct ReplayStat
    {
        public string fileName;
        public int jumpCount ;
        public int diveCount ;
        public int hopCount;
        public float distanceMovedAwayOpponent ;
        public float distanceMovedTowardsOpponent ;
        public float distanceMovedUp ;
        public int leftScore;
        public int rightScore;
        public float length;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    [MenuItem("DropFoot/Analyse Single Replay")]
    static void AnalyseSingleReplay() {
        string path = EditorUtility.OpenFilePanel("Select Replay", "", "bytes");
        var replay = Replay.ImportFromFile(path, Replay.SerializationStyle.ProtoBufNet);
        AnalyseReplay(replay);
    }


    [MenuItem("DropFoot/Analyse Replay Folder")]
    static void AnalyseReplayFolder()
    {

        string path = LoadGenomeManagerEditor.AssetsRelativePath(EditorUtility.OpenFolderPanel("Select Replay Folder", "", ""));
        List<ReplayStat> stats = new List<ReplayStat>();
        if (path.Length != 0)
        {
            List<TextAsset> xmlFiles = new List<TextAsset>();
            var assets = AssetDatabase.FindAssets("t:TextAsset", new[] { path });
            foreach (var asset in assets)
            {
                if (!AssetDatabase.GUIDToAssetPath(asset).EndsWith("bytes"))
                {
                    continue;
                }
                var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(asset));
                var replay = Replay.ImportFromTextAsset(textAsset, Replay.SerializationStyle.ProtoBufNet);
                var result = AnalyseReplay(replay, false);
                result.fileName = AssetDatabase.GUIDToAssetPath(asset);
                stats.Add(result);
            }
        }
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("File Path, length, jumps, hops, dives, movetoward, moveaway, moveup, leftscore, right score");

        foreach (var replayStat in stats)
        {
            builder.AppendFormat("{0},{1},{2},{3},{4},{5},{6},{7},{8}, {9}\n", replayStat.fileName,
                replayStat.length,
                replayStat.jumpCount,
                replayStat.hopCount,
                replayStat.diveCount,
                replayStat.distanceMovedTowardsOpponent,
                replayStat.distanceMovedAwayOpponent,
                replayStat.distanceMovedUp,
                replayStat.leftScore,
                replayStat.rightScore);
        }

        path = EditorUtility.SaveFilePanel("Save stats File", "", "", "csv");

        if (path.Length != 0)
        {
            using (StreamWriter s = new StreamWriter(path, false))
                s.Write(builder.ToString());
        }
    }


    static ReplayStat AnalyseReplay(Replay replay, bool printData = true)
    {
        ReplayStat replayStat = new ReplayStat();
        bool first = true;
        bool scoreJustChanged = false;
        ReplayEntry previousEntry = new ReplayEntry();
        
        foreach (var entry in replay.entries)
        {
            replayStat.length = entry.time;
            if (first)
            {
                previousEntry = entry;
                first = false;
                continue;
            }
            Vector3 distanceChange = entry.leftPlayerData.position - previousEntry.leftPlayerData.position;

            if (distanceChange.sqrMagnitude > 0.1 && scoreJustChanged)
            {
                //Debug.Log("ScoreResetHappened");
                previousEntry = entry;
                scoreJustChanged = false;
                continue;
            }
            scoreJustChanged |= previousEntry.leftScore != entry.leftScore || previousEntry.rightScore != entry.rightScore;
            


            if(distanceChange.y > 0)
            {
                replayStat.distanceMovedUp += distanceChange.y;
            }

            float directionToOpponent = Mathf.Sign(entry.rightPlayerData.position.x - entry.leftPlayerData.position.x);
            if(Mathf.Sign( distanceChange.x) == directionToOpponent)
            {
                replayStat.distanceMovedTowardsOpponent += Mathf.Abs(distanceChange.x);
            }
            else
            {
                replayStat.distanceMovedAwayOpponent += Mathf.Abs(distanceChange.x);
            }
            if(entry.leftPlayerData.dropping && !previousEntry.leftPlayerData.dropping)
            {
                replayStat.diveCount++;
            }
            if(!entry.leftPlayerData.onFloor && previousEntry.leftPlayerData.onFloor)
            {
                if(Mathf.Abs(distanceChange.x) > 0)
                {
                    replayStat.hopCount++;
                }
                else
                {
                    replayStat.jumpCount++;
                }
            }

            previousEntry = entry;
            
        }
        replayStat.leftScore = (previousEntry.leftScore - replay.leftStartScore);
        replayStat.rightScore = (previousEntry.rightScore - replay.rightStartScore);
        if (printData)
        {
            Debug.Log("Final Score: " + replayStat.leftScore + "::" + replayStat.rightScore);
            Debug.Log("HopCount: " + replayStat.hopCount + ". JumpCount: " + replayStat.jumpCount + ". DiveCount: " + replayStat.diveCount);
            Debug.Log("MoveToward: " + replayStat.distanceMovedTowardsOpponent + ". MoveAway: " + replayStat.distanceMovedAwayOpponent + ". Move up: " + replayStat.distanceMovedUp);
        }
        return replayStat;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
