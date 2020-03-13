using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.DropFeetGame.Replays
{
    [Serializable]
    public class Replay
    {
        public Queue<ReplayEntry> entries;

        public Replay()
        {
            entries = new Queue<ReplayEntry>();
        }
    }

    [Serializable]
    public struct ReplayEntry
    {
        public ReplayPlayerInfo leftPlayerData;
        public ReplayPlayerInfo rightPlayerData;
        public int leftScore;
        public int rightScore;
        public float time;
    }

    [Serializable]
    public struct ReplayPlayerInfo
    {
        public Vector3 position;
        public bool dropping;
        public bool onFloor;
    }
}
