using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KNN;
using KNN.Jobs;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;
using Redzen.Numerics.Distributions;
using Redzen.Random;
using SharpNeat.Genomes.Neat;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


public class GenomeRepository : IDisposable
{
    private IEstimator<ITransformer> dataPrepEstimator;
    private ITransformer dataPrepTransformer;
    private PrincipalComponentAnalysisTransformer pcaTranformer;
    private const int MAX_REPOSITORY_SIZE = 100000;
    private NativeArray<float3> repositoryPoints;

    private KnnContainer knnContainer;
    private JobHandle rebuildHandle;
    private List<GenomeMetric> metricRepository;
    private List<GenomeMetric> testCases;
    public float thresholdSq = 0.2f;
    public Bounds pcaBounds = new Bounds();
    private int targetSize = 300;
    private readonly ScatterPlot scatterPlot;
    MLContext mlContext = new MLContext();
    private HashSet<string> columnNames;
    private IRandomSource _rng;
    private readonly bool writeData;
    private readonly int runNumber;
    private readonly string gameType;
    private int nextModelUpdateValue = 0;
    private int nextModelUpdateGapIncrease = 10;
    private List<string> outputHeaders;

    public const bool RETRAIN_BASED_ON_REPO_SIZE = false;
    private DateTime beginTime;

    void UpdateThreshold()
    {
        thresholdSq = (pcaBounds.size.x * pcaBounds.size.x + pcaBounds.size.y * pcaBounds.size.y +
                       pcaBounds.size.z * pcaBounds.size.z) /
                      (targetSize * 2.0f);
    }


    public GenomeRepository(int targetSize, ScatterPlot scatterPlot, IRandomSource rng, bool writeData = false,
        string gameType = null, int runNumber = 0, DateTime? startTime = null)
    {
        columnsToIgnore = new HashSet<string>();

        columnsToIgnore.Add("leftHeadshots");
        columnsToIgnore.Add("leftHits");
        columnsToIgnore.Add("leftHops");
        columnsToIgnore.Add("leftJumps");
        columnsToIgnore.Add("leftDiveKicks");
        columnsToIgnore.Add("leftVerticalDistance");
        columnsToIgnore.Add("leftApproachDistance");
        columnsToIgnore.Add("leftRetreatDistance");

        metricRepository = new List<GenomeMetric>();
        repositoryPoints = new NativeArray<float3>(MAX_REPOSITORY_SIZE, Allocator.Persistent);
        for (int i = 0; i < MAX_REPOSITORY_SIZE; i++)
        {
            repositoryPoints[i] = new float3(float.MinValue, float.MinValue, float.MinValue);
        }

        knnContainer = new KnnContainer(repositoryPoints, false, Allocator.Persistent);
        this.targetSize = targetSize;
        this.scatterPlot = scatterPlot;
        _rng = rng;
        this.writeData = writeData;
        this.runNumber = runNumber;
        this.gameType = gameType ?? "Unknown";
        if (startTime.HasValue)
        {
            beginTime = startTime.Value;
        }
        else
        {
            beginTime = DateTime.Now;
        }
    }


    public void Dispose()
    {
        rebuildHandle.Complete();

        repositoryPoints.Dispose();
        knnContainer.Dispose();

        if (metricDataStream != null)
        {
            metricDataStream.Flush();
            repoContentDataStream.Flush();
            metricDataStream.Dispose();
            repoContentDataStream.Dispose();
        }
    }

    private float GetStableValue(float value)
    {
        if (float.IsNaN(value))
            return 0;

        return value;
    }

    private HashSet<string> columnsToIgnore;

    public NativeArray<float3> GetPCAPoints(List<GenomeMetric> tempRepository, bool updateBounds)
    {
        IDataView inData = new GenomeMetricDataView(tempRepository, columnsToIgnore);
        IDataView normalisedData = dataPrepTransformer.Transform(inData);
        var pcaData = pcaTranformer.Transform(normalisedData);
        DataViewSchema columns = pcaData.Schema;
        NativeArray<float3> queryPoints = new NativeArray<float3>(tempRepository.Count(), Allocator.TempJob);

        using (DataViewRowCursor cursor = pcaData.GetRowCursor(columns))
        {
            VBuffer<float> pcaVector = default;
            DataViewRowId rowId = default;
            var pcaGetter = cursor.GetGetter<VBuffer<float>>(columns.GetColumnOrNull("FeaturesPCA").Value);
            var idGetter = cursor.GetIdGetter();
            int index = 0;
            while (cursor.MoveNext())
            {
                //Get values from respective columns
                pcaGetter.Invoke(ref pcaVector);
                idGetter.Invoke(ref rowId);

                Debug.Assert(tempRepository[index].genome.Id == rowId.Low);
                queryPoints[index] = new float3(GetStableValue(pcaVector.GetItemOrDefault(0)),
                    GetStableValue(pcaVector.GetItemOrDefault(1)),
                    GetStableValue(pcaVector.GetItemOrDefault(2)));

                if (updateBounds)
                {
                    pcaBounds.Encapsulate(queryPoints[index]);
                }

                index++;
            }
        }

        return queryPoints;
    }

