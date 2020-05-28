using SharpNeat.Genomes.Neat;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;

public class LoadGenomeManager : MonoBehaviour
{
    public TextAsset[] genomesToLoad;
    GameCreator creator;


    int focusIndex = 0;
    // Start is called before the first frame update
    void Awake()
    {
        creator = FindObjectOfType<GameCreator>();
        creator.focusGameIndexOverride = FocusGame;
        creator.focusNextOverride = NextGame;
        creator.focusPrevOverride = PrevGame;
        creator.OnNewGeneration.AddListener(NewGen);
    }

    private void NewGen()
    {
        AbstractGameInstance[] instances = FindObjectsOfType<AbstractGameInstance>();

        for (int i = 0; i < genomesToLoad.Length; i++) 
        {
            XmlDocument Doc = new XmlDocument();
            Doc.LoadXml(genomesToLoad[i].text);
            var genomeList = NeatGenomeXmlIO.LoadCompleteGenomeList(Doc.DocumentElement, false, (NeatGenomeFactory)creator.genomeFactory);
            var g = genomeList[0];
            creator.SetupGame(i, g, true);
        }
    }

    int FocusGame()
    {
        return focusIndex;
    }


    void NextGame()
    {
        focusIndex = (focusIndex + 1) % creator.gamesToCreate;
        AbstractGameInstance[] instances = FindObjectsOfType<AbstractGameInstance>();
        foreach(var instance in instances)
        {
            instance.FullReset();
        }
    }

    void PrevGame()
    {
        focusIndex = (creator.gamesToCreate + focusIndex - 1) % creator.gamesToCreate;

        AbstractGameInstance[] instances = FindObjectsOfType<AbstractGameInstance>();
        foreach (var instance in instances)
        {
            instance.FullReset();
        }
    }
}
