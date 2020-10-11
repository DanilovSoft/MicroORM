using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM
{
    public class SqlQuery : SqlReader
    {
        private readonly Dictionary<string, object?> _parameters;
        private readonly DbProviderFactory _dbProvider;
        private readonly string _connectionString;
        private readonly string _commandText;
        private int _anonimParamCount;

        internal SqlQuery(string commandText, string connectionString, DbProviderFactory factory)
        {
            _dbProvider = factory;
            _connectionString = connectionString;
            _commandText = commandText;
            _parameters = new Dictionary<string, object?>();
        }

        internal virtual DbConnection GetConnection()
        {
            DbConnection connection = _dbProvider.CreateConnection();
            DbConnection? toDispose = connection;
            connection.ConnectionString = _connectionString;
            try
            {
                connection.Open();
                toDispose = null;
                return connection;
            }
            finally
            {
                toDispose?.Dispose();
            }
        }

        internal virtual ValueTask<DbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken)
        {
            DbConnection connection = _dbProvider.CreateConnection();
            DbConnection? toDispose = connection;
            connection.ConnectionString = _connectionString;
            try
            {
                Task task = connection.OpenAsync(cancellationToken);
                toDispose = null;
                if (task.IsCompletedSuccessfully())
                {
                    return new ValueTask<DbConnection>(result: connection);
                }
                else
                {
                    return WaitAsync(task, connection);
                    static async ValueTask<DbConnection> WaitAsync(Task task, DbConnection connection)
                    {
                        DbConnection? toDispose = connection;
                        try
                        {
                            await task.ConfigureAwait(false);
                            toDispose = null;
                            return connection;
                        }
                        finally
                        {
                            toDispose?.Dispose();
                        }
                    }
                }
            }
            finally
            {
                toDispose?.Dispose();
            }
        }

        [SuppressMessage("Security", "CA2100:Проверка запросов SQL на уязвимости безопасности", Justification = "Нарушает основную цель микро-орм")]
        protected virtual DbCommand GetCommand()
        {
            DbConnection connection = GetConnection();
            DbCommand command = connection.CreateCommand();
            AddParameters(command);
            command.CommandText = _commandText;
            command.CommandTimeout = base.QueryTimeoutSec;
            return command;
        }

        protected virtual ValueTask<DbCommand> GetCommandAsync(CancellationToken cancellationToken)
        {
            ValueTask<DbConnection> task = GetOpenConnectionAsync(cancellationToken);
            if (task.IsCompletedSuccessfully)
            {
                DbConnection connection = task.Result;
                DbCommand command = CreateCommand(connection);
                return new ValueTask<DbCommand>(result: command);
            }
            else
            {
                return WaitAsync(task);
                async ValueTask<DbCommand> WaitAsync(ValueTask<DbConnection> task)
                {
                    DbConnection connection = await task.ConfigureAwait(false);
                    return CreateCommand(connection);
                }
            }
        }

        [SuppressMessage("Security", "CA2100:Проверка запросов SQL на уязвимости безопасности", Justification = "Нарушает основную цель микро-орм")]
        private DbCommand CreateCommand(DbConnection connection)
        {
            DbCommand command = connection.CreateCommand();
            AddParameters(command);
            command.CommandText = _commandText;

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
            DbCommand command = GetCommand();
            var sqlReader = new AutoCloseMultiSqlReader(command);
            sqlReader.ExecuteReader();
            return sqlReader;
        }

        public ValueTask<MultiSqlReader> MultiResultAsync()
        {
            return MultiResultAsync(CancellationToken.None);
        }

        public virtual async ValueTask<MultiSqlReader> MultiResultAsync(CancellationToken cancellationToken)
        {
            DbCommand command = await GetCommandAsync(cancellationToken).ConfigureAwait(false);
            var sqlReader = new AutoCloseMultiSqlReader(command);
            await sqlReader.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            return sqlReader;
        }

        internal override ICommandReader GetCommandReader()
        {
            DbCommand command = GetCommand();
            return new CommandReaderCloseConnection(command);
        }

        internal override ValueTask<ICommandReader> GetCommandReaderAsync(CancellationToken cancellationToken)
        {
            ValueTask<DbCommand> task = GetCommandAsync(cancellationToken);
            if (task.IsCompletedSuccessfully)
            {
                var command = task.Result;
                var comReader = new CommandReaderCloseConnection(command);
                return new ValueTask<ICommandReader>(result: comReader);
            }
            else
            {
                return WaitAsync(task);

                static async ValueTask<ICommandReader> WaitAsync(ValueTask<DbCommand> task)
                {
                    DbCommand command = await task.ConfigureAwait(false);
                    return new CommandReaderCloseConnection(command);
                }
            }
        }

        #region Параметры

        private void AddParameters(DbCommand command)
        {
            foreach (KeyValuePair<string, object?> keyValue in _parameters)
            {
                DbParameter p = command.CreateParameter();
                p.ParameterName = keyValue.Key;
                p.Value = keyValue.Value ?? DBNull.Value;
                command.Parameters.Add(p);
            }
        }

        public SqlQuery Parameters(params object?[] parameters)
        {
            if (parameters != null)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    Parameter(_anonimParamCount.ToString(CultureInfo.InvariantCulture), parameters[i]);
                    _anonimParamCount++;
                }
                return this;
            }
            else
                throw new ArgumentNullException(nameof(parameters));
        }

        public SqlQuery Parameter(object? value)
        {
            Parameter(_anonimParamCount.ToString(CultureInfo.InvariantCulture), value);
            _anonimParamCount++;
            return this;
        }

        public SqlQuery Parameter(string name, object? value)
        {
            _parameters.Add(name, value);
            return this;
        }

        public SqlQuery Parameters(IEnumerable<KeyValuePair<string, object>> keyValueParameters)
        {
            if (keyValueParameters != null)
            {
                foreach (var p in keyValueParameters)
                {
                    _parameters.Add(p.Key, p.Value);
                }
                return this;
            }
            else
                throw new ArgumentNullException(nameof(keyValueParameters));
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
                return this;
            }
            else
                throw new ArgumentNullException(nameof(values));
        }
        #endregion
    }
}