    public void UpdatePCAModel()
    {
        var start = testCases != null ? GetPCAPoints(testCases, false) : default;

        Debug.Log("Retraining model");
        IDataView inData = new GenomeMetricDataView(metricRepository, columnsToIgnore);
        dataPrepTransformer = dataPrepEstimator.Fit(inData);
        IDataView normalisedData = dataPrepTransformer.Transform(inData);

        var pcaEstimator = mlContext.Transforms.ProjectToPrincipalComponents("FeaturesPCA", "Features", null, 3);
        pcaTranformer = pcaEstimator.Fit(normalisedData);
        var pcaData = pcaTranformer.Transform(normalisedData);
        DataViewSchema columns = pcaData.Schema;

        pcaBounds = new Bounds();
        using (DataViewRowCursor cursor = pcaData.GetRowCursor(columns))
        {
            VBuffer<float> pcaVector = default;
            DataViewRowId rowId = default;
            var pcaGetter = cursor.GetGetter<VBuffer<float>>(columns.GetColumnOrNull("FeaturesPCA").Value);
            var idGetter = cursor.GetIdGetter();
            int index = 0;
            while (cursor.MoveNext())
            {
                //Get values from respective columns
                pcaGetter.Invoke(ref pcaVector);
                idGetter.Invoke(ref rowId);

                Debug.Assert(metricRepository[index].genome.Id == rowId.Low);

                repositoryPoints[index] = new float3(
                    GetStableValue(pcaVector.GetItemOrDefault(0)),
                    GetStableValue(pcaVector.GetItemOrDefault(1)),
                    GetStableValue(pcaVector.GetItemOrDefault(2)));
                pcaBounds.Encapsulate(repositoryPoints[index]);
                index++;
            }
        }

        rebuildHandle.Complete();
        rebuildHandle = new KnnRebuildJob(knnContainer).Schedule();

        UpdateThreshold();
        //PruneClosePoints();
        if (start.IsCreated)
        {
            var end = GetPCAPoints(testCases, false);
            var distanceVector = start[0] - end[0];
            var distance = ((Vector3) distanceVector).magnitude;
            Debug.Log("Changed by: " + distance);
            start.Dispose();
            end.Dispose();
        }
    }

    void PruneClosePoints()
    {
        List<int> toBePruned = new List<int>();
        for (int i = 0; i < metricRepository.Count(); i++)
        {
            if (toBePruned.Contains(i))
            {
                continue;
            }

            HashSet<int> possiblyClose = new HashSet<int>();
            possiblyClose.Add(i);
            int toKeepIndex = i;
            for (int j = i + 1; j < metricRepository.Count(); j++)
            {
                if (toBePruned.Contains(j))
                {
                    continue;
                }

                var dist = Vector3.SqrMagnitude(repositoryPoints[i] - repositoryPoints[j]);
                if (dist < thresholdSq)
                {
                    possiblyClose.Add(j);

                    if ((metricRepository[j].curiosityScore > metricRepository[toKeepIndex].curiosityScore) ||
                        (metricRepository[j].curiosityScore == metricRepository[toKeepIndex].curiosityScore &&
                         metricRepository[j].genome.BirthGeneration >
                         metricRepository[toKeepIndex].genome.BirthGeneration))
                    {
                        toKeepIndex = j;
                    }
                }
            }

            possiblyClose.Remove(toKeepIndex);
            toBePruned.AddRange(possiblyClose);
            foreach (int prunedIndex in toBePruned)
            {
                metricRepository[toKeepIndex].nearTally += metricRepository[prunedIndex].nearTally;
            }
        }


        int insertionIndex = 0;
        for (int i = 0; i < metricRepository.Count(); i++)
        {
            if (toBePruned.Contains(i))
            {
                continue;
            }

            repositoryPoints[insertionIndex++] = repositoryPoints[i];
        }

        toBePruned.Sort();
        toBePruned.Reverse();
        foreach (int prunedPoint in toBePruned)
        {
            metricRepository.RemoveAt(prunedPoint);
        }
    }

