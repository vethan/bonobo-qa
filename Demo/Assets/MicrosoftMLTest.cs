using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;
using SharpNeat.Core;
using SharpNeat.Genomes.Neat;
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

        IDataView inData = new GenomeMetricDataView(data);

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

    public class GenomeMetric
    {
        public NeatGenome genome;
        public Dictionary<string, float> metrics;
    }

    public sealed class GenomeMetricDataView : IDataView
    {
        private readonly IEnumerable<GenomeMetric> _data;

        public IEnumerable<GenomeMetric> Data
        {
            get { return _data; }
        }

        public long? GetRowCount()
        {
            return _data.Count();
        }

        public DataViewRowCursor GetRowCursor(IEnumerable<DataViewSchema.Column> columnsNeeded,
            System.Random rand = null)
            => new Cursor(this, columnsNeeded.Select(x => x.Name));

        public DataViewRowCursor[] GetRowCursorSet(IEnumerable<DataViewSchema.Column> columnsNeeded, int n,
            System.Random rand = null)
            => new[] {GetRowCursor(columnsNeeded, rand)};

        public DataViewSchema Schema { get; }
        public bool CanShuffle => false;

        public GenomeMetricDataView(IEnumerable<GenomeMetric> data)
        {
            _data = data;

            var builder = new DataViewSchema.Builder();
            var firstData = data.First().metrics;
            foreach (KeyValuePair<string, float> keyValuePair in firstData)
            {
                builder.AddColumn(keyValuePair.Key, NumberDataViewType.Single);
            }

            Schema = builder.ToSchema();
        }


        private sealed class Cursor : DataViewRowCursor
        {
            private bool _disposed;
            private long _position;
            private readonly IEnumerator<GenomeMetric> _enumerator;

            public override long Position => _position;
            public override long Batch => 0;
            public override DataViewSchema Schema { get; }
            private readonly Dictionary<string, Delegate> _getters;

            public Cursor(GenomeMetricDataView parent, IEnumerable<string> wantedColumns)
            {
                Schema = parent.Schema;
                _position = -1;
                _enumerator = parent.Data.GetEnumerator();
                _getters = new Dictionary<string, Delegate>();
                foreach (var column in wantedColumns)
                {
                    _getters[column] =
                        (ValueGetter<float>) ((ref float value) => value = _enumerator.Current.metrics[column]);
                    ;
                }
            }

            private void IdGetterImplementation(ref DataViewRowId id)
                => id = new DataViewRowId((ulong) _position, 0);

            public override ValueGetter<TValue> GetGetter<TValue>(
                DataViewSchema.Column column)
            {
                if (!IsColumnActive(column))
                    throw new ArgumentOutOfRangeException(nameof(column));

                return (ValueGetter<TValue>) _getters[column.Name];
            }

            public override ValueGetter<DataViewRowId> GetIdGetter()
                => IdGetterImplementation;

            protected override void Dispose(bool disposing)
            {
                if (_disposed)
                    return;
                if (disposing)
                {
                    _enumerator.Dispose();
                    _position = -1;
                }

                _disposed = true;
                base.Dispose(disposing);
            }

            public override bool IsColumnActive(DataViewSchema.Column column)
                => _getters.ContainsKey(column.Name);

            public override bool MoveNext()
            {
                if (_disposed)
                    return false;
                if (_enumerator.MoveNext())
                {
                    _position++;
                    return true;
                }

                Dispose();
                return false;
            }
        }
    }
}