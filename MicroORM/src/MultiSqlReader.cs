using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM
{
    /// <summary>
    /// Не закрывает соединение.
    /// </summary>
    public class MultiSqlReader : SqlReader, IDisposable
    {
        private readonly DbCommand _dbCommand;
        private DbDataReader? _reader;
        private MultiResultCommandReader? _commandReader;

        // ctor.
        internal MultiSqlReader(DbCommand command)
        {
            _dbCommand = command;
        }

        internal void ExecuteReader()
        {
            _reader = _dbCommand.ExecuteReader();
            _commandReader = new MultiResultCommandReader(_reader, _dbCommand);
        }

        internal ValueTask ExecuteReaderAsync(CancellationToken cancellationToken)
        {
            Task<DbDataReader> task = _dbCommand.ExecuteReaderAsync(cancellationToken);
            if (task.IsCompletedSuccessfully())
            {
                var reader = task.Result;
                SetReader(reader);
                return default;
            }
            else
            {
                return WaitAsync(task);
                async ValueTask WaitAsync(Task<DbDataReader> task)
                {
                    var reader = await task.ConfigureAwait(false);
                    SetReader(reader);
                }
            }

            void SetReader(DbDataReader reader)
            {
                _reader = reader;
                _commandReader = new MultiResultCommandReader(reader, _dbCommand);
            }
        }

        internal override ICommandReader GetCommandReader()
        {
            Debug.Assert(_commandReader != null);
            return _commandReader;
        }

        internal override ValueTask<ICommandReader> GetCommandReaderAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(_commandReader != null);
            return new ValueTask<ICommandReader>(result: _commandReader);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _reader?.Dispose();
                _dbCommand.Dispose();
            }
        }
    }
}
