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
        private DbDataReader _reader;
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

        public Task<DbDataReader> GetReaderAsync(CancellationToken cancellationToken)
        {
            if (_reader != null)
                return Task.FromResult(_reader);

            return InnerGetReaderAsync(cancellationToken);
        }

        private async Task<DbDataReader> InnerGetReaderAsync(CancellationToken cancellationToken)
        {
            _reader = await Command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            return _reader;
        }

        public virtual void Dispose()
        {
            _reader?.Dispose();
            Command.Dispose();
        }
    }
}