    public void WaitForTree()
    {
        rebuildHandle.Complete();
    }


    public void Initialise(List<GenomeMetric> tempRepertoire)
    {
        columnNames = new HashSet<string>(tempRepertoire[0].metrics.Keys);
        columnNames.ExceptWith(columnsToIgnore);
        dataPrepEstimator = mlContext.Transforms.Concatenate("Features", columnNames.ToArray())
            .Append(mlContext.Transforms.NormalizeMinMax("Features"));
        //Early setup of PCA estimator

        IDataView inData = new GenomeMetricDataView(tempRepertoire, columnsToIgnore);
        dataPrepTransformer = dataPrepEstimator.Fit(inData);
        IDataView normalisedData = dataPrepTransformer.Transform(inData);
        var pcaEstimator = mlContext.Transforms.ProjectToPrincipalComponents("FeaturesPCA", "Features", null, 3);
        pcaTranformer = pcaEstimator.Fit(normalisedData);
        pcaBounds = new Bounds();
        using (var queryPoints = GetPCAPoints(tempRepertoire, true))
        {
            UpdateThreshold();
            Debug.Log("Initial Threshold: " + thresholdSq);
            HashSet<int> indexesToSkip = new HashSet<int>();

            //Remove "overlapping"
            for (int i = 0; i < queryPoints.Count(); i++)
            {
                for (int j = i + 1; j < queryPoints.Count(); j++)
                {
                    if (indexesToSkip.Contains(j))
                    {
                        Debug.Log("Skipping one");
                        continue;
                    }

                    var dist = Vector3.SqrMagnitude(queryPoints[i] -
                                                    queryPoints[j]);
                    if ((tempRepertoire.Count - (indexesToSkip.Count + 1) >= 5) && dist < thresholdSq)
                    {
                        indexesToSkip.Add(j);
                        tempRepertoire[i].nearTally++;
                    }
                }
            }

            metricRepository = new List<GenomeMetric>();
            for (int i = 0; i < tempRepertoire.Count(); i++)
            {
                if (indexesToSkip.Contains(i))
                {
                    continue;
                }

                metricRepository.Add(tempRepertoire[i]);
            }
        }

        Debug.Log("Initial repertoire size: " + metricRepository.Count());
        UpdatePCAModel();

        scatterPlot.ClearGraph();
        Vector3[] drawPoints = new Vector3[metricRepository.Count()];
        for (int i = 0; i < metricRepository.Count(); i++)
        {
            drawPoints[i] = repositoryPoints[i];
        }

        scatterPlot.DrawLayer(drawPoints, metricRepository.ToArray(), 0);
        if (RETRAIN_BASED_ON_REPO_SIZE)
        {
            nextModelUpdateValue = metricRepository.Count + 10;
            nextModelUpdateGapIncrease = 20;
        }
        else
        {
            nextModelUpdateValue = 10;
            nextModelUpdateGapIncrease = 10;
        }

        if (testCases == null)
        {
            testCases = new List<GenomeMetric>();
            testCases.Add(tempRepertoire[0]);
        }
    }

