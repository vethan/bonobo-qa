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

public class GameCreator : MonoBehaviour
{

    public int gamesToCreate  = 1;
    public GameInstance gamePrefab;
    int gameSq = 1;
    int InputCount = 6;
    int OutputCount = 2;
    IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder;
    IGenomeFactory<NeatGenome> genomeFactory;
    List<NeatGenome> genomeList;
    List<GameInstance> games;
    public Camera mainCam;
    private void Awake()
    {
        NeatGenomeParameters _neatGenomeParams = new NeatGenomeParameters();
        NetworkActivationScheme _activationScheme = NetworkActivationScheme.CreateAcyclicScheme();
        _neatGenomeParams.FeedforwardOnly = _activationScheme.AcyclicNetwork;
        _neatGenomeParams.ActivationFn = SharpNeat.Network.LeakyReLU.__DefaultInstance;// LeakyReLU.__DefaultInstance;
        //_neatGenomeParams.ConnectionWeightMutationProbability = .3;
        _neatGenomeParams.AddNodeMutationProbability = .3;
        _neatGenomeParams.AddConnectionMutationProbability = .3;
        //_neatGenomeParams.DeleteConnectionMutationProbability = .1;
        genomeFactory = new NeatGenomeFactory(InputCount, OutputCount, _neatGenomeParams);
        

        genomeDecoder = new NeatGenomeDecoder(_activationScheme);
    }
    public struct Graph
    {
        public List<List<uint>> layers;
        public List<System.Tuple<uint, uint>> connections;
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
        games = new List<GameInstance>();
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
            var instance = GameObject.Instantiate<GameInstance>(gamePrefab);
            instance.transform.localScale = new Vector3(100.0f / gameSq, 100.0f / gameSq, 1);
            instance.transform.position = new Vector3(xAdj * x, yAdj*y) + offset;
            games.Add(instance);
        }
        InitialisePopulation();
    }

    void InitialisePopulation()
    {        
        genomeList = genomeFactory.CreateGenomeList(gamesToCreate, 0);
        for (int i = 0; i < gamesToCreate; i++)
        {
            SetupGame(i);
        }
    }
    uint generation = 0;
    public void NewGeneration()
    {
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
        games[i].evolved.SetBrain(genomeDecoder.Decode(genomeList[i]));
        games[i].SetGraph(GenerateGraph(genomeList[i]));
        games[i].FullReset();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(1))
        {
            var mousePos = Input.mousePosition;
            var ray = mainCam.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit raycastHit))
            {
                var instance = raycastHit.collider.GetComponent<GameInstance>();
                if(instance != null)
                {
                    instance.ToggleSelect();
                    Debug.Log("Hit game: " + instance);
                }
            }
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            InitialisePopulation();
        }
        if(Input.GetKeyDown(KeyCode.Space))
        {
            NewGeneration();
        }
    }
}
