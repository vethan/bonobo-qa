using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;

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

    public GenomeMetricDataView(IEnumerable<GenomeMetric> data, HashSet<string> columnsToIgnore)
    {
        _data = data;

        var builder = new DataViewSchema.Builder();
        var firstData = data.First().metrics;
        foreach (KeyValuePair<string, float> keyValuePair in firstData)
        {
            if (columnsToIgnore.Contains(keyValuePair.Key))
                continue;
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
            => id = new DataViewRowId((ulong) _enumerator.Current.genome.Id, 0);

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