using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM
{
    public sealed class SqlORM : ISqlORM
    {
        public static int DefaultQueryTimeoutSec { get; set; } = 30;
        /// <summary>
        /// -1 means infinite.
        /// </summary>
        public static int CloseConnectionPenaltySec { get; set; } = 30;
        public string ConnectionString { get; }
        private readonly DbProviderFactory _factory;

        // ctor.
        public SqlORM(string connectionString, DbProviderFactory factory)
        {
            if (!string.IsNullOrEmpty(connectionString))
            {
                if (factory != null)
                {
                    _factory = factory;
                    ConnectionString = connectionString;
                }
                else
                    throw new ArgumentNullException(nameof(factory));
            }
            else
                throw new ArgumentOutOfRangeException(nameof(connectionString));
        }

        public SqlQuery Sql(string query, params object?[] parameters)
        {
            var sqlQuery = new SqlQuery(query, ConnectionString, _factory);
            sqlQuery.Parameters(parameters);
            return sqlQuery;
        }

        public SqlTransaction OpenTransaction()
        {
            var tsql = new SqlTransaction(ConnectionString, _factory);
            SqlTransaction? toDispose = tsql;
            try
            {
                tsql.OpenTransaction();
                toDispose = null;
                return tsql;
            }
            finally
            {
                toDispose?.Dispose();
            }
        }

        public ValueTask<SqlTransaction> OpenTransactionAsync()
        {
            return OpenTransactionAsync(CancellationToken.None);
        }

        public ValueTask<SqlTransaction> OpenTransactionAsync(CancellationToken cancellationToken)
        {
            var tsql = new SqlTransaction(ConnectionString, _factory);
            SqlTransaction? toDispose = tsql;

            ValueTask task;
            try
            {
                task = tsql.OpenTransactionAsync(cancellationToken);
                toDispose = null;
            }
            finally
            {
                toDispose?.Dispose();
            }

            if (task.IsCompletedSuccessfully)
            {
                return new ValueTask<SqlTransaction>(result: tsql);
            }
            else
            {
                return WaitAsync(task, tsql);
                static async ValueTask<SqlTransaction> WaitAsync(ValueTask task, SqlTransaction tsql)
                {
                    SqlTransaction? toDispose = tsql;
                    try
                    {
                        await task.ConfigureAwait(false);
                        toDispose = null;
                        return tsql;
                    }
                    finally
                    {
                        toDispose?.Dispose();
                    }
                }
            }
        }

        public SqlTransaction Transaction()
        {
            var tsql = new SqlTransaction(ConnectionString, _factory);
            return tsql;
        }
    }
}
