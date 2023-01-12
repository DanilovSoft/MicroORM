using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using DanilovSoft.MicroORM.Helpers;

using static DanilovSoft.MicroORM.Helpers.NullableHelper;

namespace DanilovSoft.MicroORM;

public class SqlQuery : SqlReader
{
    private readonly string _query;
    private protected readonly SqlORM _sqlOrm;
    private Dictionary<string, object?>? _parameters;
    private Dictionary<string, object?> LazyParameters => LazyInitializer.EnsureInitialized(ref _parameters, static () => new());
    private int _anonymParamCount;

    // ctor
    internal SqlQuery(SqlORM sqlOrm, string query) : base(sqlOrm)
    {
        _sqlOrm = sqlOrm;
        _query = query;
    }

    /// <summary>
    /// Запрашивает соединение через фабрику и делает Open.
    /// </summary>
    /// <exception cref="MicroOrmException"/>
    internal virtual DbConnection GetConnection()
    {
        var connection = _sqlOrm.GetConnection();

        if (connection.State == ConnectionState.Open)
        {
            return connection;
        }
        else
        {
            try
            {
                connection.Open();
                return SetNull(ref connection);
            }
            finally
            {
                connection?.Dispose();
            }
        }
    }

    internal virtual ValueTask<DbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = _sqlOrm.GetConnection();

        if (connection.State == ConnectionState.Open)
        {
            return ValueTask.FromResult(connection);
        }
        else
        {
            try
            {
                var task = connection.OpenAsync(cancellationToken);
                if (task.IsCompletedSuccessfully)
                {
                    return ValueTask.FromResult(SetNull(ref connection));
                }

                return Wait(task, SetNull(ref connection));
                static async ValueTask<DbConnection> Wait(Task task, [DisallowNull] DbConnection? connection)
                {
                    try
                    {
                        await task.ConfigureAwait(false);
                        return SetNull(ref connection);
                    }
                    finally
                    {
                        connection?.Dispose();
                    }
                }
            }
            finally
            {
                connection?.Dispose();
            }
        }
    }

    [SuppressMessage("Security", "CA2100:Проверка запросов SQL на уязвимости безопасности", Justification = "Нарушает основную цель микро-орм")]
    protected virtual DbCommand GetCommand()
    {
        var connection = GetConnection();
        var command = connection.CreateCommand();
        AddParameters(command);
        command.CommandText = _query;
        command.CommandTimeout = QueryTimeoutSec;
        return command;
    }

    protected virtual ValueTask<DbCommand> GetCommandAsync(CancellationToken cancellationToken)
    {
        var task = GetOpenConnectionAsync(cancellationToken);

        if (task.IsCompletedSuccessfully)
        {
            var connection = task.Result;
            var command = CreateCommand(connection);
            return ValueTask.FromResult(command);
        }

        return Wait(task);
        async ValueTask<DbCommand> Wait(ValueTask<DbConnection> task)
        {
            var connection = await task.ConfigureAwait(false);
            return CreateCommand(connection);
        }
    }

    [SuppressMessage("Security", "CA2100:Проверка запросов SQL на уязвимости безопасности", Justification = "Нарушает основную цель микро-орм")]
    private DbCommand CreateCommand(DbConnection connection)
    {
        var command = connection.CreateCommand();
        AddParameters(command);
        command.CommandText = _query;

        // The CommandTimeout property will be ignored during asynchronous method calls such as BeginExecuteReader.
        //command.CommandTimeout = base.QueryTimeoutSec;

        return command;
    }

    public SqlQuery Timeout(int timeoutSec)
    {
        QueryTimeoutSec = timeoutSec;
        return this;
    }

    public virtual MultiSqlReader MultiResult()
    {
        var command = GetCommand();
        var sqlReader = new AutoCloseMultiSqlReader(command, _sqlOrm);
        sqlReader.ExecuteReader();
        return sqlReader;
    }

    public ValueTask<MultiSqlReader> MultiResultAsync()
    {
        return MultiResultAsync(CancellationToken.None);
    }

    public virtual async ValueTask<MultiSqlReader> MultiResultAsync(CancellationToken cancellationToken)
    {
        var command = await GetCommandAsync(cancellationToken).ConfigureAwait(false);
        var sqlReader = new AutoCloseMultiSqlReader(command, _sqlOrm);
        await sqlReader.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return sqlReader;
    }

    internal override ICommandReader GetCommandReader()
    {
        var command = GetCommand();
        return new CommandReaderCloseConnection(command);
    }

    [SuppressMessage("Reliability", "CA2000:Ликвидировать объекты перед потерей области", Justification = "Перекладываем ответственность")]
    internal override ValueTask<ICommandReader> GetCommandReaderAsync(CancellationToken cancellationToken)
    {
        var task = GetCommandAsync(cancellationToken);
        if (task.IsCompletedSuccessfully)
        {
            var command = task.Result;
            var comReader = new CommandReaderCloseConnection(command);
            return ValueTask.FromResult<ICommandReader>(comReader);
        }

        return Wait(task);
        static async ValueTask<ICommandReader> Wait(ValueTask<DbCommand> task)
        {
            var command = await task.ConfigureAwait(false);
            return new CommandReaderCloseConnection(command);
        }
    }

    #region Параметры

    private void AddParameters(DbCommand command)
    {
        foreach (var (pName, pValue) in LazyParameters)
        {
            var p = command.CreateParameter();
            p.ParameterName = pName;
            p.Value = pValue ?? DBNull.Value;
            command.Parameters.Add(p);
        }
    }


    /// <exception cref="ArgumentNullException"/>
    public SqlQuery Parameters(params object?[] anonymousParameters)
    {
        if (anonymousParameters != null)
        {
            for (var i = 0; i < anonymousParameters.Length; i++)
            {
                var parameterName = _anonymParamCount.ToString(CultureInfo.InvariantCulture);

                Parameter(parameterName, anonymousParameters[i]);
                _anonymParamCount++;
            }
        }
        else
        {
            ThrowHelper.ThrowArgumentNull(nameof(anonymousParameters));
        }
        return this;
    }

    public SqlQuery Parameter(object? anonymousParameter)
    {
        Parameter(_anonymParamCount.ToString(CultureInfo.InvariantCulture), anonymousParameter);
        _anonymParamCount++;
        return this;
    }

    public SqlQuery Parameter(string name, object? value)
    {
        LazyParameters.Add(name, value);
        return this;
    }

    public SqlQuery Parameters(IEnumerable<KeyValuePair<string, object?>> keyValueParameters)
    {
        // Лучше сделать копию ссылки что-бы в цикле не дёргать ленивку.
        var parameters = LazyParameters;

        if (keyValueParameters != null)
        {
            foreach (var p in keyValueParameters)
            {
                parameters.Add(p.Key, p.Value);
            }
        }
        else
        {
            ThrowHelper.ThrowArgumentNull(nameof(keyValueParameters));
        }
        return this;
    }

    public SqlQuery ParametersFromObject(object values)
    {
        if (values != null)
        {
            var dict = new RouteValueDictionary(values);
            foreach (var keyValue in dict)
            {
                Parameter(keyValue.Key, keyValue.Value);
            }
        }
        else
        {
            ThrowHelper.ThrowArgumentNull(nameof(values));
        }
        return this;
    }
    #endregion
}
