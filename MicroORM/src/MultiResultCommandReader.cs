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

        public async Task<DbDataReader> GetReaderAsync(CancellationToken cancellationToken)
        {
            if (!_firstTime)
            {
                bool hasNextResult = await _reader.NextResultAsync(cancellationToken).ConfigureAwait(false);
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

        public void Dispose()
        {
            
        }
    }
}
