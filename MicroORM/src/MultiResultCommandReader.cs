using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM
{
    internal sealed class MultiResultCommandReader : ICommandReader
    {
        private const string NoNextResultError = "There is no next result set.";
        private readonly DbDataReader _reader;
        private bool _firstTime = true;

        internal MultiResultCommandReader(DbDataReader reader, DbCommand command)
        {
            Debug.Assert(reader != null);
            Debug.Assert(command != null);
            Debug.Assert(command.Connection != null);

            _reader = reader;
            Command = command;
            Connection = command.Connection;
        }

        public DbConnection Connection { get; private set; }
        public DbCommand Command { get; private set; }

        public DbDataReader GetReader()
        {
            if (!_firstTime)
            {
                bool hasNextResult = _reader.NextResult();
                if (hasNextResult)
                {
                    return _reader;
                }
                else
                {
                    throw new MicroOrmException(NoNextResultError);
                }
            }
            else
            {
                _firstTime = false;
            }
            return _reader;
        }

        public bool TryGetReader(out DbDataReader? reader)
        {
            if (!_firstTime)
            {
                reader = default;
                return false;
            }
            else
            {
                _firstTime = false;
                reader = _reader;
                return true;
            }
        }

        public ValueTask<DbDataReader> GetReaderAsync(CancellationToken cancellationToken)
        {
            if (!_firstTime)
            {
                Task<bool> task = _reader.NextResultAsync(cancellationToken);
                if (task.IsCompletedSuccessfully())
                {
                    bool hasNextResult = task.Result;
                    if (hasNextResult)
                    {
                        return new ValueTask<DbDataReader>(result: _reader);
                    }
                    else
                        return new ValueTask<DbDataReader>(Task.FromException<DbDataReader>(new MicroOrmException(NoNextResultError)));
                }
                else
                {
                    return WaitAsync(task, _reader);
                    static async ValueTask<DbDataReader> WaitAsync(Task<bool> task, DbDataReader reader)
                    {
                        bool hasNextResult = await task.ConfigureAwait(false);
                        if (hasNextResult)
                        {
                            return reader;
                        }
                        else
                            throw new MicroOrmException(NoNextResultError);
                    }
                }
            }
            else
            {
                _firstTime = false;
                return new ValueTask<DbDataReader>(result: _reader);
            }
        }

        public void Dispose()
        {

        }
    }
}
