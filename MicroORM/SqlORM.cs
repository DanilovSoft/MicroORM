using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
                _factory = factory;
                ConnectionString = connectionString;
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

        public SqlTransaction Transaction()
        {
            var tsql = new SqlTransaction(ConnectionString, _factory);
            return tsql;
        }
    }
}
