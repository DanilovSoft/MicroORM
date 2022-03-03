using System;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DanilovSoft.MicroORM.Helpers;

namespace DanilovSoft.MicroORM;

/// <summary>
/// Не закрывает соединение.
/// </summary>
public class MultiSqlReader : SqlReader, IDisposable
{
    private DbCommand? _dbCommand;
    private DbDataReader? _reader;
    private MultiResultCommandReader? _commandReader;

    internal MultiSqlReader(DbCommand command, SqlORM sqlOrm) : base(sqlOrm)
    {
        Debug.Assert(command != null);

        _dbCommand = command;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <exception cref="ObjectDisposedException"/>
    internal void ExecuteReader()
    {
        CheckDisposed();

        _reader = _dbCommand.ExecuteReader();
        _commandReader = new MultiResultCommandReader(_reader, _dbCommand);
    }

    /// <exception cref="ObjectDisposedException"/>
    internal ValueTask ExecuteReaderAsync(CancellationToken cancellationToken)
    {
        CheckDisposed();

        var task = _dbCommand.ExecuteReaderAsync(cancellationToken);

        if (task.IsCompletedSuccessfully)
        {
            var reader = task.Result;
            SetReader(reader);
            return default;
        }
        else
        {
            return WaitAsync(task, this);

            static async ValueTask WaitAsync(Task<DbDataReader> task, MultiSqlReader self)
            {
                var reader = await task.ConfigureAwait(false);
                self.SetReader(reader);
            }
        }
    }

    internal override ICommandReader GetCommandReader()
    {
        Debug.Assert(_commandReader != null, "Должны были сначала создать ридер");

        return _commandReader;
    }

    internal override ValueTask<ICommandReader> GetCommandReaderAsync(CancellationToken cancellationToken)
    {
        Debug.Assert(_commandReader != null, "Должны были сначала создать ридер");

        return ValueTask.FromResult<ICommandReader>(_commandReader);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _reader?.Dispose();
            _dbCommand?.Dispose();
            _commandReader?.Dispose();
        }

        _reader = null;
        _dbCommand = null;
        _commandReader = null;
    }

    private void SetReader(DbDataReader reader)
    {
        CheckDisposed();

        _reader = reader;
        _commandReader = new MultiResultCommandReader(reader, _dbCommand);
    }

    /// <exception cref="ObjectDisposedException"/>
    [MemberNotNull(nameof(_dbCommand))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckDisposed()
    {
        if (_dbCommand != null)
        {
            return;
        }

        ThrowHelper.ThrowObjectDisposed(GetType().Name);
    }
}
