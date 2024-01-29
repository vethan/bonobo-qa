﻿using System;
using Redzen.Numerics;
using Redzen.Numerics.Distributions;
using Redzen.Random;
using Redzen.Sorting;
using SharpNeat;
using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.Decoders.Neat;
using SharpNeat.DistanceMetrics;
using SharpNeat.Domains;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.Neat;
using SharpNeat.Network;
using SharpNeat.Phenomes;
using SharpNeat.SpeciationStrategies;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameCreator : MonoBehaviour
{
    #region sharpNEAT variables

    //ISpeciationStrategy<NeatGenome> _speciationStrategy;
    IList<Specie<NeatGenome>> _specieList;

    int _bestSpecieIdx;
    IRandomSource _rng = RandomDefaults.CreateRandomSource();

    NeatEvolutionAlgorithmParameters _eaParams;
    NeatEvolutionAlgorithmParameters _eaParamsComplexifying;
    NeatEvolutionAlgorithmParameters _eaParamsSimplifying;

    ComplexityRegulationMode _complexityRegulationMode;
    IComplexityRegulationStrategy _complexityRegulationStrategy;

    internal AbstractGameInstance GetGame(int i)
    {
        return games[i];
    }

    NeatAlgorithmStats _stats;

    protected NeatGenome _currentBestGenome;

    #endregion

    public Rect gameDisplayRect = new Rect(0, 0, .87f, .87f);

    public Text proTipText;
    public Text generationLabel;

    public Toggle automaticModeSwitch;
    public Button nextGenerationButton;
    public Button resetButton;

    public Button focussedNext;
    public Button focussedPrev;
    public Text focussedSpeciesText;
    int focusSpecies;

    public Toggle highSpeedMode;
    public Toggle pauseEvolution;
    public Toggle focusedView;

    public GameObject automaticPanel;
    public GameObject interactivePanel;
    public GameObject focusPanel;
    public bool inspectionMode;
    public int gamesToCreate = 1;

    public int gamesToShow = 4;
    public AbstractGameInstance gamePrefab;
    int gameSq = 1;
    int gameShowSq = 1;

    IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder;
    public IGenomeFactory<NeatGenome> genomeFactory;
    List<NeatGenome> genomeList;
    List<AbstractGameInstance> games;
    public Camera mainCam;
    public bool interactiveMode = false;
    int currentTextIndex = 0;
    int textChangeSeconds = 5;
    float textChangeTimer = 0;
    uint generation = 0;
    double avgComplexity;
    public UnityEngine.Events.UnityEvent OnNewGeneration = new UnityEngine.Events.UnityEvent();
    Dictionary<NeatGenome, int> displayedGames = new Dictionary<NeatGenome, int>();

    public System.Func<int> focusGameIndexOverride = null;
    public System.Action focusNextOverride = null;

    public System.Action focusPrevOverride = null;

    string[] proTips =
    {
        "The background graphs show the neural networks used by the agents",
        "Left click on a game to zoom in and view details about the neural network",
        "The left player is controlled by a neural net, the right player is a human authored agent"
    };


    const int AUTOMATIC_GEN_SECONDS = 30;
    public bool pauseAutoAtTargetGeneration = true;
    public int targetGeneration = 200;
    float generationTimer = 0;

    private ScatterPlot scatterPlot;
    private GenomeRepository repository;
    private DateTime startTime;
    private float topSpeedScale = 25;
    private bool shouldCycle = true;

    private void Awake()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        var parser = new Parser(with =>
        {
            with.IgnoreUnknownArguments = true;
            with.AllowMultiInstance = true;
        });
        var result = parser.ParseArguments<CommandLineOptions>(args)
            .WithParsed(options =>
            {
                {
                    targetGeneration = (int)options.gens;
                    Debug.Log("Setting gens to " + targetGeneration);
                }
                shouldCycle = options.selectmode == 0;
                switch (options.selectmode)
                {
                    case 0:
                    case 1:
                        currentSelectionType = GenomeRepository.SelectionType.Curisoity;
                        break;
                    case 2:
                        currentSelectionType = GenomeRepository.SelectionType.Novelty;
                        break;
                    case 3:
                        currentSelectionType = GenomeRepository.SelectionType.Uniform;
                        break;
                }

                {
                    gamesToCreate = (int)options.population;
                    if (gamesToCreate <= 4)
                    {
                        topSpeedScale = 80;
                    }
                    else if (gamesToCreate <= 8)
                    {
                        topSpeedScale = 50;
                    }
                    else if (gamesToCreate <= 16)
                    {
                        topSpeedScale = 25;
                    }
                    else if (gamesToCreate <= 32)
                    {
                        topSpeedScale = 16;
                    }
                    else
                    {
                        topSpeedScale = 10;
                    }

                    Debug.Log("Setting population to " + gamesToCreate);
                }
                this.highSpeedMode.isOn = true;
            }) // options is an instance of Options type
            .WithNotParsed(errors =>
            {
                Debug.Log("FAILED PARSE");
                foreach (var error in errors)
                {
                    print(error.ToString());
                }
            });
        bootOptions = result.Value;
        if (result.Value.neat != 0)
        {
            this.enabled = false;
            return;
        }

        if (gamesToShow > gamesToCreate)
        {
            gamesToShow = gamesToCreate;
        }

        scatterPlot = new GameObject("ScatterPlot").AddComponent<ScatterPlot>();
        scatterPlot.transform.position = new Vector3(-2000, -2000, 0);
        startTime = DateTime.Now;
        if (inspectionMode)
        {
            interactiveMode = false;
            pauseEvolution.isOn = true;
            focusedView.isOn = true;
            SetFocussedSpeciesAsFirst();
            automaticModeSwitch.gameObject.SetActive(false);
            pauseEvolution.gameObject.SetActive(false);
            focusedView.gameObject.SetActive(false);
            highSpeedMode.gameObject.SetActive(false);
            proTips = new[] { "Please look through these samples" };
            generationLabel.gameObject.SetActive(false);
            resetButton.gameObject.SetActive(false);
        }

        /*  if (bootOptions.behaviour == 0)
          {
              _speciationStrategy = new KMeansClusteringStrategy<NeatGenome>(new EuclideanDistanceMetric());
          }
          else
          {
              _speciationStrategy =
                  new KMeansBehaviourClusteringStrategy<NeatGenome>(repository, new EuclideanDistanceMetric());
          }*/
        _complexityRegulationMode = ComplexityRegulationMode.Complexifying;
        //_complexityRegulationStrategy = new DefaultComplexityRegulationStrategy(ComplexityCeilingType.Relative, 4);
        _complexityRegulationStrategy = new NullComplexityRegulationStrategy();
        _eaParams = new NeatEvolutionAlgorithmParameters();
        _eaParams.SpecieCount = 1;
        _eaParams.ElitismProportion = 0.20;
        _eaParamsSimplifying = _eaParams.CreateSimplifyingParameters();

        _eaParamsComplexifying = _eaParams;

        _stats = new NeatAlgorithmStats(_eaParams);


        proTipText.text = proTips[0];
        automaticPanel.SetActive(!interactiveMode);
        interactivePanel.SetActive(interactiveMode);
        focusPanel.SetActive(inspectionMode);
        nextGenerationButton.onClick.AddListener(() =>
        {
            Debug.Log("NEW GENERATION");
            NewGeneration();
        });
        resetButton.onClick.AddListener(() => { InitialisePopulation(); });

        automaticModeSwitch.onValueChanged.AddListener((newValue) =>
        {
            automaticPanel.SetActive(!newValue);
            interactivePanel.SetActive(newValue);
            interactiveMode = newValue;
            _specieList = null;
            generationTimer = 0;
            paused = false;
        });


        focusedView.onValueChanged.AddListener((newValue) =>
        {
            focusPanel.SetActive(newValue);
            SetFocussedSpeciesAsFirst();
            RepositionGames();
        });

        focussedNext.onClick.AddListener(() =>
        {
            if (focusNextOverride != null)
            {
                focusNextOverride();
            }
            else
            {
                NextSpecies();
            }

            RepositionGames();
        });

        focussedPrev.onClick.AddListener(() =>
        {
            if (focusPrevOverride != null)
            {
                focusPrevOverride();
            }
            else
            {
                PrevSpecies();
            }

            RepositionGames();
        });
        NeatGenomeParameters _neatGenomeParams = new NeatGenomeParameters();
        NetworkActivationScheme _activationScheme = NetworkActivationScheme.CreateAcyclicScheme();
        _neatGenomeParams.FeedforwardOnly = _activationScheme.AcyclicNetwork;

        _neatGenomeParams.ActivationFn = SharpNeat.Network.LeakyReLU.__DefaultInstance; // LeakyReLU.__DefaultInstance;
        //_neatGenomeParams.InitialInterconnectionsProportion = 0.1;
        //_neatGenomeParams.ConnectionWeightMutationProbability = .3;
        //_neatGenomeParams.AddNodeMutationProbability = .2;
        //_neatGenomeParams.AddConnectionMutationProbability = .4;

        //_neatGenomeParams.DeleteConnectionMutationProbability = .1;
        genomeFactory = new NeatGenomeFactory(gamePrefab.InputCount, gamePrefab.OutputCount, _neatGenomeParams);

        genomeDecoder = new NeatGenomeDecoder(_activationScheme);
    }

    void SetFocussedSpeciesAsFirst()
    {
        focusSpecies = 0;
        if (_specieList == null)
            return;
        for (int i = 0; i < _specieList.Count; i++)
        {
            if (_specieList[i].GenomeList.Count > 0)
            {
                focusSpecies = i;
                return;
            }
        }
    }

    void NextSpecies()
    {
        for (int i = 1; i < _specieList.Count; i++)
        {
            int index = (focusSpecies + i) % _specieList.Count;
            if (_specieList[index].GenomeList.Count > 0)
            {
                focusSpecies = index;
                return;
            }
        }
    }

    void PrevSpecies()
    {
        for (int i = 1; i < _specieList.Count; i++)
        {
            int index = (focusSpecies + _specieList.Count - i) % _specieList.Count;
            if (_specieList[index].GenomeList.Count > 0)
            {
                focusSpecies = index;
                return;
            }
        }
    }

    Graph GenerateGraph(NeatGenome genome)
    {
        List<List<uint>> layers = new List<List<uint>>();
        List<uint> outputLayer = new List<uint>();
        layers.Add(new List<uint>());
        List<uint> processedNodes = new List<uint>();
        List<System.Tuple<uint, uint>> connections = new List<System.Tuple<uint, uint>>();
        int i = 0;
        while (processedNodes.Count < genome.NodeList.Count)
        {
            var node = genome.NodeList[i++ % genome.NodeList.Count] as NeuronGene;
            if (processedNodes.Contains(node.Id))
                continue;

            if (node.NodeType == NodeType.Bias)
            {
                layers[0].Add(node.Id);

                processedNodes.Add(node.Id);
                continue;
            }

            if (node.NodeType == NodeType.Input)
            {
                processedNodes.Add(node.Id);
                layers[0].Add(node.Id);
                continue;
            }

            if (node.NodeType == NodeType.Output)
            {
                foreach (var nodeId in node.SourceNeurons)
                {
                    connections.Add(new System.Tuple<uint, uint>(nodeId, node.Id));
                }

                processedNodes.Add(node.Id);
                outputLayer.Add(node.Id);
                continue;
            }

            int highestSourceLayer = 0;
            bool shouldSkip = false;
            foreach (var nodeId in node.SourceNeurons)
            {
                if (!processedNodes.Contains(nodeId))
                {
                    shouldSkip = true;
                    break;
                }

                for (int j = 0; j < layers.Count; j++)
                {
                    if (layers[j].Contains(nodeId))
                    {
                        if (j > highestSourceLayer)
                            highestSourceLayer = j;
                    }
                }
            }

            foreach (var nodeId in node.SourceNeurons)
            {
                connections.Add(new System.Tuple<uint, uint>(nodeId, node.Id));
            }

            if (shouldSkip)
            {
                continue;
            }

            if (highestSourceLayer + 1 >= layers.Count)
            {
                layers.Add(new List<uint>());
            }

            layers[highestSourceLayer + 1].Add(node.Id);
            processedNodes.Add(node.Id);
        }

        layers.Add(outputLayer);
        return new Graph() { layers = layers, connections = connections };
    }

    // Start is called before the first frame update
    void Start()
    {
        games = new List<AbstractGameInstance>();
        bool done = false;
        while (!done)
        {
            if (gameSq * gameSq < gamesToCreate)
            {
                gameSq++;
            }
            else
            {
                done = true;
            }
        }

        done = false;
        while (!done)
        {
            if (gameShowSq * gameShowSq < gamesToShow)
            {
                gameShowSq++;
            }
            else
            {
                done = true;
            }
        }


        for (int i = 0; i < gamesToCreate; i++)
        {
            games.Add(GameObject.Instantiate<AbstractGameInstance>(gamePrefab));
        }

        InitialisePopulation();
        RepositionGames(true);
    }

    class SpeciesRecord : System.IComparable<SpeciesRecord>
    {
        public int speciesIndex;
        public double bestFitness;
        public NeatGenome genome;

        public int CompareTo(object obj)
        {
            if (obj is SpeciesRecord)
            {
                CompareTo((SpeciesRecord)obj);
            }

            return bestFitness.CompareTo(obj);
        }

        public int CompareTo(SpeciesRecord other)
        {
            return -bestFitness.CompareTo(other.bestFitness);
        }
    }

    void SelectDisplayedGamesBasedOnSpecies()
    {
        displayedGames.Clear();
        for (int i = 0; i < gamesToShow; i++)
        {
            displayedGames[genomeList[i]] = i;
        }

        RepositionGames();
    }

    void RepositionGames(bool reposition = false)
    {
        var xDisplayAdj = gameDisplayRect.width / gameShowSq;
        var yDisplayAdj = gameDisplayRect.height / gameShowSq;

        var min = mainCam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        var max = mainCam.ViewportToWorldPoint(new Vector3(1, 1, 0));

        var xAdj = (max.x - min.x);
        var yAdj = (max.y - min.y);
        Vector3 offset = Vector3.zero;

        //Focus on the species leader
        if (focusedView.isOn)
        {
            NeatGenome focusGenome;
            if (focusGameIndexOverride != null)
            {
                focusGenome = genomeList[focusGameIndexOverride() % genomeList.Count];
            }
            else
            {
                if (_specieList[focusSpecies].GenomeList.Count == 0)
                    SetFocussedSpeciesAsFirst();

                focusGenome = _specieList[focusSpecies].GenomeList[0];
            }

            for (int i = 0; i < gamesToCreate; i++)
            {
                var instance = games[i];
                if (focusGenome.Equals(genomeList[i]))
                {
                    instance.displayCam.rect = gameDisplayRect;
                    instance.displayCam.enabled = true;
                }
                else
                {
                    instance.displayCam.enabled = false;
                    instance.zoomCam.enabled = false;
                }

                if (reposition)
                {
                    var x = (i) % gameSq;
                    var y = (gamesToCreate - ((i) + 1)) / gameSq;
                    PositionGame(instance, xAdj, yAdj, x, y, offset);
                }
            }

            return;
        }

        //Not in focus mode


        for (int i = 0; i < gamesToCreate; i++)
        {
            var instance = games[i];
            if (displayedGames.ContainsKey(genomeList[i]))
            {
                int index = displayedGames[genomeList[i]];
                var x = index % gameShowSq;
                var y = (gamesToShow - (index + 1)) / gameShowSq;

                instance.displayCam.rect = new Rect(xDisplayAdj * x, yDisplayAdj * y, xDisplayAdj, yDisplayAdj);
                instance.displayCam.enabled = true;
            }
            else
            {
                instance.displayCam.enabled = false;
                instance.zoomCam.enabled = false;
            }

            if (reposition)
            {
                var x = i % gameSq;
                var y = (gamesToCreate - (i + 1)) / gameSq;
                PositionGame(instance, xAdj, yAdj, x, y, offset);
            }
        }
    }

    public void PositionGame(AbstractGameInstance instance, float xAdj, float yAdj, float x, float y, Vector3 offset)
    {
        instance.transform.localScale = new Vector3(100.0f, 100.0f, 1);
        instance.transform.position = new Vector3(xAdj * 2 * x, yAdj * 2 * y) + offset;
    }

    void InitialisePopulation()
    {
        repository?.Dispose();
        repository = new GenomeRepository(300, scatterPlot, _rng, bootOptions.behaviour == 1, true,
            gamePrefab.GameName + " p" + gamesToCreate.ToString() + " g" + targetGeneration.ToString() + " s" + bootOptions.species +
            (bootOptions.behaviour == 0 ? " " : " behav ") +
            currentSelectionType.ToString(),
            runNumberForType, startTime, bootOptions.species);


        if (update != null)
            StopCoroutine(update);
        generationTimer = 0;
        generation = 0;
        paused = false;


        genomeList = genomeFactory.CreateGenomeList(gamesToCreate, 0);
        for (int i = 0; i < gamesToShow; i++)
        {
            displayedGames[genomeList[i]] = i;
        }

        for (int i = 0; i < gamesToCreate; i++)
        {
            SetupGame(i);
        }

        OnNewGeneration.Invoke();
    }

    public void NewGeneration()
    {
        generationTimer = 0;
        paused = false;
        List<NeatGenome> selected = new List<NeatGenome>();
        for (int i = 0; i < genomeList.Count; i++)
        {
            if (games[i].selected)
                selected.Add(genomeList[i]);
        }

        if (selected.Count == 0)
        {
            return;
        }

        generation++;
        genomeList.Clear();
        genomeList.AddRange(selected);
        while (genomeList.Count < gamesToCreate)
        {
            int a = Random.Range(0, selected.Count);
            int b = Random.Range(0, selected.Count);
            if (a == b)
            {
                genomeList.Add(selected[a].CreateOffspring(generation));
            }
            else
            {
                genomeList.Add(selected[a].CreateOffspring(selected[b], generation));
            }
        }

        for (int i = 0; i < gamesToCreate; i++)
        {
            SetupGame(i);
        }
    }

    public void SetupGame(int i, NeatGenome genome, bool hideGraph = false)
    {
        games[i].SetEvolvedBrain(genomeDecoder.Decode(genome), genome);


        games[i].SetGraph(GenerateGraph(genome));

        if (hideGraph)
        {
            games[i].DisableGraph();
        }

        games[i].FullReset();
    }

    private void SetupGame(int i)
    {
        SetupGame(i, genomeList[i]);
    }

    void HandleInteractiveUpdate()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log("Clickerd");
            var mousePos = Input.mousePosition;
            var ray = mainCam.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit raycastHit))
            {
                Debug.Log("Hit");

                var instance = raycastHit.collider.GetComponent<GameInstance>();
                if (instance != null)
                {
                    instance.ToggleSelect();
                }
            }
        }
    }

    bool paused = false;

    IEnumerator AnimateAutomaticSelectionProcess(List<int> gamesToBeSelected)
    {
        foreach (int gameIndex in gamesToBeSelected)
        {
            games[gameIndex].ToggleSelect();

            yield return new WaitForSeconds(0.5f);
            if (interactiveMode)
            {
                yield break;
            }
        }

        yield return new WaitForSeconds(2);
        if (interactiveMode)
        {
            yield break;
        }

        NewGeneration();
    }

    int[] CreateRandomIndexOrder(int count)
    {
        int[] ts = new int[count];
        for (int i = 0; i < count; i++)
        {
            ts[i] = i;
        }

        var last = count - 1;
        for (var i = 0; i < last; ++i)
        {
            var r = UnityEngine.Random.Range(i, count);
            var tmp = ts[i];
            ts[i] = ts[r];
            ts[r] = tmp;
        }

        return ts;
    }

    private string statFolder;
    private StreamWriter dataCsvStream;
    private List<string> headers;
    private HashSet<uint> recordedGenomeIds;
    bool emptySpeciesFlag;
    List<NeatGenome> offspringList;


    private void OnDestroy()
    {
        repository?.Dispose();
    }


    void HandleAutomaticUpdate()
    {
        Time.timeScale = highSpeedMode.isOn ? topSpeedScale : 1;
        bool allFinished = true;
        foreach (var game in games)
        {
            allFinished &= game.GameDone;
            if (game.interesting != game.selected)
            {
                game.ToggleSelect();
            }
        }

        generationTimer += Time.deltaTime;

        if (paused || pauseEvolution.isOn || (generationTimer <= AUTOMATIC_GEN_SECONDS && !allFinished))
        {
            return;
        }
        //paused = true;

        //Calculate the fitnesses of each game instance
        for (int i = 0; i < games.Count; i++)
        {
            AbstractGameInstance gi = games[i];
            genomeList[i].EvaluationInfo.SetFitness(Mathf.Max(1000 + gi.CalculateFitness(), 0));
        }

        if (generation > 0)
        {
            var tempRepository = new List<GenomeMetric>(gamesToCreate);
            for (int i = 0; i < games.Count; i++)
            {
                var game = games[i];
                var genome = genomeList[i];
                tempRepository.Add(new GenomeMetric() { metrics = game.GetGameStats(), genome = genome });
            }

            repository.HandleNewGeneration(tempRepository, generation);

            // Integrate offspring into species.
            /*if (emptySpeciesFlag)
            {
                // We have one or more terminated species. Therefore we need to fully re-speciate all genomes to divide them
                // evenly between the required number of species.

                // Clear all genomes from species (we still have the elite genomes in _genomeList).
                ClearAllSpecies();
                Debug.Log("Clearing All Species.");
                // Speciate genomeList.
                _speciationStrategy.SpeciateGenomes(genomeList, _specieList);
            }
            else
            {
                // Integrate offspring into the existing species. 
                _speciationStrategy.SpeciateOffspring(offspringList, _specieList);
            }

            Debug.Assert(!TestForEmptySpecies(_specieList), "Speciation resulted in one or more empty species.");

            // Sort the genomes in each specie. Fittest first (secondary sort - youngest first).
            SortSpecieGenomes();*/

            // Update stats and store reference to best genome.
            //UpdateBestGenome();
            UpdateStats();
            avgComplexity = _stats._meanComplexity;
            //Debug.Log(_stats._maxFitness + ":::" + _stats._meanFitness);
            // Determine the complexity regulation mode and switch over to the appropriate set of evolution
            // algorithm parameters. Also notify the genome factory to allow it to modify how it creates genomes
            // (e.g. reduce or disable additive mutations).
            _complexityRegulationMode = _complexityRegulationStrategy.DetermineMode(_stats);
            genomeFactory.SearchMode = (int)_complexityRegulationMode;
            switch (_complexityRegulationMode)
            {
                case ComplexityRegulationMode.Complexifying:
                    _eaParams = _eaParamsComplexifying;
                    break;
                case ComplexityRegulationMode.Simplifying:
                    _eaParams = _eaParamsSimplifying;
                    break;
            }

            // TODO: More checks.
            Debug.Assert(genomeList.Count == gamesToCreate);
        }
        else
        {
            Debug.Log("FIRST RUN");
            //First!
            var tempRepertoire = new List<GenomeMetric>();
            for (int i = 0; i < games.Count; i++)
            {
                var game = games[i];
                var genome = genomeList[i];
                tempRepertoire.Add(new GenomeMetric() { metrics = game.GetGameStats(), genome = genome });
            }

            repository.Initialise(tempRepertoire);

            //_specieList = _speciationStrategy.InitializeSpeciation(genomeList, _eaParams.SpecieCount);
            //SortSpecieGenomes();
            //UpdateBestGenome();
        }


        // Calculate statistics for each specie (mean fitness, target size, number of offspring to produce etc.)
        if (false)
        {
            //Write data for current gens
            for (int i = 0; i < games.Count; i++)
            {
                AbstractGameInstance gi = games[i];
                var stats = gi.GetGameStats();
                stats["_genomeID"] = genomeList[i].Id;
                stats["_birthGeneration"] = genomeList[i].BirthGeneration;
                stats["_genomeSpecies"] = genomeList[i].SpecieIdx;

                if (dataCsvStream == null)
                {
                    string baseMetricSavePath =
                        Path.Combine(Path.GetDirectoryName(Application.dataPath), "MetricStats");
                    if (!Directory.Exists(baseMetricSavePath))
                    {
                        Directory.CreateDirectory(baseMetricSavePath);
                    }

                    statFolder = Path.Combine(baseMetricSavePath,
                        string.Format(gamePrefab.name + "metrics{0:yyyy-dd-M--HH-mm-ss}", DateTime.Now));
                    if (!Directory.Exists(statFolder))
                    {
                        Directory.CreateDirectory(statFolder);
                    }

                    string filepath = Path.Combine(statFolder, "statistics.csv");
                    recordedGenomeIds = new HashSet<uint>();
                    headers = stats.Keys.ToList();
                    dataCsvStream = new StreamWriter(new FileStream(filepath, FileMode.Create,
                        FileAccess.Write, FileShare.ReadWrite));
                    dataCsvStream.WriteLine(String.Join(",", headers));
                }

                if (!recordedGenomeIds.Contains(genomeList[i].Id))
                {
                    recordedGenomeIds.Add(genomeList[i].Id);
                    dataCsvStream.WriteLine(String.Join(",", headers.Select(x => stats[x])));

                    //Write the genome
                    string filename = string.Format("Genome{0}.xml", genomeList[i].Id);
                    var xmlDoc = NeatGenomeXmlIO.SaveComplete(genomeList[i], false);
                    xmlDoc.Save(Path.Combine(statFolder, filename));
                }

                //print(gi.GetGameStats().PrettyPrint());
            }

            dataCsvStream.Flush();
        }

        // Create offspring.
        genomeList = CreateOffspring(gamesToCreate);

        // Append offspring genomes to the elite genomes in _genomeList. We do this before calling the
        // _genomeListEvaluator.Evaluate because some evaluation schemes re-evaluate the elite genomes 
        // (otherwise we could just evaluate offspringList).


        SelectDisplayedGamesBasedOnSpecies();

        for (int i = 0; i < gamesToCreate; i++)
        {
            SetupGame(i);
        }

        OnNewGeneration.Invoke();
        generation++;
        if (pauseAutoAtTargetGeneration && generation == targetGeneration)
        {
            if (shouldCycle)
            {
                switch (currentSelectionType)
                {
                    case GenomeRepository.SelectionType.Curisoity:
                        currentSelectionType = GenomeRepository.SelectionType.Novelty;
                        break;
                    case GenomeRepository.SelectionType.Novelty:
                        currentSelectionType = GenomeRepository.SelectionType.Uniform;
                        break;
                    default:
                        runNumberForType++;
                        currentSelectionType = GenomeRepository.SelectionType.Curisoity;
                        break;
                }
            }
            else
            {
                runNumberForType++;
            }


            InitialisePopulation();
            //pauseAutoAtTargetGeneration = false;
            //pauseEvolution.isOn = true;
        }

        generationTimer = 0;
        GC.Collect();
    }

    private int runNumberForType = 0;
    private GenomeRepository.SelectionType currentSelectionType = GenomeRepository.SelectionType.Curisoity;
    Coroutine update = null;
    private CommandLineOptions bootOptions;

    public ulong EvaluationCount => throw new System.NotImplementedException();

    public bool StopConditionSatisfied => throw new System.NotImplementedException();

    // Update is called once per frame
    void Update()
    {
        focusedView.isOn &= _specieList != null || focusGameIndexOverride != null;
        focusedView.interactable = _specieList != null || focusGameIndexOverride != null;
        if (_specieList != null || focusGameIndexOverride != null)
        {
            focussedSpeciesText.text = "Sample: " + (focusGameIndexOverride != null
                ? focusGameIndexOverride().ToString()
                : _specieList[focusSpecies].Id.ToString());
        }

        generationLabel.text = "Generation: " + generation;
        proTipText.color = new Color(1, 1, 1, 0.8f + 0.2f * Mathf.Sin(3 * Time.unscaledTime));
        textChangeTimer += Time.unscaledDeltaTime;
        if (textChangeTimer > textChangeSeconds)
        {
            currentTextIndex = (currentTextIndex + 1) % proTips.Length;
            proTipText.text = proTips[currentTextIndex];
            textChangeTimer -= textChangeSeconds;
        }


        if (Input.GetKeyDown(KeyCode.R))
        {
            InitialisePopulation();
        }

        if (interactiveMode)
        {
            HandleInteractiveUpdate();
        }
        else
        {
            HandleAutomaticUpdate();
        }
    }


    #region Private Methods [High Level Algorithm Methods. CalcSpecieStats/CreateOffspring]

    /// <summary>
    /// Calculate statistics for each specie. This method is at the heart of the evolutionary algorithm,
    /// the key things that are achieved in this method are - for each specie we calculate:
    ///  1) The target size based on fitness of the specie's member genomes.
    ///  2) The elite size based on the current size. Potentially this could be higher than the target 
    ///     size, so a target size is taken to be a hard limit.
    ///  3) Following (1) and (2) we can calculate the total number offspring that need to be generated 
    ///     for the current generation.
    /// </summary>
    private SpecieStats[] CalcSpecieStats(out int offspringCount)
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
            double targetSizeReal = (double)gamesToCreate / (double)specieCount;

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
            for (int i = 0; i < specieCount; i++)
            {
                SpecieStats inst = specieStatsArr[i];
                inst._targetSizeReal = (inst._meanFitness / totalMeanFitness) * (double)gamesToCreate;

                // Discretize targetSize (stochastic rounding).
                inst._targetSizeInt = (int)NumericsUtils.ProbabilisticRound(inst._targetSizeReal, _rng);

                // Total up discretized target sizes.
                totalTargetSizeInt += inst._targetSizeInt;
            }
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
        int targetSizeDeltaInt = totalTargetSizeInt - gamesToCreate;

        if (targetSizeDeltaInt < 0)
        {
            // Check for special case. If we are short by just 1 then increment targetSizeInt for the specie containing
            // the best genome. We always ensure that this specie has a minimum target size of 1 with a final test (below),
            // by incrementing here we avoid the probabilistic allocation below followed by a further correction if
            // the champ specie ended up with a zero target size.
            if (-1 == targetSizeDeltaInt)
            {
                specieStatsArr[_bestSpecieIdx]._targetSizeInt++;
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
        Debug.Assert(SumTargetSizeInt(specieStatsArr) == gamesToCreate);

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
                print("Target size was zero. Elite size became zero");
                specieStatsArr[i]._eliteSizeInt = 0;
                continue;
            }

            // Discretize the real size with a probabilistic handling of the fractional part.
            double eliteSizeReal = _specieList[i].GenomeList.Count * _eaParams.ElitismProportion;
            int eliteSizeInt = (int)NumericsUtils.ProbabilisticRound(eliteSizeReal, _rng);

            // Ensure eliteSizeInt is no larger than the current target size (remember it was calculated 
            // against the current size of the specie not its new target size).
            SpecieStats inst = specieStatsArr[i];
            inst._eliteSizeInt = System.Math.Min(eliteSizeInt, inst._targetSizeInt);

            // Ensure the champ specie preserves the champ genome. We do this even if the target size is just 1
            // - which means the champ genome will remain and no offspring will be produced from it, apart from 
            // the (usually small) chance of a cross-species mating.
            if (i == _bestSpecieIdx && inst._eliteSizeInt == 0)
            {
                Debug.Assert(inst._targetSizeInt != 0, "Zero target size assigned to champ specie.");
                inst._eliteSizeInt = 1;
            }

            // Now we can determine how many offspring to produce for the specie.
            inst._offspringCount = inst._targetSizeInt - inst._eliteSizeInt;
            offspringCount += inst._offspringCount;

            // While we're here we determine the split between asexual and sexual reproduction. Again using 
            // some probabilistic logic to compensate for any rounding bias.
            double offspringAsexualCountReal = (double)inst._offspringCount * _eaParams.OffspringAsexualProportion;
            inst._offspringAsexualCount = (int)NumericsUtils.ProbabilisticRound(offspringAsexualCountReal, _rng);
            inst._offspringSexualCount = inst._offspringCount - inst._offspringAsexualCount;

            // Also while we're here we calculate the selectionSize. The number of the specie's fittest genomes
            // that are selected from to create offspring. This should always be at least 1.
            double selectionSizeReal = _specieList[i].GenomeList.Count * _eaParams.SelectionProportion;
            inst._selectionSizeInt =
                System.Math.Max(1, (int)NumericsUtils.ProbabilisticRound(selectionSizeReal, _rng));
        }

        return specieStatsArr;
    }

    /// <summary>
    /// Create the required number of offspring genomes, using specieStatsArr as the basis for selecting how
    /// many offspring are produced from each species.
    /// </summary>
    private List<NeatGenome> CreateOffspring(int offspringCount)
    {
        double offspringAsexualCountReal = (double)offspringCount * _eaParams.OffspringAsexualProportion;
        int offspringAsexualCount = (int)NumericsUtils.ProbabilisticRound(offspringAsexualCountReal, _rng);
        int offspringSexualCount = offspringCount - offspringAsexualCount;
        // Produce offspring from each specie in turn and store them in offspringList.
        List<NeatGenome> tempOffspringList =
            repository.GenerateOffspring(generation, offspringCount, _eaParams.OffspringAsexualProportion,
                _eaParams.SelectionProportion, _eaParams.InterspeciesMatingProportion, currentSelectionType);
        _stats._asexualOffspringCount += (ulong)offspringAsexualCount;
        _stats._sexualOffspringCount += (ulong)(offspringSexualCount);
        _stats._totalOffspringCount += (ulong)offspringCount;
        return tempOffspringList;
    }

    /// <summary>
    /// Cross specie mating.
    /// </summary>
    /// <param name="dist">DiscreteDistribution for selecting genomes in the current specie.</param>
    /// <param name="distArr">Array of DiscreteDistribution objects for genome selection. One for each specie.</param>
    /// <param name="rwlSpecies">DiscreteDistribution for selecting species. Based on relative fitness of species.</param>
    /// <param name="currentSpecieIdx">Current specie's index in _specieList</param>
    /// <param name="genomeList">Current specie's genome list.</param>
    private NeatGenome CreateOffspring_CrossSpecieMating(DiscreteDistribution dist,
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
        return parent1.CreateOffspring(parent2, generation);
    }

    #endregion

    #region Private Methods [Low Level Helper Methods]

    /// <summary>
    /// Updates _currentBestGenome and _bestSpecieIdx, these are the fittest genome and index of the specie
    /// containing the fittest genome respectively.
    /// 
    /// This method assumes that all specie genomes are sorted fittest first and can therefore save much work
    /// by not having to scan all genomes.
    /// Note. We may have several genomes with equal best fitness, we just select one of them in that case.
    /// </summary>
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

        _currentBestGenome = bestGenome;
        _bestSpecieIdx = bestSpecieIdx;
    }

    /// <summary>
    /// Updates the NeatAlgorithmStats object.
    /// </summary>
    private void UpdateStats()
    {
        lock (_stats)
        {
            _stats._generation = generation;
            _stats._totalEvaluationCount = (generation + 1) * (uint)gamesToCreate;

            // Evaluation per second.
            System.DateTime now = System.DateTime.Now;
            System.TimeSpan duration = now - _stats._evalsPerSecLastSampleTime;

            // To smooth out the evals per sec statistic we only update if at least 1 second has elapsed 
            // since it was last updated.
            if (duration.Ticks > 9999)
            {
                long evalsSinceLastUpdate = (long)(_stats._totalEvaluationCount - _stats._evalsCountAtLastUpdate);
                _stats._evaluationsPerSec = (int)((evalsSinceLastUpdate * 1e7) / duration.Ticks);

                // Reset working variables.
                _stats._evalsCountAtLastUpdate = (generation + 1) * (uint)gamesToCreate;
                _stats._evalsPerSecLastSampleTime = now;
            }

            // Fitness and complexity stats.
            double totalFitness = genomeList[0].EvaluationInfo.Fitness;
            double totalComplexity = genomeList[0].Complexity;
            double maxComplexity = totalComplexity;
            double maxFitness = double.MinValue;
            int count = genomeList.Count;
            for (int i = 1; i < count; i++)
            {
                totalFitness += genomeList[i].EvaluationInfo.Fitness;
                if (genomeList[i].EvaluationInfo.Fitness > maxFitness)
                    maxFitness = genomeList[i].EvaluationInfo.Fitness;
                totalComplexity += genomeList[i].Complexity;
                maxComplexity = System.Math.Max(maxComplexity, genomeList[i].Complexity);
            }

            _stats._maxFitness = maxFitness;
            _stats._meanFitness = totalFitness / count;

            _stats._maxComplexity = maxComplexity;
            _stats._meanComplexity = totalComplexity / count;

            // Specie champs mean fitness.
            /* double totalSpecieChampFitness = _specieList[0].GenomeList[0].EvaluationInfo.Fitness;
             int specieCount = _specieList.Count;
             for (int i = 1; i < specieCount; i++)
             {
                 totalSpecieChampFitness += _specieList[i].GenomeList[0].EvaluationInfo.Fitness;
             }
 
             _stats._meanSpecieChampFitness = totalSpecieChampFitness / specieCount;*/

            // Moving averages.
            _stats._prevBestFitnessMA = _stats._bestFitnessMA.Mean;
            _stats._bestFitnessMA.Enqueue(_stats._maxFitness);

            //  _stats._prevMeanSpecieChampFitnessMA = _stats._meanSpecieChampFitnessMA.Mean;
            // _stats._meanSpecieChampFitnessMA.Enqueue(_stats._meanSpecieChampFitness);

            _stats._prevComplexityMA = _stats._complexityMA.Mean;
            _stats._complexityMA.Enqueue(_stats._meanComplexity);
        }
    }

    /// <summary>
    /// Sorts the genomes within each species fittest first, secondary sorts on age.
    /// </summary>
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
        _stats._minSpecieSize = minSize;
        _stats._maxSpecieSize = maxSize;
    }

    /// <summary>
    /// Clear the genome list within each specie.
    /// </summary>
    private void ClearAllSpecies()
    {
        foreach (Specie<NeatGenome> specie in _specieList)
        {
            specie.GenomeList.Clear();
        }
    }

    /// <summary>
    /// Trims the genomeList in each specie back to the number of elite genomes specified in
    /// specieStatsArr. Returns true if there are empty species following trimming.
    /// </summary>
    private bool TrimSpeciesBackToElite(SpecieStats[] specieStatsArr)
    {
        bool emptySpeciesFlag = false;
        int count = _specieList.Count;
        for (int i = 0; i < count; i++)
        {
            Specie<NeatGenome> specie = _specieList[i];
            SpecieStats stats = specieStatsArr[i];

            int removeCount = specie.GenomeList.Count - stats._eliteSizeInt;
            specie.GenomeList.RemoveRange(stats._eliteSizeInt, removeCount);

            if (0 == stats._eliteSizeInt)
            {
                emptySpeciesFlag = true;
            }
        }

        return emptySpeciesFlag;
    }

    #endregion

    #region Private Methods [Debugging]

    /// <summary>
    /// Returns true if there is one or more empty species.
    /// </summary>
    private bool TestForEmptySpecies(IList<Specie<NeatGenome>> specieList)
    {
        foreach (Specie<NeatGenome> specie in specieList)
        {
            if (specie.GenomeList.Count == 0)
            {
                return true;
            }
        }

        return false;
    }


    private static int SumTargetSizeInt(SpecieStats[] specieStatsArr)
    {
        int total = 0;
        foreach (SpecieStats inst in specieStatsArr)
        {
            total += inst._targetSizeInt;
        }

        return total;
    }

    #endregion

    #region InnerClass [SpecieStats]

    /*class SpecieStats
    {
        // Real/continuous stats.
        public double _meanFitness;
        public double _targetSizeReal;

        // Integer stats.
        public int _targetSizeInt;
        public int _eliteSizeInt;
        public int _offspringCount;
        public int _offspringAsexualCount;
        public int _offspringSexualCount;

        // Selection data.
        public int _selectionSizeInt;
    }*/

    #endregion
}