using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KNN;
using KNN.Jobs;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;
using Redzen.Numerics;
using Redzen.Numerics.Distributions;
using Redzen.Random;
using Redzen.Sorting;
using SharpNeat;
using SharpNeat.Core;
using SharpNeat.DistanceMetrics;
using SharpNeat.Genomes.Neat;
using SharpNeat.SpeciationStrategies;
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
    private Dictionary<uint, GenomeMetric> metricIdLookup;
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
    private readonly int speciesCount;
    private readonly string gameType;
    private int nextModelUpdateValue = 0;
    private int nextModelUpdateGapIncrease = 10;
    private List<string> outputHeaders;

    public const bool RETRAIN_BASED_ON_REPO_SIZE = false;
    private DateTime beginTime;

    private ISpeciationStrategy<NeatGenome> _speciationStrategy;
    IList<Specie<NeatGenome>> _specieList;

    void UpdateThreshold()
    {
        thresholdSq = (pcaBounds.size.x * pcaBounds.size.x + pcaBounds.size.y * pcaBounds.size.y +
                       pcaBounds.size.z * pcaBounds.size.z) /
                      (targetSize * 2.0f);
    }


    public GenomeRepository(int targetSize, ScatterPlot scatterPlot, IRandomSource rng, bool behaviourSpec,
        bool writeData = false,
        string gameType = null, int runNumber = 0, DateTime? startTime = null, int speciesCount = 4)
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
        metricIdLookup = new Dictionary<uint, GenomeMetric>();
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
        this.speciesCount = speciesCount;
        this.gameType = gameType ?? "Unknown";
        if (startTime.HasValue)
        {
            beginTime = startTime.Value;
        }
        else
        {
            beginTime = DateTime.Now;
        }

        if (behaviourSpec)
        {
            _speciationStrategy =
                new KMeansBehaviourClusteringStrategy<NeatGenome>(this, new EuclideanDistanceMetric());
        }
        else
        {
            _speciationStrategy = new KMeansClusteringStrategy<NeatGenome>(new EuclideanDistanceMetric());
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

    public NativeArray<float3> GetPCAPoints(List<GenomeMetric> tempRepository, bool updateBounds,
        bool forceAddLookup = false)
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
                if (forceAddLookup)
                {
                    pointLookup[tempRepository[index].genome.Id] = (Vector3)queryPoints[index];
                }

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
                pointLookup[metricRepository[index].genome.Id] = repositoryPoints[index];

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
            var distance = ((Vector3)distanceVector).magnitude;
            Debug.Log("Changed by: " + distance);
            start.Dispose();
            end.Dispose();
        }

        if (_specieList == null)
        {
            Debug.Log("NO species set up");
            return;
        }

        foreach (Specie<NeatGenome> specie in _specieList)
        {
            specie.GenomeList.Clear();
        }

        _speciationStrategy.SpeciateGenomes(metricRepository.Select(x => x.genome).ToList(), _specieList);
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
            var metric = metricRepository[prunedPoint];
            metricRepository.RemoveAt(prunedPoint);
            metricIdLookup.Remove(metric.genome.Id);
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
//                        Debug.Log("Skipping one");
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
                metricIdLookup[tempRepertoire[i].genome.Id] = tempRepertoire[i];
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

        _specieList =
            _speciationStrategy.InitializeSpeciation(metricRepository.Select(x => x.genome).ToList(), speciesCount);
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
            testCases = new List<GenomeMetric> { tempRepertoire[0] };
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
                thisGenInteresting[interestingPointer] = (Vector3)queryPoints[i];
                thisGenInterestingData[interestingPointer] = tempRepository[i];
                interestingPointer++;
            }
            else
            {
                thisGenData[normalPointer] = tempRepository[i];
                thisGeneration[normalPointer] = ((Vector3)queryPoints[i]);
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
        List<GenomeMetric> newThisGeneration = new List<GenomeMetric>();
        for (int i = 0; i < interestingIndexes.Count(); i++)
        {
            repositoryPoints[metricRepository.Count] = queryPoints[interestingIndexes[i]];
            pointLookup[tempRepository[interestingIndexes[i]].genome.Id] = queryPoints[interestingIndexes[i]];
            metricRepository.Add(tempRepository[interestingIndexes[i]]);
            metricIdLookup[tempRepository[interestingIndexes[i]].genome.Id] = tempRepository[interestingIndexes[i]];
            newThisGeneration.Add(tempRepository[interestingIndexes[i]]);
            pcaBounds.Encapsulate(queryPoints[interestingIndexes[i]]);
        }


        if (interestingIndexes.Count() > 0)
        {
            _speciationStrategy.SpeciateOffspring(newThisGeneration.Select(x => x.genome).ToList(), _specieList);
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
    private int _bestSpecieIdx;

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
            distances[i] = new GenomeDistance() { DistSq = totalDistSq };
        }

        //Debug.Log("MAX DIST FOR NOVELTY: " + maxDist);

        var normaliser = mlContext.Transforms.NormalizeMinMax("DistSq");
        //Early setup of PCA estimator

        IDataView inData = mlContext.Data.LoadFromEnumerable<GenomeDistance>(distances);
        var normaliseTransformer = normaliser.Fit(inData);
        IDataView normalisedData = normaliseTransformer.Transform(inData);

        result.Dispose();

        return normalisedData.GetColumn<float>("DistSq").Select(x => (double)x).ToArray();
    }

    public enum SelectionType
    {
        Curisoity,
        Novelty,
        Uniform
    }

    public void AssignScoresToFitness(SelectionType selectionType)
    {
        switch (selectionType)
        {
            case SelectionType.Curisoity:
                foreach (var genomeMetric in metricRepository)
                {
                    genomeMetric.genome.EvaluationInfo.SetFitness(genomeMetric.curiosityScore);
                }

                break;
            case SelectionType.Novelty:
                var genomeProbabilities = GenerateNearestNeighbourNovelty(Mathf.Min(metricRepository.Count - 1, 5));
                for (int i = 0; i < genomeProbabilities.Length; i++)
                {
                    metricRepository[i].genome.EvaluationInfo.SetFitness(genomeProbabilities[i]);
                }

                break;
            case SelectionType.Uniform:
                foreach (var genomeMetric in metricRepository)
                {
                    genomeMetric.genome.EvaluationInfo.SetFitness(1);
                }

                break;
            default:
                foreach (var genomeMetric in metricRepository)
                {
                    genomeMetric.genome.EvaluationInfo.SetFitness(genomeMetric.curiosityScore);
                }

                break;
        }
    }

    public List<NeatGenome> GenerateOffspring(uint generation, int populationSize, double OffspringAsexualProportion,
        double selectionProportion,
        double interspeciesProportion,
        SelectionType selectionType = SelectionType.Novelty)
    {
        AssignScoresToFitness(selectionType);
        SortSpecieGenomes();
        UpdateBestGenome();


        int offspringCount;
        SpecieStats[] specieStatsArr = CalcSpecieStats(out offspringCount, populationSize, OffspringAsexualProportion,
            selectionProportion);
        int specieCount = specieStatsArr.Length;
        double[] specieFitnessArr = new double[specieCount];
        DiscreteDistribution[] distArr = new DiscreteDistribution[specieCount];
        foreach (var genomeMetric in metricRepository)
        {
            genomeMetric.childrenThisGen.Clear();
        }

        // Count of species with non-zero selection size.
        // If this is exactly 1 then we skip inter-species mating. One is a special case because for 0 the 
        // species all get an even chance of selection, and for >1 we can just select normally.
        int nonZeroSpecieCount = 0;
        for (int i = 0; i < specieCount; i++)
        {
            // Array of probabilities for specie selection. Note that some of these probabilities can be zero, but at least one of them won't be.
            SpecieStats inst = specieStatsArr[i];
            specieFitnessArr[i] = inst._selectionSizeInt;

            if (0 == inst._selectionSizeInt)
            {
                // Skip building a DiscreteDistribution for species that won't be selected from.
                distArr[i] = null;
                continue;
            }

            nonZeroSpecieCount++;

            // For each specie we build a DiscreteDistribution for genome selection within 
            // that specie. Fitter genomes have higher probability of selection.
            List<NeatGenome> genomeList = _specieList[i].GenomeList;
            double[] probabilities = new double[inst._selectionSizeInt];
            for (int j = 0; j < inst._selectionSizeInt; j++)
            {
                probabilities[j] = genomeList[j].EvaluationInfo.Fitness;
            }

            distArr[i] = new DiscreteDistribution(probabilities);
        }

        // Complete construction of DiscreteDistribution for specie selection.
        DiscreteDistribution rwlSpecies = new DiscreteDistribution(specieFitnessArr);

        // Produce offspring from each specie in turn and store them in offspringList.
        List<NeatGenome> offspringList = new List<NeatGenome>(offspringCount);
        for (int specieIdx = 0; specieIdx < specieCount; specieIdx++)
        {
            SpecieStats inst = specieStatsArr[specieIdx];
            List<NeatGenome> genomeList = _specieList[specieIdx].GenomeList;

            // Get DiscreteDistribution for genome selection.
            DiscreteDistribution dist = distArr[specieIdx];

            // --- Produce the required number of offspring from asexual reproduction.
            for (int i = 0; i < inst._offspringAsexualCount; i++)
            {
                int genomeIdx = DiscreteDistribution.Sample(_rng, dist);
                NeatGenome offspring = genomeList[genomeIdx].CreateOffspring(generation);

                metricIdLookup[genomeList[genomeIdx].Id].childrenThisGen.Add(offspring.Id);
                metricIdLookup[genomeList[genomeIdx].Id].hasHadChildren = true;
                offspringList.Add(offspring);
            }

            // --- Produce the required number of offspring from sexual reproduction.
            // Cross-specie mating.
            // If nonZeroSpecieCount is exactly 1 then we skip inter-species mating. One is a special case because
            // for 0 the  species all get an even chance of selection, and for >1 we can just select species normally.
            int crossSpecieMatings = nonZeroSpecieCount == 1
                ? 0
                : (int)NumericsUtils.ProbabilisticRound(interspeciesProportion
                                                        * inst._offspringSexualCount, _rng);


            // An index that keeps track of how many offspring have been produced in total.
            int matingsCount = 0;
            for (; matingsCount < crossSpecieMatings; matingsCount++)
            {
                NeatGenome offspring =
                    CreateOffspring_CrossSpecieMating(generation, dist, distArr, rwlSpecies, specieIdx, genomeList);
                offspringList.Add(offspring);
            }

            // For the remainder we use normal intra-specie mating.
            // Test for special case - we only have one genome to select from in the current specie. 
            if (1 == inst._selectionSizeInt)
            {
                // Fall-back to asexual reproduction.
                for (; matingsCount < inst._offspringSexualCount; matingsCount++)
                {
                    int genomeIdx = DiscreteDistribution.Sample(_rng, dist);
                    NeatGenome offspring = genomeList[genomeIdx].CreateOffspring(generation);
                    offspringList.Add(offspring);
                    metricIdLookup[genomeList[genomeIdx].Id].childrenThisGen.Add(offspring.Id);
                    metricIdLookup[genomeList[genomeIdx].Id].hasHadChildren = true;
                }
            }
            else
            {
                // Remainder of matings are normal within-specie.
                for (; matingsCount < inst._offspringSexualCount; matingsCount++)
                {
                    // Select parent 1.
                    int parent1Idx = DiscreteDistribution.Sample(_rng, dist);
                    NeatGenome parent1 = genomeList[parent1Idx];

                    // Remove selected parent from set of possible outcomes.
                    DiscreteDistribution distTmp = dist.RemoveOutcome(parent1Idx);

                    // Test for existence of at least one more parent to select.
                    if (0 != distTmp.Probabilities.Length)
                    {
                        // Get the two parents to mate.
                        int parent2Idx = DiscreteDistribution.Sample(_rng, distTmp);
                        NeatGenome parent2 = genomeList[parent2Idx];
                        NeatGenome offspring = parent1.CreateOffspring(parent2, generation);
                        offspringList.Add(offspring);
                        metricIdLookup[genomeList[parent1Idx].Id].childrenThisGen.Add(offspring.Id);
                        metricIdLookup[genomeList[parent2Idx].Id].childrenThisGen.Add(offspring.Id);
                        metricIdLookup[genomeList[parent1Idx].Id].hasHadChildren = true;
                        metricIdLookup[genomeList[parent2Idx].Id].hasHadChildren = true;
                    }
                    else
                    {
                        // No other parent has a non-zero selection probability (they all have zero fitness).
                        // Fall back to asexual reproduction of the single genome with a non-zero fitness.
                        NeatGenome offspring = parent1.CreateOffspring(generation);
                        offspringList.Add(offspring);
                        metricIdLookup[genomeList[parent1Idx].Id].childrenThisGen.Add(offspring.Id);
                        metricIdLookup[genomeList[parent1Idx].Id].hasHadChildren = true;
                    }
                }
            }
        }

        return offspringList;
/*
        double[] genomeProbabilities;

        switch (selectionType)
        {
            case SelectionType.Curisoity:
                genomeProbabilities = metricRepository.Select(x => (double)x.curiosityScore).ToArray();
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

        return offspringList;*/
    }

    private void SortSpecieGenomes()
    {
        int minSize = _specieList[0].GenomeList.Count;
        int maxSize = minSize;
        int specieCount = _specieList.Count;

        for (int i = 0; i < specieCount; i++)
        {
            SortUtils.SortUnstable(_specieList[i].GenomeList, GenomeFitnessComparer<NeatGenome>.Singleton, _rng);
            minSize = System.Math.Min(minSize, _specieList[i].GenomeList.Count);
            maxSize = System.Math.Max(maxSize, _specieList[i].GenomeList.Count);
        }

        // Update stats.
        //_stats._minSpecieSize = minSize;
        //_stats._maxSpecieSize = maxSize;
    }

    private SpecieStats[] CalcSpecieStats(out int offspringCount, int populationSize, double OffspringAsexualProportion,
        double SelectionProportion)
    {
        double totalMeanFitness = 0.0;

        // Build stats array and get the mean fitness of each specie.
        int specieCount = _specieList.Count;
        SpecieStats[] specieStatsArr = new SpecieStats[specieCount];
        for (int i = 0; i < specieCount; i++)
        {
            SpecieStats inst = new SpecieStats();
            specieStatsArr[i] = inst;
            inst._meanFitness = _specieList[i].CalcMeanFitness();
            totalMeanFitness += inst._meanFitness;
        }

        // Calculate the new target size of each specie using fitness sharing. 
        // Keep a total of all allocated target sizes, typically this will vary slightly from the
        // overall target population size due to rounding of each real/fractional target size.
        int totalTargetSizeInt = 0;

        if (0.0 == totalMeanFitness)
        {
            // Handle specific case where all genomes/species have a zero fitness. 
            // Assign all species an equal targetSize.
            double targetSizeReal = (double)populationSize / (double)specieCount;

            for (int i = 0; i < specieCount; i++)
            {
                SpecieStats inst = specieStatsArr[i];
                inst._targetSizeReal = targetSizeReal;

                // Stochastic rounding will result in equal allocation if targetSizeReal is a whole
                // number, otherwise it will help to distribute allocations evenly.
                inst._targetSizeInt = (int)NumericsUtils.ProbabilisticRound(targetSizeReal, _rng);

                // Total up discretized target sizes.
                totalTargetSizeInt += inst._targetSizeInt;
            }
        }
        else
        {
            // The size of each specie is based on its fitness relative to the other species.
            /*  for (int i = 0; i < specieCount; i++)
              {
                  SpecieStats inst = specieStatsArr[i];
                  inst._targetSizeReal = (inst._meanFitness / totalMeanFitness) * (double) gamesToCreate;
  
                  // Discretize targetSize (stochastic rounding).
                  inst._targetSizeInt = (int) NumericsUtils.ProbabilisticRound(inst._targetSizeReal, _rng);
  
                  // Total up discretized target sizes.
                  totalTargetSizeInt += inst._targetSizeInt;
              }*/
        }

        // Discretized target sizes may total up to a value that is not equal to the required overall population
        // size. Here we check this and if there is a difference then we adjust the specie's targetSizeInt values
        // to compensate for the difference.
        //
        // E.g. If we are short of the required populationSize then we add the required additional allocation to
        // selected species based on the difference between each specie's targetSizeReal and targetSizeInt values.
        // What we're effectively doing here is assigning the additional required target allocation to species based
        // on their real target size in relation to their actual (integer) target size.
        // Those species that have an actual allocation below there real allocation (the difference will often 
        // be a fractional amount) will be assigned extra allocation probabilistically, where the probability is
        // based on the differences between real and actual target values.
        //
        // Where the actual target allocation is higher than the required target (due to rounding up), we use the same
        // method but we adjust specie target sizes down rather than up.
        int targetSizeDeltaInt = totalTargetSizeInt - populationSize;

        if (targetSizeDeltaInt < 0)
        {
            // Check for special case. If we are short by just 1 then increment targetSizeInt for the specie containing
            // the best genome. We always ensure that this specie has a minimum target size of 1 with a final test (below),
            // by incrementing here we avoid the probabilistic allocation below followed by a further correction if
            // the champ specie ended up with a zero target size.
            if (-1 == targetSizeDeltaInt)
            {
                specieStatsArr[0]._targetSizeInt++;
            }
            else
            {
                // We are short of the required populationSize. Add the required additional allocations.
                // Determine each specie's relative probability of receiving additional allocation.
                double[] probabilities = new double[specieCount];
                for (int i = 0; i < specieCount; i++)
                {
                    SpecieStats inst = specieStatsArr[i];
                    probabilities[i] = System.Math.Max(0.0, inst._targetSizeReal - (double)inst._targetSizeInt);
                }

                // Use a built in class for choosing an item based on a list of relative probabilities.
                DiscreteDistribution dist = new DiscreteDistribution(probabilities);

                // Probabilistically assign the required number of additional allocations.
                // FIXME/ENHANCEMENT: We can improve the allocation fairness by updating the DiscreteDistribution 
                // after each allocation (to reflect that allocation).
                // targetSizeDeltaInt is negative, so flip the sign for code clarity.
                targetSizeDeltaInt *= -1;
                for (int i = 0; i < targetSizeDeltaInt; i++)
                {
                    int specieIdx = DiscreteDistribution.Sample(_rng, dist);
                    specieStatsArr[specieIdx]._targetSizeInt++;
                }
            }
        }
        else if (targetSizeDeltaInt > 0)
        {
            // We have overshot the required populationSize. Adjust target sizes down to compensate.
            // Determine each specie's relative probability of target size downward adjustment.
            double[] probabilities = new double[specieCount];
            for (int i = 0; i < specieCount; i++)
            {
                SpecieStats inst = specieStatsArr[i];
                probabilities[i] = System.Math.Max(0.0, (double)inst._targetSizeInt - inst._targetSizeReal);
            }

            // Use a built in class for choosing an item based on a list of relative probabilities.
            DiscreteDistribution dist = new DiscreteDistribution(probabilities);

            // Probabilistically decrement specie target sizes.
            // ENHANCEMENT: We can improve the selection fairness by updating the DiscreteDistribution 
            // after each decrement (to reflect that decrement).
            for (int i = 0; i < targetSizeDeltaInt;)
            {
                int specieIdx = DiscreteDistribution.Sample(_rng, dist);

                // Skip empty species. This can happen because the same species can be selected more than once.
                if (0 != specieStatsArr[specieIdx]._targetSizeInt)
                {
                    specieStatsArr[specieIdx]._targetSizeInt--;
                    i++;
                }
            }
        }

        // We now have Sum(_targetSizeInt) == _populationSize. 
        //Debug.Assert(SumTargetSizeInt(specieStatsArr) == populationSize);

        // TODO: Better way of ensuring champ species has non-zero target size?
        // However we need to check that the specie with the best genome has a non-zero targetSizeInt in order
        // to ensure that the best genome is preserved. A zero size may have been allocated in some pathological cases.
        if (0 == specieStatsArr[_bestSpecieIdx]._targetSizeInt)
        {
            specieStatsArr[_bestSpecieIdx]._targetSizeInt++;

            // Adjust down the target size of one of the other species to compensate.
            // Pick a specie at random (but not the champ specie). Note that this may result in a specie with a zero 
            // target size, this is OK at this stage. We handle allocations of zero in PerformOneGeneration().
            int idx = _rng.Next(specieCount - 1);
            idx = idx == _bestSpecieIdx ? idx + 1 : idx;

            if (specieStatsArr[idx]._targetSizeInt > 0)
            {
                specieStatsArr[idx]._targetSizeInt--;
            }
            else
            {
                // Scan forward from this specie to find a suitable one.
                bool done = false;
                idx++;
                for (; idx < specieCount; idx++)
                {
                    if (idx != _bestSpecieIdx && specieStatsArr[idx]._targetSizeInt > 0)
                    {
                        specieStatsArr[idx]._targetSizeInt--;
                        done = true;
                        break;
                    }
                }

                // Scan forward from start of species list.
                if (!done)
                {
                    for (int i = 0; i < specieCount; i++)
                    {
                        if (i != _bestSpecieIdx && specieStatsArr[i]._targetSizeInt > 0)
                        {
                            specieStatsArr[i]._targetSizeInt--;
                            done = true;
                            break;
                        }
                    }

                    if (!done)
                    {
                        throw new SharpNeatException(
                            "CalcSpecieStats(). Error adjusting target population size down. Is the population size less than or equal to the number of species?");
                    }
                }
            }
        }

        // Now determine the eliteSize for each specie. This is the number of genomes that will remain in a 
        // specie from the current generation and is a proportion of the specie's current size.
        // Also here we calculate the total number of offspring that will need to be generated.
        offspringCount = 0;
        for (int i = 0; i < specieCount; i++)
        {
            // Special case - zero target size.
            if (0 == specieStatsArr[i]._targetSizeInt)
            {
                Debug.Log("Target size was zero. Elite size became zero");
                specieStatsArr[i]._eliteSizeInt = 0;
                continue;
            }

            // Discretize the real size with a probabilistic handling of the fractional part.
            double eliteSizeReal = _specieList[i].GenomeList.Count * 0; // _eaParams.ElitismProportion;
            int eliteSizeInt = (int)NumericsUtils.ProbabilisticRound(eliteSizeReal, _rng);

            // Ensure eliteSizeInt is no larger than the current target size (remember it was calculated 
            // against the current size of the specie not its new target size).
            SpecieStats inst = specieStatsArr[i];
            inst._eliteSizeInt = System.Math.Min(eliteSizeInt, inst._targetSizeInt);

            // Ensure the champ specie preserves the champ genome. We do this even if the target size is just 1
            // - which means the champ genome will remain and no offspring will be produced from it, apart from 
            // the (usually small) chance of a cross-species mating.
            /*if (i == _bestSpecieIdx && inst._eliteSizeInt == 0)
            {
                Debug.Assert(inst._targetSizeInt != 0, "Zero target size assigned to champ specie.");
                inst._eliteSizeInt = 1;
            }*/

            // Now we can determine how many offspring to produce for the specie.
            inst._offspringCount = inst._targetSizeInt - inst._eliteSizeInt;
            offspringCount += inst._offspringCount;

            // While we're here we determine the split between asexual and sexual reproduction. Again using 
            // some probabilistic logic to compensate for any rounding bias.
            double offspringAsexualCountReal = (double)inst._offspringCount * OffspringAsexualProportion;
            inst._offspringAsexualCount = (int)NumericsUtils.ProbabilisticRound(offspringAsexualCountReal, _rng);
            inst._offspringSexualCount = inst._offspringCount - inst._offspringAsexualCount;

            // Also while we're here we calculate the selectionSize. The number of the specie's fittest genomes
            // that are selected from to create offspring. This should always be at least 1.
            double selectionSizeReal = _specieList[i].GenomeList.Count * SelectionProportion;
            inst._selectionSizeInt =
                System.Math.Max(1, (int)NumericsUtils.ProbabilisticRound(selectionSizeReal, _rng));
        }

        return specieStatsArr;
    }

    protected void UpdateBestGenome()
    {
        // If all genomes have the same fitness (including zero) then we simply return the first genome.
        NeatGenome bestGenome = null;
        double bestFitness = -1.0;
        int bestSpecieIdx = -1;

        int count = _specieList.Count;
        for (int i = 0; i < count; i++)
        {
            // Get the specie's first genome. Genomes are sorted, therefore this is also the fittest 
            // genome in the specie.
            NeatGenome genome = _specieList[i].GenomeList[0];
            if (genome.EvaluationInfo.Fitness > bestFitness)
            {
                bestGenome = genome;
                bestFitness = genome.EvaluationInfo.Fitness;
                bestSpecieIdx = i;
            }
        }

        //_currentBestGenome = bestGenome;
        _bestSpecieIdx = bestSpecieIdx;
    }


    private NeatGenome CreateOffspring_CrossSpecieMating(uint generation, DiscreteDistribution dist,
        DiscreteDistribution[] distArr,
        DiscreteDistribution rwlSpecies,
        int currentSpecieIdx,
        IList<NeatGenome> genomeList)
    {
        // Select parent from current specie.
        int parent1Idx = DiscreteDistribution.Sample(_rng, dist);

        // Select specie other than current one for 2nd parent genome.
        DiscreteDistribution distSpeciesTmp = rwlSpecies.RemoveOutcome(currentSpecieIdx);
        int specie2Idx = DiscreteDistribution.Sample(_rng, distSpeciesTmp);

        // Select a parent genome from the second specie.
        int parent2Idx = DiscreteDistribution.Sample(_rng, distArr[specie2Idx]);

        // Get the two parents to mate.
        NeatGenome parent1 = genomeList[parent1Idx];
        NeatGenome parent2 = _specieList[specie2Idx].GenomeList[parent2Idx];
        var offspring = parent1.CreateOffspring(parent2, generation);
        metricIdLookup[parent1.Id].childrenThisGen.Add(offspring.Id);
        metricIdLookup[parent2.Id].childrenThisGen.Add(offspring.Id);
        metricIdLookup[parent1.Id].hasHadChildren = true;
        metricIdLookup[parent2.Id].hasHadChildren = true;
        return offspring;
    }

    private Dictionary<uint, Vector3> pointLookup = new Dictionary<uint, Vector3>();

    public CoordinateVector GetPosition<TGenome>(TGenome genome) where TGenome : class, IGenome<TGenome>
    {
        //genome.Id
        //TODO: Cache positions
        KeyValuePair<ulong, double>[] positions = new KeyValuePair<ulong, double>[3];
        var point = pointLookup[genome.Id];
        positions[0] = new KeyValuePair<ulong, double>(0, point.x);
        positions[1] = new KeyValuePair<ulong, double>(1, point.y);
        positions[2] = new KeyValuePair<ulong, double>(2, point.z);
        return new CoordinateVector(positions);
    }
}