    public void HandleNewGeneration(List<GenomeMetric> tempRepository, uint generation)
    {
        var queryPoints = GetPCAPoints(tempRepository, false);
        //Find Nearest Neighbour for each.
        WaitForTree();

        var result = new NativeArray<int>(tempRepository.Count, Allocator.TempJob);
        var batchQueryJob = new QueryKNearestBatchJob(knnContainer, queryPoints, result);
        batchQueryJob.ScheduleBatch(queryPoints.Length, Mathf.Max(1, queryPoints.Length / 32)).Complete();
        float maxDist = 0;
        List<int> interestingIndexes = new List<int>();
        for (int i = 0; i < tempRepository.Count(); i++)
        {
            Debug.Assert(result[i] < metricRepository.Count,
                "result[i] < metricRepository.Count i=" + i + "&result[i]=" + result[i]);
            var dist = Vector3.SqrMagnitude(queryPoints[i] - repositoryPoints[result[i]]);
            maxDist = Mathf.Max(maxDist, dist);
            if (dist > thresholdSq)
            {
                interestingIndexes.Add(i);
            }
            else if (result[i] < metricRepository.Count)
            {
                metricRepository[result[i]].nearTally++;
            }
        }

        float maxInternalDist = 0;
        //Remove "overlapping"
        for (int i = 0; i < interestingIndexes.Count(); i++)
        {
            int pos = i + 1;
            while (pos < interestingIndexes.Count())
            {
                var dist = Vector3.SqrMagnitude(queryPoints[interestingIndexes[i]] -
                                                queryPoints[interestingIndexes[pos]]);
                if (dist < thresholdSq)
                {
                    interestingIndexes.RemoveAt(pos);
                    tempRepository[interestingIndexes[i]].nearTally++;
                }
                else
                {
                    pos++;
                }
            }
        }

        //Draw Graph
        scatterPlot.ClearGraph();
        List<Vector3> drawPoints = new List<Vector3>();
        List<Vector3> parentPoints = new List<Vector3>();
        List<GenomeMetric> drawData = new List<GenomeMetric>();
        List<GenomeMetric> parentData = new List<GenomeMetric>();
        for (int i = 0; i < metricRepository.Count(); i++)
        {
            if (metricRepository[i].childrenThisGen.Count > 0)
            {
                parentPoints.Add(repositoryPoints[i]);
                parentData.Add(metricRepository[i]);
            }
            else
            {
                drawPoints.Add(repositoryPoints[i]);
                drawData.Add(metricRepository[i]);
            }
        }

        scatterPlot.DrawLayer(drawPoints.ToArray(), drawData.ToArray(), 0);
        scatterPlot.DrawLayer(parentPoints.ToArray(), parentData.ToArray(), 3);
        Vector3[] thisGeneration = new Vector3[queryPoints.Length - interestingIndexes.Count];
        Vector3[] thisGenInteresting = new Vector3[interestingIndexes.Count];
        GenomeMetric[] thisGenData = new GenomeMetric[queryPoints.Length - interestingIndexes.Count];
        GenomeMetric[] thisGenInterestingData = new GenomeMetric[interestingIndexes.Count];
        int interestingPointer = 0;
        int normalPointer = 0;
        for (int i = 0; i < queryPoints.Length; i++)
        {
            if (interestingIndexes.Contains(i))
            {
                thisGenInteresting[interestingPointer] = (Vector3) queryPoints[i];
                thisGenInterestingData[interestingPointer] = tempRepository[i];
                interestingPointer++;
            }
            else
            {
                thisGenData[normalPointer] = tempRepository[i];
                thisGeneration[normalPointer] = ((Vector3) queryPoints[i]);
                normalPointer++;
            }
        }

        scatterPlot.DrawLayer(thisGeneration.ToArray(), thisGenData, 1);
        scatterPlot.DrawLayer(thisGenInteresting.ToArray(), thisGenInterestingData, 2);
        //Update parents curiosity!
        foreach (var potentialParent in metricRepository)
        {
            if (potentialParent.hasHadChildren == false)
            {
                potentialParent.curiosityScore += 0.5f;
                continue;
            }

            bool successfulProcreation = false;
            for (int i = 0; i < interestingIndexes.Count(); i++)
            {
                successfulProcreation |=
                    potentialParent.childrenThisGen.Contains(tempRepository[interestingIndexes[i]].genome.Id);
            }

            if (successfulProcreation)
            {
                potentialParent.curiosityScore = GenomeMetric.STARTING_CURIOSITY_SCORE;
            }
            else
            {
                potentialParent.curiosityScore = Mathf.Max(0,
                    potentialParent.curiosityScore - (0.5f * potentialParent.childrenThisGen.Count()));
            }
        }

        //Add new ones to repository
        for (int i = 0; i < interestingIndexes.Count(); i++)
        {
            repositoryPoints[metricRepository.Count] = queryPoints[interestingIndexes[i]];
            metricRepository.Add(tempRepository[interestingIndexes[i]]);
            pcaBounds.Encapsulate(queryPoints[interestingIndexes[i]]);
        }

        if (interestingIndexes.Count() > 0)
        {
            rebuildHandle = new KnnRebuildJob(knnContainer).Schedule();
            UpdateThreshold();
        }


        //Debug.Log("Added to repertoire: " + interestingIndexes.Count() + ". Max dist: " + maxDist);
        result.Dispose();
        queryPoints.Dispose();
        if (RETRAIN_BASED_ON_REPO_SIZE)
        {
            if (metricRepository.Count >= nextModelUpdateValue)
            {
                UpdatePCAModel();
                nextModelUpdateValue = metricRepository.Count + nextModelUpdateGapIncrease;
                //nextModelUpdateGapIncrease += 10;
            }
        }
        else
        {
            if (generation == nextModelUpdateValue)
            {
                UpdatePCAModel();
                nextModelUpdateValue = nextModelUpdateGapIncrease + nextModelUpdateValue;
                nextModelUpdateGapIncrease += 10;
                nextModelUpdateGapIncrease = Mathf.Min(nextModelUpdateGapIncrease, 20);
            }
        }

        if (writeData)
        {
            if (metricDataStream == null)
            {
                string baseMetricSavePath =
                    Path.Combine(Path.GetDirectoryName(Application.dataPath), "MetricStats");
                if (!Directory.Exists(baseMetricSavePath))
                {
                    Directory.CreateDirectory(baseMetricSavePath);
                }

                statFolder = Path.Combine(baseMetricSavePath,
                    string.Format("{2:yyyy-M-dd--HH-mm} {0} Run {1} Metrics", gameType, runNumber, beginTime));
                if (!Directory.Exists(statFolder))
                {
                    Directory.CreateDirectory(statFolder);
                }

                string filepath = Path.Combine(statFolder, "statistics.csv");
                string filepath2 = Path.Combine(statFolder, "repositoryContents.csv");
                recordedGenomeIds = new HashSet<uint>();
                outputHeaders = metricRepository[0].metrics.Keys.ToList();
                outputHeaders.Add("__birthGeneration__");
                outputHeaders.Add("__genomeId__");
                outputHeaders.Add("__nearbyTally__");
                metricDataStream = new StreamWriter(new FileStream(filepath, FileMode.Create,
                    FileAccess.Write, FileShare.ReadWrite));
                repoContentDataStream = new StreamWriter(new FileStream(filepath2, FileMode.Create,
                    FileAccess.Write, FileShare.ReadWrite));
                metricDataStream.WriteLine(String.Join(",", outputHeaders));
            }

            repoContentDataStream.WriteLine(String.Join(",", metricRepository.Select(x => x.genome.Id)));

            foreach (var genomeMetric in metricRepository)
            {
                if (!recordedGenomeIds.Contains(genomeMetric.genome.Id))
                {
                    recordedGenomeIds.Add(genomeMetric.genome.Id);
                    metricDataStream.WriteLine(String.Join(",", outputHeaders.Select(x =>
                    {
                        if (x.Equals("__birthGeneration__"))
                        {
                            return genomeMetric.genome.BirthGeneration;
                        }
                        else if (x.Equals("__genomeId__"))
                        {
                            return genomeMetric.genome.Id;
                        }
                        else if (x.Equals("__nearbyTally__"))
                        {
                            return genomeMetric.nearTally;
                        }
                        else
                        {
                            return genomeMetric.metrics[x];
                        }
                    })));

                    //Write the genome
                    /*string filename = string.Format("Genome{0}.xml", genomeMetric.genome.Id);
                    var xmlDoc = NeatGenomeXmlIO.SaveComplete(genomeMetric.genome, false);
                    xmlDoc.Save(Path.Combine(statFolder, filename));*/
                }
            }

            metricDataStream.Flush();
            repoContentDataStream.Flush();
        }
    }

