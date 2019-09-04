using System;
using System.Collections.Generic;
using System.Data.Common;
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
        private readonly DbCommand _command;
        private DbDataReader _reader;
        private MultiResultCommandReader _commandReader;

        // ctor.
        internal MultiSqlReader(DbCommand command)
        {
            _command = command;
        }

        internal void ExecuteReader()
        {
            _reader = _command.ExecuteReader();
            _commandReader = new MultiResultCommandReader(_reader, _command);
        }

        internal async Task ExecuteReaderAsync(CancellationToken cancellationToken)
        {
            _reader = await _command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            _commandReader = new MultiResultCommandReader(_reader, _command);
        }

        internal override ICommandReader GetCommandReader()
        {
            return _commandReader;
        }

        internal override Task<ICommandReader> GetCommandReaderAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<ICommandReader>(_commandReader);
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
                _command.Dispose();
            }
        }
    }
}
