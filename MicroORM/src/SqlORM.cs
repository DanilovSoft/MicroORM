using System;
using System.Data.Common;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM
{
    public sealed class SqlORM : ISqlORM
    {
        /// <remarks>Default value is 30 seconds.</remarks>
        public static int DefaultQueryTimeoutSec { get; set; } = 30;
        /// <summary>
        /// -1 means infinite.
        /// </summary>
        /// <remarks>Default value is 30 seconds.</remarks>
        public static int CloseConnectionPenaltySec { get; set; } = 30;
        public string ConnectionString { get; }
        internal readonly DbProviderFactory Factory;
        internal readonly bool UsePascalCaseNamingConvention;

        //public SqlORM(string connectionString, DbConnection connection, bool usePascalCaseNamingConvention = false) 
        //    : this(connectionString, new ConnectionFactoryWrapper(connection), usePascalCaseNamingConvention)
        //{
            
        //}

        // ctor.
        public SqlORM(string connectionString, DbProviderFactory factory, bool usePascalCaseNamingConvention = false)
        {
            if (!string.IsNullOrEmpty(connectionString))
            {
                if (factory != null)
                {
                    Factory = factory;
                    ConnectionString = connectionString;
                    UsePascalCaseNamingConvention = usePascalCaseNamingConvention;
                }
                else
                    throw new ArgumentNullException(nameof(factory));
            }
            else
                throw new ArgumentOutOfRangeException(nameof(connectionString));
        }

        public SqlQuery Sql(string query, params object?[] parameters)
        {
            var sqlQuery = new SqlQuery(this, query);
            sqlQuery.Parameters(parameters);
            return sqlQuery;
        }

        public SqlQuery SqlInterpolated(FormattableString query, char parameterPrefix = '@')
        {
            if (query != null)
            {
                object[] argNames = new object[query.ArgumentCount];
                for (int i = 0; i < query.ArgumentCount; i++)
                {
                    argNames[i] = FormattableString.Invariant($"{parameterPrefix}{i}");
                }

                string formattedQuery = string.Format(CultureInfo.InvariantCulture, query.Format, argNames);

                var sqlQuery = new SqlQuery(this, formattedQuery);
                sqlQuery.Parameters(query.GetArguments());
                return sqlQuery;
            }
            else
                throw new ArgumentNullException(nameof(query));
        }

        public SqlTransaction OpenTransaction()
        {
            var tsql = new SqlTransaction(this);
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
            var tsql = new SqlTransaction(this);
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
            var tsql = new SqlTransaction(this);
            return tsql;
        }
    }
}