    private string statFolder;
    private HashSet<uint> recordedGenomeIds;
    private StreamWriter metricDataStream = null;
    private StreamWriter repoContentDataStream = null;

    class GenomeDistance
    {
        [LoadColumn(0)] public float DistSq { get; set; }
    }

    public double[] GenerateNearestNeighbourNovelty(int kNeighbours)
    {
        rebuildHandle.Complete();
        kNeighbours = Mathf.Min(kNeighbours, metricRepository.Count());
        Debug.Assert(kNeighbours > 0, "kNeighbours is 0 or less");
        Debug.Assert(kNeighbours < metricRepository.Count, "kNeighbours is massive");
        var result = new NativeArray<int>(kNeighbours * metricRepository.Count, Allocator.TempJob);
        var batchQueryJob = new QueryKNearestBatchJob(knnContainer,
            repositoryPoints.GetSubArray(0, metricRepository.Count), result);
        batchQueryJob.ScheduleBatch(metricRepository.Count(), Mathf.Max(1, metricRepository.Count() / 32)).Complete();
        float maxDist = 0;
        GenomeDistance[] distances = new GenomeDistance[metricRepository.Count];

        for (int i = 0; i < metricRepository.Count(); i++)
        {
            float totalDistSq = 0;
            int startIndex = i * kNeighbours;
            for (int j = startIndex; j < startIndex + kNeighbours; j++)
            {
                Debug.Assert(i < repositoryPoints.Length, "i<repositoryPoints.Length");
                Debug.Assert(result[j] < repositoryPoints.Length,
                    "result[j]<repositoryPoints.Length: j=" + j + " result[j]=" + result[j]);
                totalDistSq += Vector3.SqrMagnitude(repositoryPoints[i] - repositoryPoints[result[j]]);
            }

            maxDist = Mathf.Max(maxDist, totalDistSq);
            distances[i] = new GenomeDistance() {DistSq = totalDistSq};
        }

        //Debug.Log("MAX DIST FOR NOVELTY: " + maxDist);

        var normaliser = mlContext.Transforms.NormalizeMinMax("DistSq");
        //Early setup of PCA estimator

        IDataView inData = mlContext.Data.LoadFromEnumerable<GenomeDistance>(distances);
        var normaliseTransformer = normaliser.Fit(inData);
        IDataView normalisedData = normaliseTransformer.Transform(inData);

        result.Dispose();

        return normalisedData.GetColumn<float>("DistSq").Select(x => (double) x).ToArray();
    }

