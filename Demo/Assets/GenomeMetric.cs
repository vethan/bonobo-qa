using System.Collections.Generic;
using SharpNeat.Genomes.Neat;
using UnityEngine;

public class GenomeMetric
{
    public const float STARTING_CURIOSITY_SCORE = 10;
    public NeatGenome genome;
    public Dictionary<string, float> metrics;
    public float curiosityScore = STARTING_CURIOSITY_SCORE;
    public HashSet<uint> childrenThisGen = new HashSet<uint>();
    public Vector2 PcaPosition;
    public bool hasHadChildren = false;
}