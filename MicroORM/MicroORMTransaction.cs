using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DanilovSoft.MicroORM.Helpers;

namespace DanilovSoft.MicroORM;

public sealed class MicroORMTransaction : ISqlORM, IDisposable
{
    private readonly SqlORM _parent;
    private readonly DbConnection _dbConnection;
    private DbTransaction? _dbTransaction;
    private bool _disposed;

    /// <exception cref="ArgumentNullException"/>
    public MicroORMTransaction(SqlORM parent)
    {
        Guard.ThrowIfNull(parent);

        _parent = parent;
        _dbConnection = parent.GetConnection();
    }

    /// <exception cref="ArgumentNullException"/>
    public MicroORMTransaction(SqlORM parent, DbTransaction dbTransaction)
    {
        Guard.ThrowIfNull(parent);
        Guard.ThrowIfNull(dbTransaction);

        _parent = parent;
        _dbConnection = parent.GetConnection();
        _dbTransaction = dbTransaction;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _dbTransaction?.Dispose();
        _dbConnection.Dispose();
        _dbTransaction = null;
    }

    /// <exception cref="MicroOrmException"/>
    /// <exception cref="ObjectDisposedException"/>
    public DbTransaction GetDbTransaction()
    {
        CheckDisposed();
        CheckTransactionNotNull();

        return _dbTransaction;
    }

    /// <exception cref="ObjectDisposedException"/>
    public void UseTransaction(DbTransaction dbTransaction)
    {
        Guard.ThrowIfNull(dbTransaction);
        CheckDisposed();
        CheckTransactionIsNull();

        _dbTransaction = dbTransaction;
    }

    /// <exception cref="ObjectDisposedException"/>
    public void OpenTransaction()
    {
        CheckDisposed();
        CheckTransactionIsNull();

        if (_dbConnection.State != System.Data.ConnectionState.Open)
        {
            _dbConnection.Open();
        }
        
        _dbTransaction = _dbConnection.BeginTransaction();
    }

    /// <exception cref="ObjectDisposedException"/>
    public ValueTask OpenTransactionAsync() => OpenTransactionAsync(CancellationToken.None);

    /// <exception cref="ObjectDisposedException"/>
    public ValueTask OpenTransactionAsync(CancellationToken cancellationToken)
    {
        CheckDisposed();
        CheckTransactionIsNull();

        if (_dbConnection.State == System.Data.ConnectionState.Open)
        {
            _dbTransaction = _dbConnection.BeginTransaction();
            return default;
        }

        var task = _dbConnection.OpenAsync(cancellationToken);
        if (task.IsCompletedSuccessfully)
        {
            _dbTransaction = _dbConnection.BeginTransaction();
            return default;
        }

        return Wait(task);
        async ValueTask Wait(Task openConnectionTask)
        {
            await openConnectionTask.ConfigureAwait(false);
            _dbTransaction = _dbConnection.BeginTransaction();
        }
    }

    /// <exception cref="MicroOrmException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ObjectDisposedException"/>
    public SqlQuery Sql(string query, params object?[] parameters)
    {
        Guard.ThrowIfNull(query);
        Guard.ThrowIfNull(parameters);
        CheckDisposed();
        CheckTransactionNotNull();

        var sqlQuery = new MicroORMQueryTransaction(_parent, _dbTransaction, query);
        sqlQuery.Parameters(parameters);
        return sqlQuery;
    }

    /// <exception cref="ObjectDisposedException"/>
    public SqlQuery SqlInterpolated(FormattableString query, char parameterPrefix = '@')
    {
        Guard.ThrowIfNull(query);
        CheckDisposed();
        CheckTransactionNotNull();

        var argNames = new object[query.ArgumentCount];
        for (var i = 0; i < query.ArgumentCount; i++)
        {
            argNames[i] = FormattableString.Invariant($"{parameterPrefix}{i}");
        }

        var formattedQuery = string.Format(CultureInfo.InvariantCulture, query.Format, argNames);

        var sqlQuery = new MicroORMQueryTransaction(_parent, _dbTransaction, formattedQuery);
        sqlQuery.Parameters(query.GetArguments());
        return sqlQuery;
    }

    /// <summary>
    /// Commits the database transaction.
    /// Try/Catch exception handling should always be used when committing or rolling back a SqlTransaction.
    /// </summary>
    /// <exception cref="ObjectDisposedException"/>
    public void Commit()
    {
        CheckDisposed();
        CheckTransactionNotNull();

        _dbTransaction.Commit();
    }

    /// <summary>
    /// Rolls back a transaction from a pending state.
    /// Try/Catch exception handling should always be used when committing or rolling back a SqlTransaction.
    /// </summary>
    /// <exception cref="ObjectDisposedException"/>
    public void Rollback()
    {
        CheckDisposed();
        CheckTransactionNotNull();

        _dbTransaction.Rollback();
    }



    /// <exception cref="ObjectDisposedException"/>
    [MemberNotNull(nameof(_dbConnection))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckDisposed()
    {
        if (_dbConnection is not null)
        {
            return;
        }

        ThrowHelper.ThrowObjectDisposed<MicroORMTransaction>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckTransactionIsNull()
    {
        if (_dbTransaction is null)
        {
            return;
        }

        ThrowTransactionAlreadyOpen();
    }

    [MemberNotNull(nameof(_dbTransaction))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckTransactionNotNull()
    {
        if (_dbTransaction is not null)
        {
            return;
        }

        ThrowTransactionNotOpen();
    }

    [DoesNotReturn]
    private static void ThrowTransactionNotOpen()
    {
        throw new MicroOrmException("Transaction is not open");
    }

    [DoesNotReturn]
    private static void ThrowTransactionAlreadyOpen()
    {
        throw new MicroOrmException("Transaction already open");
    }
}