    public enum SelectionType
    {
        Curisoity,
        Novelty,
        Uniform
    }

    public List<NeatGenome> GenerateOffspring(uint generation, int aSexualCount, int sexualCount,
        SelectionType selectionType = SelectionType.Novelty)
    {
        foreach (var genomeMetric in metricRepository)
        {
            genomeMetric.childrenThisGen.Clear();
        }

        double[] genomeProbabilities;

        switch (selectionType)
        {
            case SelectionType.Curisoity:
                genomeProbabilities = metricRepository.Select(x => (double) x.curiosityScore).ToArray();
                break;
            case SelectionType.Novelty:
                genomeProbabilities = GenerateNearestNeighbourNovelty(Mathf.Min(metricRepository.Count - 1, 5));
                break;
            case SelectionType.Uniform:
                genomeProbabilities = metricRepository.Select(x => 1.0).ToArray();
                break;
            default:
                genomeProbabilities = GenerateNearestNeighbourNovelty(Mathf.Min(metricRepository.Count - 1, 5));
                break;
        }

        //
        DiscreteDistribution dist = new DiscreteDistribution(genomeProbabilities);
        List<NeatGenome> offspringList = new List<NeatGenome>(aSexualCount + sexualCount);

        // --- Produce the required number of offspring from asexual reproduction.
        for (int i = 0; i < aSexualCount; i++)
        {
            int genomeIdx = DiscreteDistribution.Sample(_rng, dist);
            NeatGenome offspring = metricRepository[genomeIdx].genome.CreateOffspring(generation);
            offspringList.Add(offspring);
            metricRepository[genomeIdx].childrenThisGen.Add(offspring.Id);
            metricRepository[genomeIdx].hasHadChildren = true;
        }

        for (int i = 0; i < sexualCount; i++)
        {
            // Select parent 1.
            int parent1Idx = DiscreteDistribution.Sample(_rng, dist);
            NeatGenome parent1 = metricRepository[parent1Idx].genome;

            // Remove selected parent from set of possible outcomes.
            DiscreteDistribution distTmp = dist.RemoveOutcome(parent1Idx);

            // Test for existence of at least one more parent to select.
            if (0 != distTmp.Probabilities.Length)
            {
                // Get the two parents to mate.
                int parent2Idx = DiscreteDistribution.Sample(_rng, distTmp);
                NeatGenome parent2 = metricRepository[parent2Idx].genome;
                NeatGenome offspring = parent1.CreateOffspring(parent2, generation);
                offspringList.Add(offspring);
                metricRepository[parent1Idx].childrenThisGen.Add(offspring.Id);
                metricRepository[parent2Idx].childrenThisGen.Add(offspring.Id);
                metricRepository[parent1Idx].hasHadChildren = true;
                metricRepository[parent2Idx].hasHadChildren = true;
            }
            else
            {
                // No other parent has a non-zero selection probability (they all have zero fitness).
                // Fall back to asexual reproduction of the single genome with a non-zero fitness.
                NeatGenome offspring = parent1.CreateOffspring(generation);
                metricRepository[parent1Idx].childrenThisGen.Add(offspring.Id);
                metricRepository[parent1Idx].hasHadChildren = true;
                offspringList.Add(offspring);
            }
        }

        return offspringList;
    }
}