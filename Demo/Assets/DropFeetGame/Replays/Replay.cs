using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;

namespace Assets.DropFeetGame.Replays
{

    [Serializable]
    [ProtoContract(SkipConstructor = true)]
    public class Replay
    {
        public enum SerializationStyle { DotNet, ProtoBufNet }

        [ProtoMember(1)]
        public int leftStartScore;

        [ProtoMember(2)]
        public int rightStartScore;


        [ProtoMember(3)]
        public Queue<ReplayEntry> entries;

        static bool hasSetup = false;

        static void SetupModel()
        {
            if (hasSetup)
                return;

            hasSetup = true;
            ProtoBuf.Meta.RuntimeTypeModel.Default.Add(typeof(Vector3), false).SetSurrogate(typeof(ProtoVector3));
            ProtoBuf.Meta.RuntimeTypeModel.Default.Add(typeof(Queue<ReplayEntry>), false).SetSurrogate(typeof(ProtoQueue));
        }

        public readonly static string baseSavePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Replays");
        public Replay(int leftStartScore, int rightStartScore)
        {
            entries = new Queue<ReplayEntry>();
            this.leftStartScore = leftStartScore;
            this.rightStartScore = rightStartScore;
        }

        internal Replay Clone()
        {
            Replay result = new Replay(this.leftStartScore, this.rightStartScore);

            result.entries = new Queue<ReplayEntry>(entries);
            return result;
        }

        void SaveOldStyle(FileStream fileStream, string filepath)
        {
            
            BinaryFormatter formatter = new BinaryFormatter();
            SurrogateSelector surrogateSelector = new SurrogateSelector();
            Vector3SerializationSurrogate vector3SS = new Vector3SerializationSurrogate();

            surrogateSelector.AddSurrogate(typeof(Vector3), new StreamingContext(StreamingContextStates.All), vector3SS);
            formatter.SurrogateSelector = surrogateSelector;
            try
            {                
                formatter.Serialize(fileStream, this);
                Debug.Log("Wrote out file to " + filepath);
            }
            catch (SerializationException e)
            {
                Debug.LogError("Failed to serialize. Reason: " + e.Message);
                throw;
            }
        }

        public void Save(string filename, SerializationStyle serializationStyle = SerializationStyle.ProtoBufNet)
        {
            SetupModel();
            if (!Directory.Exists(baseSavePath))
            {
                Directory.CreateDirectory(baseSavePath);
            }

            string filepath = Path.Combine(baseSavePath, filename);
            try
            {
                using (FileStream fileStream = File.Create(filepath))
                {

                    if (serializationStyle == SerializationStyle.DotNet)
                    {
                        SaveOldStyle(fileStream, filepath);
                        return;
                    }

                    Serializer.Serialize<Replay>(fileStream, this);
                }
            }
            catch(Exception e)
            {
                Debug.LogError("Failed to serialize. Reason: " + e.Message);
                throw;
            }

        }

        public static Replay ImportFromFile(string filePath, SerializationStyle serializationStyle = SerializationStyle.DotNet)
        {
            SetupModel();
            using (var file = File.Open(filePath, FileMode.Open))
            {
                if (serializationStyle == SerializationStyle.DotNet)
                {
                    return LoadOldStyle(file);
                }
                try
                {
                    return Serializer.Deserialize<Replay>(file);
                }
                catch (SerializationException e)
                {
                    Debug.Log("Failed to deserialize. Reason: " + e.Message);
                    throw;
                }
            }
        }

        private static Replay LoadOldStyle(FileStream file)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            SurrogateSelector surrogateSelector = new SurrogateSelector();
            Vector3SerializationSurrogate vector3SS = new Vector3SerializationSurrogate();

            surrogateSelector.AddSurrogate(typeof(Vector3), new StreamingContext(StreamingContextStates.All), vector3SS);
            formatter.SurrogateSelector = surrogateSelector;

            try
            {
                return formatter.Deserialize(file) as Replay;
            }
            catch (SerializationException e)
            {
                Debug.Log("Failed to deserialize. Reason: " + e.Message);
                throw;
            }
        }
    }

    [Serializable]
    [ProtoContract(SkipConstructor = true)]
    public struct ReplayEntry
    {
        [ProtoMember(1)]
        public ReplayPlayerInfo leftPlayerData;

        [ProtoMember(2)]
        public ReplayPlayerInfo rightPlayerData;

        [ProtoMember(3)]
        public int leftScore;

        [ProtoMember(4)]
        public int rightScore;

        [ProtoMember(5)]
        public float time;
    }

    [Serializable]
    [ProtoContract(SkipConstructor = true)]
    public struct ReplayPlayerInfo
    {
        [ProtoMember(1)]
        public Vector3 position;

        [ProtoMember(2)]
        public bool dropping;

        [ProtoMember(3)]
        public bool onFloor;
    }


    [Serializable]
    [ProtoContract(SkipConstructor = true)]
    public struct ProtoQueue
    {
        [ProtoMember(1)]
        List<ReplayEntry> entries;

        public static implicit operator Queue<ReplayEntry>(ProtoQueue v)
        {
            return new Queue<ReplayEntry>(v.entries);
        }

        public static implicit operator ProtoQueue(Queue<ReplayEntry> v)
        {
            if (v == null)
                return new ProtoQueue();
            ProtoQueue protoQueue = new ProtoQueue();
            protoQueue.entries = new List<ReplayEntry>(v);
            return protoQueue;
        }
    }

    [Serializable]
    [ProtoContract(SkipConstructor = true)]
    public struct ProtoVector3
    {
        [ProtoMember(1)]
        public float x;

        [ProtoMember(2)]
        public float y;

        [ProtoMember(3)]
        public float z;

        public static implicit operator Vector3(ProtoVector3 v)
        {
            return new Vector3(v.x, v.y, v.z);
        }

        public static implicit operator ProtoVector3(Vector3 v)
        {
            ProtoVector3 protoVector = new ProtoVector3();
            protoVector.x = v.x;
            protoVector.y = v.y;
            protoVector.z = v.z;
            return protoVector;
        }
    }
}
