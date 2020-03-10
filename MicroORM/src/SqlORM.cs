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

        public SqlQuery Sql(string query, params object[] parameters)
        {
            var sqlQuery = new SqlQuery(query, ConnectionString, _factory);
            sqlQuery.Parameters(parameters);
            return sqlQuery;
        }

        public SqlTransaction OpenTransaction()
        {
            var tsql = new SqlTransaction(ConnectionString, _factory);
            try
            {
                tsql.OpenTransaction();
                return GlobalVars.SetNull(ref tsql);
            }
            finally
            {
                tsql?.Dispose();
            }
        }

        public Task<SqlTransaction> OpenTransactionAsync()
        {
            return OpenTransactionAsync(CancellationToken.None);
        }

        public async Task<SqlTransaction> OpenTransactionAsync(CancellationToken cancellationToken)
        {
            var tsql = new SqlTransaction(ConnectionString, _factory);
            try
            {
                await tsql.OpenTransactionAsync(cancellationToken).ConfigureAwait(false);
                return GlobalVars.SetNull(ref tsql);
            }
            finally
            {
                tsql?.Dispose();
            }
        }

        public SqlTransaction Transaction()
        {
            var tsql = new SqlTransaction(ConnectionString, _factory);
            return tsql;
        }
    }
}
