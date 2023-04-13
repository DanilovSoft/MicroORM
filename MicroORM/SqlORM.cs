using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using DanilovSoft.MicroORM.Helpers;

using static DanilovSoft.MicroORM.Helpers.NullableHelper;

namespace DanilovSoft.MicroORM;

public sealed class SqlORM : ISqlORM
{
    /// <remarks>Default value is 30 seconds.</remarks>
    public static int DefaultQueryTimeoutSec { get; set; } = 30;

    /// <summary>
    /// -1 means infinite.
    /// </summary>
    /// <remarks>Default value is 30 seconds.</remarks>
    public static int CloseConnectionPenaltySec { get; set; } = 30;

    private readonly string? _connectionString;
    private readonly DbProviderFactory? _factory;
    private readonly DbConnection? _dbConnection;

    public SqlORM(string connectionString, DbProviderFactory factory, bool usePascalCaseNamingConvention = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        ArgumentNullException.ThrowIfNull(factory);

        _factory = factory;
        _connectionString = connectionString;
        UsePascalCaseNamingConvention = usePascalCaseNamingConvention;
    }

    public SqlORM(DbConnection connection, bool usePascalCaseNamingConvention = false)
    {
        Guard.ThrowIfNull(connection);

        _dbConnection = connection;
        UsePascalCaseNamingConvention = usePascalCaseNamingConvention;
    }

    internal bool UsePascalCaseNamingConvention { get; }

    public SqlQuery Sql(string query, params object?[] parameters)
    {
        Guard.ThrowIfNull(query);
        Guard.ThrowIfNull(parameters);

        var sqlQuery = new SqlQuery(this, query);
        sqlQuery.Parameters(parameters);
        return sqlQuery;
    }

    public SqlQuery SqlInterpolated(FormattableString query, char parameterPrefix = '@')
    {
        Guard.ThrowIfNull(query);

        var argNames = new object[query.ArgumentCount];
        for (var i = 0; i < query.ArgumentCount; i++)
        {
            argNames[i] = FormattableString.Invariant($"{parameterPrefix}{i}");
        }

        var formattedQuery = string.Format(CultureInfo.InvariantCulture, query.Format, argNames);
        var sqlQuery = new SqlQuery(this, formattedQuery);
        sqlQuery.Parameters(query.GetArguments());
        return sqlQuery;
    }

    public MicroOrmTransaction OpenTransaction()
    {
        var t = new MicroOrmTransaction(this);
        try
        {
            t.OpenTransaction();
            return SetNull(ref t);
        }
        finally
        {
            t?.Dispose();
        }
    }

    public ValueTask<MicroOrmTransaction> OpenTransactionAsync() => OpenTransactionAsync(CancellationToken.None);

    public ValueTask<MicroOrmTransaction> OpenTransactionAsync(CancellationToken cancellationToken)
    {
        var transaction = new MicroOrmTransaction(this);
        try
        {
            var task = transaction.OpenTransactionAsync(cancellationToken);

            if (task.IsCompletedSuccessfully)
            {
                task.GetAwaiter().GetResult(); // У Value-тасков нужно обазательно забирать результат.
                return ValueTask.FromResult(SetNull(ref transaction));
            }

            return Wait(task, SetNull(ref transaction));
            static async ValueTask<MicroOrmTransaction> Wait(ValueTask task, [DisallowNull] MicroOrmTransaction? transaction)
            {
                try
                {
                    await task.ConfigureAwait(false);
                    return SetNull(ref transaction);
                }
                finally
                {
                    transaction?.Dispose();
                }
            }
        }
        finally
        {
            transaction?.Dispose();
        }
    }

    public MicroOrmTransaction Transaction() => new(this);

    /// <exception cref="MicroOrmException"/>
    internal DbConnection GetConnection()
    {
        if (_dbConnection is not null)
        {
            return _dbConnection;
        }

        var connection = _factory!.CreateConnection();
        if (connection is null)
        {
            ThrowHelper.ThrowCantGetConnection();
        }

        connection.ConnectionString = _connectionString;
        return connection;
    }
}
