using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using DanilovSoft.MicroORM.Helpers;

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
        Guard.ThrowIfNull(connectionString);
        Guard.ThrowIfEmpty(connectionString);
        Guard.ThrowIfNull(factory);

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
        var sqlQuery = new SqlQuery(this, query);
        sqlQuery.Parameters(parameters);
        return sqlQuery;
    }

    public SqlQuery SqlInterpolated(FormattableString query, char parameterPrefix = '@')
    {
        if (query != null)
        {
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
        else
        {
            throw new ArgumentNullException(nameof(query));
        }
    }

    public SqlTransaction OpenTransaction()
    {
        var t = new SqlTransaction(this);
        try
        {
            t.OpenTransaction();
            return NullableHelper.SetNull(ref t);
        }
        finally
        {
            t?.Dispose();
        }
    }

    public ValueTask<SqlTransaction> OpenTransactionAsync()
    {
        return OpenTransactionAsync(CancellationToken.None);
    }

    public ValueTask<SqlTransaction> OpenTransactionAsync(CancellationToken cancellationToken)
    {
        var t = new SqlTransaction(this);
        try
        {
            var task = t.OpenTransactionAsync(cancellationToken);

            if (task.IsCompletedSuccessfully)
            {
                task.GetAwaiter().GetResult(); // У Value-тасков нужно обазательно забирать результат.
                return ValueTask.FromResult(NullableHelper.SetNull(ref t));
            }
            else
            {
                return WaitAsync(task, NullableHelper.SetNull(ref t));

                static async ValueTask<SqlTransaction> WaitAsync(ValueTask task, [DisallowNull] SqlTransaction? transaction)
                {
                    try
                    {
                        await task.ConfigureAwait(false);
                        return NullableHelper.SetNull(ref transaction);
                    }
                    finally
                    {
                        transaction?.Dispose();
                    }
                }
            }
        }
        finally
        {
            t?.Dispose();
        }
    }

    public SqlTransaction Transaction()
    {
        return new SqlTransaction(this);
    }

    internal DbConnection GetConnection()
    {
        if (_dbConnection is null)
        {
            var connection = _factory!.CreateConnection();
            if (connection is not null)
            {
                connection.ConnectionString = _connectionString;
            }
            else
            {
                ThrowHelper.ThrowCantGetConnection();
            }
            return connection;
        }
        else
        {
            return _dbConnection;
        }
    }
}
