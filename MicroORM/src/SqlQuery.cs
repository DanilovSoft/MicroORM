using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
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

        internal virtual DbConnection GetConnection()
        {
            DbConnection? connection = _sqlOrm.Factory.CreateConnection();
            Debug.Assert(connection != null);
            connection.ConnectionString = _sqlOrm.ConnectionString;

            if (connection.State == ConnectionState.Open)
            {
                return connection;
            }
            else
            {
                DbConnection? toDispose = connection;
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
        }

        internal virtual ValueTask<DbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken)
        {
            DbConnection? connection = _sqlOrm.Factory.CreateConnection();
            Debug.Assert(connection != null);

            connection.ConnectionString = _sqlOrm.ConnectionString;

            if (connection.State == ConnectionState.Open)
                return new ValueTask<DbConnection>(result: connection);

            DbConnection? toDispose = connection;
            try
            {
                Task task = connection.OpenAsync(cancellationToken);
                toDispose = null;
                if (task.IsCompletedSuccessfully)
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
            command.CommandText = _query;
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
            DbCommand command = GetCommand();
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
            DbCommand command = await GetCommandAsync(cancellationToken).ConfigureAwait(false);
            var sqlReader = new AutoCloseMultiSqlReader(command, _sqlOrm);
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
            foreach ((string pName, object? pValue) in LazyParameters)
            {
                DbParameter p = command.CreateParameter();
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
                for (int i = 0; i < anonymousParameters.Length; i++)
                {
                    string parameterName = _anonymParamCount.ToString(CultureInfo.InvariantCulture);

                    Parameter(parameterName, anonymousParameters[i]);
                    _anonymParamCount++;
                }
                return this;
            }
            else
                throw new ArgumentNullException(nameof(anonymousParameters));
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
