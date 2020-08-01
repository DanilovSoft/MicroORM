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

        public bool TryGetReader(out DbDataReader? reader)
        {
            if (_reader == null)
            {
                reader = default;
                return false;
            }
            else
            {
                reader = _reader;
                return true;
            }
        }

        public ValueTask<DbDataReader> GetReaderAsync(CancellationToken cancellationToken)
        {
            if (_reader == null)
                return new ValueTask<DbDataReader>(task: InnerGetReaderAsync(cancellationToken));
            else
                return new ValueTask<DbDataReader>(result: _reader);
        }

        private Task<DbDataReader> InnerGetReaderAsync(CancellationToken cancellationToken)
        {
            // Только Sqlite может завершиться синхронно.
            Task<DbDataReader> task = Command.ExecuteReaderAsync(cancellationToken);
            if (task.IsCompletedSuccessfully())
            {
                _reader = task.Result;
                return Task.FromResult(_reader);
            }
            else
            {
                return WaitAsync(task);
                async Task<DbDataReader> WaitAsync(Task<DbDataReader> task)
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
