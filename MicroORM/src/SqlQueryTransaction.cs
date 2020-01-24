using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM
{
    internal sealed class SqlQueryTransaction : SqlQuery
    {
        private readonly DbTransaction _transaction;

        internal SqlQueryTransaction(DbTransaction transaction, string commandText, string connectionString, DbProviderFactory factory) : base(commandText, connectionString, factory)
        {
            _transaction = transaction;
        }

        internal override DbConnection GetConnection()
        {
            DbConnection connection = _transaction.Connection;
            return connection;
        }

        internal override Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken)
        {
            DbConnection connection = _transaction.Connection;
            return Task.FromResult(connection);
        }

        internal override ICommandReader GetCommandReader()
        {
            DbCommand command = GetCommand();
            var commandReader = new CommandReader(command);
            return commandReader;
        }

        internal async override Task<ICommandReader> GetCommandReaderAsync(CancellationToken cancellationToken)
        {
            DbCommand command = await GetCommandAsync(cancellationToken).ConfigureAwait(false);
            var commandReader = new CommandReader(command);
            return commandReader;
        }

        public override MultiSqlReader MultiResult()
        {
            DbCommand command = GetCommand();
            MultiSqlReader sqlReader = new MultiSqlReader(command);
            sqlReader.ExecuteReader();
            return sqlReader;
        }

        public override async Task<MultiSqlReader> MultiResultAsync(CancellationToken cancellationToken)
        {
            DbCommand command = await GetCommandAsync(cancellationToken).ConfigureAwait(false);
            MultiSqlReader sqlReader = new MultiSqlReader(command);
            await sqlReader.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            return sqlReader;
        }

        protected override DbCommand GetCommand()
        {
            DbCommand command = base.GetCommand();
            command.Transaction = _transaction;
            return command;
        }

        protected override async Task<DbCommand> GetCommandAsync(CancellationToken cancellationToken)
        {
            DbCommand command = await base.GetCommandAsync(cancellationToken).ConfigureAwait(false);
            command.Transaction = _transaction;
            return command;
        }
    }
}
