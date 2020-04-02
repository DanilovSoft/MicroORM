using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM
{
    public class SqlQuery : SqlReader
    {
        private readonly Dictionary<string, object> _parameters;
        private readonly DbProviderFactory _factory;
        private readonly string _connectionString;
        private readonly string _commandText;
        private int _anonimParamCount = 0;

        internal SqlQuery(string commandText, string connectionString, DbProviderFactory factory)
        {
            _factory = factory;
            _connectionString = connectionString;
            _commandText = commandText;
            _parameters = new Dictionary<string, object>();
        }

        internal virtual DbConnection GetConnection()
        {
            DbConnection connection = _factory.CreateConnection();
            connection.ConnectionString = _connectionString;
            connection.Open();
            return connection;
        }

        internal virtual async Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken)
        {
            DbConnection connection = _factory.CreateConnection();
            connection.ConnectionString = _connectionString;
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }

        protected virtual DbCommand GetCommand()
        {
            DbConnection connection = GetConnection();
            DbCommand command = connection.CreateCommand();
            AddParameters(command);
            command.CommandText = _commandText;
            command.CommandTimeout = base.QueryTimeoutSec;
            return command;
        }

        protected virtual async Task<DbCommand> GetCommandAsync(CancellationToken cancellationToken)
        {
            DbConnection connection = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
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

        public Task<MultiSqlReader> MultiResultAsync()
        {
            return MultiResultAsync(CancellationToken.None);
        }

        public virtual async Task<MultiSqlReader> MultiResultAsync(CancellationToken cancellationToken)
        {
            DbCommand command = await GetCommandAsync(cancellationToken).ConfigureAwait(false);
            var sqlReader = new AutoCloseMultiSqlReader(command);
            await sqlReader.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            return sqlReader;
        }

        internal override ICommandReader GetCommandReader()
        {
            DbCommand command = GetCommand();
            var comReader = new CommandReaderCloseConnection(command);
            return comReader;
        }

        internal override async Task<ICommandReader> GetCommandReaderAsync(CancellationToken cancellationToken)
        {
            DbCommand command = await GetCommandAsync(cancellationToken).ConfigureAwait(false);
            var comReader = new CommandReaderCloseConnection(command);
            return comReader;
        }

        #region Параметры

        private void AddParameters(DbCommand command)
        {
            foreach (var keyValue in _parameters)
            {
                DbParameter p = command.CreateParameter();
                p.ParameterName = keyValue.Key;
                p.Value = keyValue.Value ?? DBNull.Value;
                command.Parameters.Add(p);
            }
        }

        public SqlQuery Parameters(params object[] parameters)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                Parameter(_anonimParamCount.ToString(), parameters[i]);
                _anonimParamCount++;
            }

            return this;
        }

        public SqlQuery Parameter(object value)
        {
            Parameter(_anonimParamCount.ToString(), value);
            _anonimParamCount++;
            return this;
        }

        public SqlQuery Parameter(string name, object value)
        {
            _parameters.Add(name, value);
            return this;
        }

        public SqlQuery Parameters(IEnumerable<KeyValuePair<string, object>> parameters)
        {
            foreach (var p in parameters)
            {
                _parameters.Add(p.Key, p.Value);
            }

            return this;
        }

        public SqlQuery ParametersFromObject(object values)
        {
            RouteValueDictionary dict = new RouteValueDictionary(values);
            foreach (var keyValue in dict)
            {
                Parameter(keyValue.Key, keyValue.Value);
            }

            return this;
        }
        #endregion
    }
}
