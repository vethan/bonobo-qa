using Assets.DropFeetGame.Replays;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine.WSA;

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
        public int segmentID;
        
        public static ReplayStat operator +(ReplayStat a, ReplayStat b)
            => new ReplayStat() {fileName = a.fileName,
                jumpCount =  a.jumpCount + b.jumpCount,
                diveCount = a.diveCount +b.diveCount,
                hopCount = a.hopCount+b.hopCount,
                distanceMovedAwayOpponent =  a.distanceMovedAwayOpponent+b.distanceMovedAwayOpponent,
                distanceMovedTowardsOpponent = a.distanceMovedTowardsOpponent+b.distanceMovedTowardsOpponent,
                distanceMovedUp = a.distanceMovedUp + b.distanceMovedUp,
                segmentID = a.segmentID,
                length =  a.length + b.length,
                leftScore =  a.leftScore + b.leftScore,
                rightScore =  a.rightScore + b.rightScore
            };
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    [MenuItem("DropFoot/Standard/Analyse Single Replay")]
    static void AnalyseSingleReplay() {
        string path = EditorUtility.OpenFilePanel("Select Replay", "", "bytes");
        var replay = Replay.ImportFromFile(path, Replay.SerializationStyle.ProtoBufNet);
        AnalyseReplay(replay);
    }

    [MenuItem("DropFoot/Variance/Analyse Replay Folder Group")]
    static void AnalyseReplayFolderVarianceGroup()
    {
        string path = LoadGenomeManagerEditor.AssetsRelativePath(EditorUtility.OpenFolderPanel("Select Root Replay Folder", "", ""));
        var subfolders = AssetDatabase.GetSubFolders(path);
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("FilePath,segment,Type, length, jumps, hops, dives, movetoward, moveaway, moveup, leftscore, rightscore");
        foreach (var subfold in subfolders)
        {
            var stats = AnalyseForVarianceList(subfold);
            var foldername = new DirectoryInfo(subfold).Name;
            foreach (var replayStat in stats)
            {
                builder.AppendFormat("{0},{1},{2},{3},{4},{5},{6},{7},{8}, {9}, {10},{11}\n", 
                    replayStat.fileName,
                    replayStat.segmentID,
                    foldername,
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
        }
        path = EditorUtility.SaveFilePanel("Save stats File", "", "", "csv");

        if (path.Length != 0)
        {
            using (StreamWriter s = new StreamWriter(path, false))
                s.Write(builder.ToString());
        }
    }

    public const int SEGMENT_COUNT = 5;
    
        [MenuItem("DropFoot/Variance/Analyse Replay Folder")]
        static void AnalyseReplayFolderVariance()
    {

        string path = LoadGenomeManagerEditor.AssetsRelativePath(EditorUtility.OpenFolderPanel("Select Replay Folder", "", ""));

        var stats = AnalyseForVarianceList(path);
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("FilePath,segment,Type, length, jumps, hops, dives, movetoward, moveaway, moveup, leftscore, rightscore");

        foreach (var replayStat in stats)
        {
            builder.AppendFormat("{0},{1},{2},{3},{4},{5},{6},{7},{8}, {9}, {10},{11}\n", 
                replayStat.fileName,
                replayStat.segmentID,
                Path.GetDirectoryName(path),
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

        private static List<ReplayStat> AnalyseForVarianceList(string path)
        {
            List<ReplayStat> stats = new List<ReplayStat>();
            if (path.Length != 0)
            {
                List<TextAsset> xmlFiles = new List<TextAsset>();
                var assets = AssetDatabase.FindAssets("t:TextAsset", new[] {path});
                foreach (var asset in assets)
                {
                    if (!AssetDatabase.GUIDToAssetPath(asset).EndsWith("bytes"))
                    {
                        continue;
                    }

                    var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(asset));
                    var replay = Replay.ImportFromTextAsset(textAsset, Replay.SerializationStyle.ProtoBufNet);
                    var result = AnalyseReplayForVariance(replay, SEGMENT_COUNT, AssetDatabase.GUIDToAssetPath(asset),false);
                    stats.AddRange(result);
                }
            }

            return stats;
        }


        [MenuItem("DropFoot/Variance/Analyse Single Replay")]
    static void AnalyseSingleReplayVariance() {
        string path = EditorUtility.OpenFilePanel("Select Replay", "", "bytes");
        var replay = Replay.ImportFromFile(path, Replay.SerializationStyle.ProtoBufNet);
        AnalyseReplayForVariance(replay,SEGMENT_COUNT, path);
    }
    
    [MenuItem("DropFoot/Standard/Analyse Replay Folder")]
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
        builder.AppendLine("FilePath, length, jumps, hops, dives, movetoward, moveaway, moveup, leftscore, rightscore");

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

    static List<ReplayStat> AnalyseReplayForVariance(Replay replay, int segmentCount, string filename, bool printData = true)
    {
        List<ReplayStat> result = new List<ReplayStat>();
        List<ReplayStat> windowSegments = new List<ReplayStat>();
        ReplayStat replayStat = new ReplayStat();
        replayStat.fileName = filename;
        bool first = true;
        bool started = false;
        bool scoreJustChanged = false;
        ReplayEntry previousEntry = new ReplayEntry();
        float timePerSegment = 1;
        float lastDivide = 0;
        int lastLeftScore = replay.leftStartScore;
        int lastRightScore = replay.rightStartScore;
        //Generate all the windows
        foreach (var entry in replay.entries)
        {
            if (first)
            {
                previousEntry = entry;
                first = false;
                continue;
            }

            if (!started)
            {
                if (entry.time - previousEntry.time > 0.5f)
                {
                    previousEntry = entry;
                    continue;
                }
                lastDivide = entry.time;
                started = true;
            }
            if (entry.time -lastDivide> timePerSegment)
            {
                replayStat.leftScore = entry.leftScore - lastLeftScore;
                replayStat.rightScore = entry.rightScore - lastRightScore;
                lastLeftScore  = entry.leftScore;
                lastRightScore = entry.rightScore; 
                replayStat.length = entry.time-lastDivide;
                PrintReplayStat(replayStat, printData);
                windowSegments.Add(replayStat);
               
                
                replayStat = new ReplayStat();
                replayStat.fileName = filename;
                
                lastDivide = entry.time;
            }
            

            
            scoreJustChanged = UpdateStatsForEntry(ref replayStat, entry, previousEntry, scoreJustChanged);
            previousEntry = entry;
        }
        int segmentID = 0;
        for (int i = segmentCount-1; i < windowSegments.Count; i++)
        {
            var temp = windowSegments[i];
            for (int j = 1; j < segmentCount; j++)
            {
                temp += windowSegments[i-j];
            }
            
            temp.segmentID = segmentID;
            segmentID++;
            result.Add(temp);
        }
        
        return result;
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
            
            scoreJustChanged = UpdateStatsForEntry(ref replayStat, entry, previousEntry, scoreJustChanged);

            previousEntry = entry;
            
        }
        replayStat.leftScore = (previousEntry.leftScore - replay.leftStartScore);
        replayStat.rightScore = (previousEntry.rightScore - replay.rightStartScore);

        PrintReplayStat(replayStat, printData);
        
        return replayStat;
    }

    private static void PrintReplayStat(ReplayStat replayStat, bool printData)
    {
        if(!printData)
            return;

        Debug.Log("Final Score: " + replayStat.leftScore + "::" + replayStat.rightScore + ". Length: " + replayStat.length);
        Debug.Log("HopCount: " + replayStat.hopCount + ". JumpCount: " + replayStat.jumpCount + ". DiveCount: " +
                  replayStat.diveCount);
        Debug.Log("MoveToward: " + replayStat.distanceMovedTowardsOpponent + ". MoveAway: " +
                  replayStat.distanceMovedAwayOpponent + ". Move up: " + replayStat.distanceMovedUp);
    }

    private static bool UpdateStatsForEntry( ref ReplayStat replayStat, ReplayEntry entry,
        ReplayEntry previousEntry, bool scoreJustChanged)
    {
        Vector3 distanceChange = entry.leftPlayerData.position - previousEntry.leftPlayerData.position;

        if (distanceChange.sqrMagnitude > 0.1 && scoreJustChanged)
        {
            return false;
        }
        
        if (distanceChange.y > 0)
        {
            replayStat.distanceMovedUp += distanceChange.y;
        }

        float directionToOpponent = Mathf.Sign(entry.rightPlayerData.position.x - entry.leftPlayerData.position.x);
        if (Mathf.Sign(distanceChange.x) == directionToOpponent)
        {
            replayStat.distanceMovedTowardsOpponent += Mathf.Abs(distanceChange.x);
        }
        else
        {
            replayStat.distanceMovedAwayOpponent += Mathf.Abs(distanceChange.x);
        }

        if (entry.leftPlayerData.dropping && !previousEntry.leftPlayerData.dropping)
        {
            replayStat.diveCount++;
        }

        if (!entry.leftPlayerData.onFloor && previousEntry.leftPlayerData.onFloor)
        {
            if (Mathf.Abs(distanceChange.x) > 0)
            {
                replayStat.hopCount++;
            }
            else
            {
                replayStat.jumpCount++;
            }
        }
        return scoreJustChanged | previousEntry.leftScore != entry.leftScore || previousEntry.rightScore != entry.rightScore;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
