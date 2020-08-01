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

        internal override ValueTask<DbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken)
        {
            DbConnection connection = _transaction.Connection;
            return new ValueTask<DbConnection>(result: connection);
        }

        internal override ICommandReader GetCommandReader()
        {
            DbCommand command = GetCommand();
            var commandReader = new CommandReader(command);
            return commandReader;
        }

        internal async override ValueTask<ICommandReader> GetCommandReaderAsync(CancellationToken cancellationToken)
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

        public override ValueTask<MultiSqlReader> MultiResultAsync(CancellationToken cancellationToken)
        {
            ValueTask<DbCommand> task = GetCommandAsync(cancellationToken);
            if (task.IsCompletedSuccessfully)
            {
                DbCommand command = task.Result;
                return CreateReaderAsync(command, cancellationToken);
            }
            else
            {
                return WaitAsync(task, cancellationToken);
                static async ValueTask<MultiSqlReader> WaitAsync(ValueTask<DbCommand> task, CancellationToken cancellationToken)
                {
                    DbCommand command = await task.ConfigureAwait(false);
                    return await CreateReaderAsync(command, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private static ValueTask<MultiSqlReader> CreateReaderAsync(DbCommand command, CancellationToken cancellationToken)
        {
            var sqlReader = new MultiSqlReader(command);
            MultiSqlReader? toDispose = sqlReader;
            ValueTask task;
            try
            {
                task = sqlReader.ExecuteReaderAsync(cancellationToken);
                toDispose = null;
            }
            finally
            {
                toDispose?.Dispose();
            }
            if (task.IsCompletedSuccessfully)
            {
                return new ValueTask<MultiSqlReader>(result: sqlReader);
            }
            else
            {
                return WaitAsync(task, sqlReader);
                static async ValueTask<MultiSqlReader> WaitAsync(ValueTask task, MultiSqlReader sqlReader)
                {
                    MultiSqlReader? toDispose = sqlReader;
                    try
                    {
                        await task.ConfigureAwait(false);
                        toDispose = null;
                        return sqlReader;
                    }
                    finally
                    {
                        toDispose?.Dispose();
                    }
                }
            }
        }

        protected override DbCommand GetCommand()
        {
            DbCommand command = base.GetCommand();
            command.Transaction = _transaction;
            return command;
        }

        protected override ValueTask<DbCommand> GetCommandAsync(CancellationToken cancellationToken)
        {
            ValueTask<DbCommand> task = base.GetCommandAsync(cancellationToken);
            if (task.IsCompletedSuccessfully)
            {
                DbCommand command = task.Result;
                command.Transaction = _transaction;
                return new ValueTask<DbCommand>(result: command);
            }
            else
            {
                return WaitAsync(task, _transaction);
                static async ValueTask<DbCommand> WaitAsync(ValueTask<DbCommand> task, DbTransaction transaction)
                {
                    DbCommand command = await task.ConfigureAwait(false);
                    command.Transaction = transaction;
                    return command;
                }
            }
        }
    }
}
