using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DanilovSoft.MicroORM.Helpers;

namespace DanilovSoft.MicroORM
{
    internal sealed class SqlQueryTransaction : SqlQuery
    {
        private readonly DbTransaction _transaction;

        internal SqlQueryTransaction(SqlORM sqlOrm, DbTransaction transaction, string commandText) 
            : base(sqlOrm, commandText)
        {
            _transaction = transaction;
        }

        internal override DbConnection GetConnection()
        {
            DbConnection? connection = _transaction.Connection;
            Debug.Assert(connection != null);

            return connection;
        }

        internal override ValueTask<DbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken)
        {
            DbConnection? connection = _transaction.Connection;
            Debug.Assert(connection != null);

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
            var sqlReader = new MultiSqlReader(command, _sqlOrm);
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
                return WaitMultiResultAsync(task, cancellationToken);
            }
        }

        private async ValueTask<MultiSqlReader> WaitMultiResultAsync(ValueTask<DbCommand> task, CancellationToken cancellationToken)
        {
            DbCommand command = await task.ConfigureAwait(false);
            return await CreateReaderAsync(command, cancellationToken).ConfigureAwait(false);
        }

        private ValueTask<MultiSqlReader> CreateReaderAsync(DbCommand command, CancellationToken cancellationToken)
        {
            var sqlReader = new MultiSqlReader(command, _sqlOrm);
            
            ValueTask task = sqlReader.ExecuteReaderAsync(cancellationToken);
            
            if (task.IsCompletedSuccessfully)
            {
                return new ValueTask<MultiSqlReader>(result: sqlReader);
            }
            else
            {
                return WaitAsync(task, sqlReader);
                static async ValueTask<MultiSqlReader> WaitAsync(ValueTask task, MultiSqlReader sqlReader)
                {
                    var copy = sqlReader;
                    try
                    {
                        await task.ConfigureAwait(false);
                        return NullableHelper.SetNull(ref copy);
                    }
                    finally
                    {
                        copy?.Dispose();
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
