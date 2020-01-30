using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.Decoders.Neat;
using SharpNeat.Domains;
using SharpNeat.Genomes.Neat;
using SharpNeat.Network;
using SharpNeat.Phenomes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SimpleGameCreatorEvaluator : MonoBehaviour
{
    public Text proTipText;
    public Text generationLabel;

    public Toggle automaticModeSwitch;
    public Button nextGenerationButton;
    public Button resetButton;


    public Toggle highSpeedMode;
    public GameObject automaticPanel;
    public GameObject interactivePanel;

    public int gamesToCreate  = 1;
    public AbstractGameInstance gamePrefab;
    int gameSq = 1;

    IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder;
    IGenomeFactory<NeatGenome> genomeFactory;
    List<NeatGenome> genomeList;
    List<AbstractGameInstance> games;
    public Camera mainCam;
    public bool interactiveMode = false;
    int currentTextIndex = 0;
    int textChangeSeconds = 5;
    float textChangeTimer = 0;
    uint generation = 0;

    string[] proTips = { "The background graphs show the neural networks used by the agents",
        "Left click on a game to zoom in and view details about the neural network",
        "The left player is controlled by a neural net, the right player is a human authored agent"};

    int automaticGenerationSeconds = 10;
    float generationTimer = 0;
    private void Awake()
    {
        proTipText.text = proTips[0];
        automaticPanel.SetActive(!interactiveMode);
        interactivePanel.SetActive(interactiveMode);

        nextGenerationButton.onClick.AddListener(() => { Debug.Log("NEW GENERATION"); NewGeneration(); });
        resetButton.onClick.AddListener(() => { InitialisePopulation(); });

        automaticModeSwitch.onValueChanged.AddListener((newValue) => {
            automaticPanel.SetActive(!newValue);
            interactivePanel.SetActive(newValue);
            interactiveMode = newValue;
            generationTimer = 0;
            paused = false;
        });
        

        NeatGenomeParameters _neatGenomeParams = new NeatGenomeParameters();
        NetworkActivationScheme _activationScheme = NetworkActivationScheme.CreateAcyclicScheme();
        _neatGenomeParams.FeedforwardOnly = _activationScheme.AcyclicNetwork;
        _neatGenomeParams.ActivationFn = SharpNeat.Network.LeakyReLU.__DefaultInstance;// LeakyReLU.__DefaultInstance;
        //_neatGenomeParams.ConnectionWeightMutationProbability = .3;
        _neatGenomeParams.AddNodeMutationProbability = .2;
        _neatGenomeParams.AddConnectionMutationProbability = .4;

        //_neatGenomeParams.DeleteConnectionMutationProbability = .1;
        genomeFactory = new NeatGenomeFactory(gamePrefab.InputCount, gamePrefab.OutputCount, _neatGenomeParams);
        

        genomeDecoder = new NeatGenomeDecoder(_activationScheme);
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
            var node = genome.NodeList[i++%genome.NodeList.Count] as NeuronGene;
            if (processedNodes.Contains(node.Id))
                continue;

            if (node.NodeType == NodeType.Bias ){
                layers[0].Add(node.Id);

                processedNodes.Add(node.Id);
                continue;
            }

           if(node.NodeType == NodeType.Input)
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
            foreach(var nodeId in node.SourceNeurons)
            {
                if(!processedNodes.Contains(nodeId))
                {
                    shouldSkip = true;
                    break;
                }
                for(int j = 0; j < layers.Count; j++)
                {
                    if(layers[j].Contains(nodeId))
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
            if(highestSourceLayer+1 >= layers.Count)
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
        while(!done)
        {
            if(gameSq * gameSq < gamesToCreate)
            {
                gameSq++;
            }
            else
            {
                done = true;
            }
        }
        var min  = mainCam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        var max= mainCam.ViewportToWorldPoint(new Vector3(1, 1, 0));
        var xAdj = (max.x - min.x) / gameSq;
        var yAdj = (max.y - min.y) / gameSq;
        Vector3 offset = new Vector3(((gameSq - 1)/2.0f) * -xAdj, ((gameSq - 1)/2.0f) * -yAdj);
        for (int i = 0; i < gamesToCreate; i++)
        {
            var x = i % gameSq;
            var y = (gamesToCreate - (i + 1)) / gameSq;
            var instance = GameObject.Instantiate<AbstractGameInstance>(gamePrefab);
            instance.transform.localScale = new Vector3(100.0f / gameSq, 100.0f / gameSq, 1);
            instance.transform.position = new Vector3(xAdj * x, yAdj*y) + offset;
            games.Add(instance);
        }
        InitialisePopulation();
    }

    void InitialisePopulation()
    {
        if(update != null)
            StopCoroutine(update);
        generationTimer = 0;
        generation = 0;
        paused = false;
        genomeList = genomeFactory.CreateGenomeList(gamesToCreate, 0);
        for (int i = 0; i < gamesToCreate; i++)
        {
            SetupGame(i);
        }        
    }

    public void NewGeneration()
    {
        generationTimer = 0;
        paused = false;
        List<NeatGenome> selected = new List<NeatGenome>();
        for(int i = 0; i < genomeList.Count; i++)
        {
            if (games[i].selected)
                selected.Add(genomeList[i]);
        }

         if(selected.Count == 0)
        {
            return;
        }
        generation++;
        genomeList.Clear();
        genomeList.AddRange(selected);
        while(genomeList.Count < gamesToCreate)
        {
            int a  = Random.Range(0, selected.Count);
            int b = Random.Range(0, selected.Count);
            if(a==b)
            {
                genomeList.Add(selected[a].CreateOffspring(generation));                
            }
            else
            {
                genomeList.Add(selected[a].CreateOffspring(selected[b],generation));
            }
        }
        for (int i = 0; i < gamesToCreate; i++)
        {
            SetupGame(i);
        }
    }

    private void SetupGame(int i)
    {
        games[i].SetEvolvedBrain(genomeDecoder.Decode(genomeList[i]));
        games[i].SetGraph(GenerateGraph(genomeList[i]));
        games[i].FullReset();
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
        foreach(int gameIndex in gamesToBeSelected)
        {
            games[gameIndex].ToggleSelect();

            yield return new WaitForSeconds(0.5f);
            if (interactiveMode)
            {
                yield break;
            }
        }
        yield return new WaitForSeconds(2);
        if(interactiveMode)
        {
            yield break;
        }
        NewGeneration();
    }

    int[] CreateRandomIndexOrder(int count)
    {
        int[] ts = new int[count];
        for(int i= 0; i< count; i++)
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


    void HandleAutomaticUpdate()
    {
        Time.timeScale = highSpeedMode.isOn ? 3 : 1;
        if(paused)
        {
            return;
        }
        generationTimer += Time.deltaTime;
        
        if(generationTimer > automaticGenerationSeconds)
        {
            paused = true;
            Dictionary<int, float> fitnesses = new Dictionary<int, float>();
            //Calculate the fitnesses of each game instance
            for(int i =0; i< games.Count; i++)
            {
                AbstractGameInstance gi = games[i];
                fitnesses[i] = gi.CalculateFitness();
            }

            List<int> selected = new List<int>();
            for (int j = 0; j < 5; j++)
            {
                int[] indexes = CreateRandomIndexOrder(games.Count);
                float highestFitness = float.MinValue;
                int bestFitness = -1;
                for (int i = 0; i < games.Count; i++)
                {
                    if (selected.Contains(indexes[i]))
                    {
                        continue;
                    }
                    if(bestFitness == -1 || fitnesses[indexes[i]] > highestFitness)
                    {
                        highestFitness = fitnesses[indexes[i]];
                        bestFitness = indexes[i];
                    }
                }
                selected.Add(bestFitness);
            }
            update = StartCoroutine(AnimateAutomaticSelectionProcess(selected));
        }

    }
    Coroutine update = null;

    // Update is called once per frame
    void Update()
    {
        generationLabel.text = "Generation: " + generation;
        proTipText.color = new Color(1,1,1,0.8f+0.2f*    Mathf.Sin(3*Time.unscaledTime));
        textChangeTimer += Time.unscaledDeltaTime;
        if(textChangeTimer > textChangeSeconds)
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
}
