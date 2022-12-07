using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;
using SharpNeat.Core;
using UnityEngine;
using UnityEngine.Assertions.Comparers;
using Random = UnityEngine.Random;

public class MicrosoftMLTest : MonoBehaviour
{
    private List<GenomeMetric> data;

    // Start is called before the first frame update
    void Start()
    {
        MLContext mlContext = new MLContext();
        data = new List<GenomeMetric>();
        HashSet<string> columnNames = new HashSet<string>();
        for (int i = 0; i < 10; i++)
        {
            Dictionary<string, float> entry = new Dictionary<string, float>();
            for (int j = 0; j < 5; j++)
            {
                entry["datapoint" + j] = Random.value;
                columnNames.Add("datapoint" + j);
            }

            data.Add(new GenomeMetric() {metrics = entry});
        }

        IDataView inData = new GenomeMetricDataView(data, new HashSet<string>());

        print(inData.Preview().ToString());


        IEstimator<ITransformer> dataPrepEstimator = mlContext.Transforms.Concatenate("Features", columnNames.ToArray())
            .Append(mlContext.Transforms.NormalizeMinMax("Features"));
        ITransformer dataPrepTransformer = dataPrepEstimator.Fit(inData);
        IDataView normalisedData = dataPrepTransformer.Transform(inData);
        PrintColumnValues(normalisedData, "Features");


        var pcaEstimator = mlContext.Transforms.ProjectToPrincipalComponents("FeaturesPCA", "Features", null, 2);
        var pcaTranformer = pcaEstimator.Fit(normalisedData);
        var pcaData = pcaTranformer.Transform(normalisedData);

        PrintColumnValues(pcaData, "FeaturesPCA");
    }

   
    
    
    void PrintColumnValues(IDataView dataView, string column)
    {
        var colum = dataView.GetColumn<System.Single[]>(column);
        print(String.Join("\n", colum.Select(x => String.Join(",", x))));
    }


    // Update is called once per frame
    void Update()
    {
    }
}