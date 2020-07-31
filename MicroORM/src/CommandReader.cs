using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM
{
    internal class CommandReader : ICommandReader
    {
        private DbDataReader? _reader;
        public bool SkipNextResult { get; set; }
        public DbConnection Connection { get; private set; }
        public DbCommand Command { get; private set; }
        
        internal CommandReader(DbCommand command)
        {
            Command = command;
            Connection = command.Connection;
        }

        public DbDataReader GetReader()
        {
            if (_reader != null)
                return _reader;

            _reader = Command.ExecuteReader();
            return _reader;
        }

        public ValueTask<DbDataReader> GetReaderAsync(CancellationToken cancellationToken)
        {
            if (_reader != null)
                return new ValueTask<DbDataReader>(result: _reader);
            else
                return InnerGetReaderAsync(cancellationToken);
        }

        private ValueTask<DbDataReader> InnerGetReaderAsync(CancellationToken cancellationToken)
        {
            Task<DbDataReader> task = Command.ExecuteReaderAsync(cancellationToken);
            if (task.IsCompletedSuccessfully())
            {
                _reader = task.Result;
                return new ValueTask<DbDataReader>(result: _reader);
            }
            else
            {
                return WaitAsync(task);
                async ValueTask<DbDataReader> WaitAsync(Task<DbDataReader> task)
                {
                    DbDataReader reader = await task.ConfigureAwait(false);
                    _reader = reader;
                    return reader;
                }
            }
        }

        public virtual void Dispose()
        {
            _reader?.Dispose();
            Command.Dispose();
        }
    }
}
