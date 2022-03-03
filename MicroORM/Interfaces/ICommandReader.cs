using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM;

internal interface ICommandReader : IDisposable
{
    DbDataReader GetReader();
    ValueTask<DbDataReader> GetReaderAsync(CancellationToken cancellationToken);
    DbConnection Connection { get; }
    DbCommand Command { get; }
}
