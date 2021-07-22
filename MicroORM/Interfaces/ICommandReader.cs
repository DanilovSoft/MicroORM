using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM
{
    internal interface ICommandReader : IDisposable
    {
        DbDataReader GetReader();
        bool TryGetReader([MaybeNullWhen(false)] out DbDataReader? reader);
        ValueTask<DbDataReader> GetReaderAsync(CancellationToken cancellationToken);
        DbConnection Connection { get; }
        DbCommand Command { get; }
    }
}
