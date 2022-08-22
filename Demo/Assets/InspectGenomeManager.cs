using SharpNeat.Genomes.Neat;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

public class InspectGenomeManager : MonoBehaviour
{
    private List<GenomeFile> genomeOptions;
    public InputField genomeInput;
    public Button jumpButton;
    class GenomeFile
    {
        private int index;
        public string filePath { get; private set; }

        public GenomeFile(string filePath, int index)
        {
            this.filePath = filePath;
            this.index = index;
        }
    }

    public string genomeFolderPath;
    GameCreator creator;

    int focusIndex = 0;

    // Start is called before the first frame update
    void Awake()
    {
        Regex rx = new Regex(@"Genome(?<index>\d+).xml$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        genomeOptions = new List<GenomeFile>();
        var files = Directory.GetFiles(genomeFolderPath);
        foreach (var potentialGenome in files)
        {
            var match = rx.Match(potentialGenome);
            if (!match.Success)
                continue;

            genomeOptions.Add(new GenomeFile(potentialGenome, int.Parse(match.Groups["index"].Value)));
        }

        Debug.Log(genomeOptions.Count);

        creator = FindObjectOfType<GameCreator>();
        creator.focusGameIndexOverride = FocusGame;
        creator.focusNextOverride = NextGame;
        creator.focusPrevOverride = PrevGame;
        creator.OnNewGeneration.AddListener(NewGen);

        jumpButton.onClick.AddListener( () =>
        {
            if (int.TryParse(genomeInput.text, out int jumpTarget))
            {
                if (jumpTarget < genomeOptions.Count && jumpTarget > 0)
                {
                    focusIndex = jumpTarget;
                    NewGen();
                }
            }
            Debug.Log("Things");
        });
    }

    
    
    private void NewGen()
    {
        XmlDocument Doc = new XmlDocument();
        Doc.Load(genomeOptions[focusIndex].filePath);
        var genomeList = NeatGenomeXmlIO.LoadCompleteGenomeList(Doc.DocumentElement, false,
            (NeatGenomeFactory) creator.genomeFactory);
        var g = genomeList[0];
        creator.SetupGame(0, g, true);
    }

    int FocusGame()
    {
        return focusIndex;
    }


    void NextGame()
    {
        focusIndex = (focusIndex + 1) % genomeOptions.Count;
        NewGen();
    }

    void PrevGame()
    {
        focusIndex = (genomeOptions.Count + focusIndex - 1) % genomeOptions.Count;
        NewGen();
    }
}