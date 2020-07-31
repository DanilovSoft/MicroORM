using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM
{
    internal sealed class MultiResultCommandReader : ICommandReader
    {
        private const string NoNextResultError = "There is no next result set.";
        private readonly DbDataReader _reader;
        private bool _firstTime = true;
        public DbConnection Connection { get; private set; }
        public DbCommand Command { get; private set; }

        internal MultiResultCommandReader(DbDataReader reader, DbCommand command)
        {
            this._reader = reader;
            Command = command;
            Connection = command.Connection;
        }

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
                    throw new MicroORMException(NoNextResultError);
            }
            else
            {
                _firstTime = false;
            }
            return _reader;
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
                        return new ValueTask<DbDataReader>(Task.FromException<DbDataReader>(new MicroORMException(NoNextResultError)));
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
                            throw new MicroORMException(NoNextResultError);
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
