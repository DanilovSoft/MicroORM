using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM
{
    public sealed class SqlTransaction : ISqlORM, IDisposable
    {
        private readonly string _connectionString;
        private readonly DbConnection _connection;
        private readonly DbProviderFactory _factory;
        private DbTransaction _transaction;
        private bool _disposed;

        internal SqlTransaction(string connectionString, DbProviderFactory factory)
        {
            _factory = factory;
            _connectionString = connectionString;
            _connection = _factory.CreateConnection();
            _connection.ConnectionString = connectionString;
        }

        public void OpenTransaction()
        {
            _connection.Open();
            _transaction = _connection.BeginTransaction();
        }

        public async Task OpenTransactionAsync(CancellationToken cancellationToken)
        {
            await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            _transaction = _connection.BeginTransaction();
        }

        public Task OpenTransactionAsync()
        {
            return OpenTransactionAsync(CancellationToken.None);
        }

        /// <exception cref="MicroORMException"/>
        public SqlQuery Sql(string query, params object[] parameters)
        {
            if (_transaction == null)
                throw new MicroORMException("Transaction is not open.");

            SqlQuery sql = new SqlQueryTransaction(_transaction, query, _connectionString, _factory);
            sql.Parameters(parameters);
            return sql;
        }

        /// <summary>
        /// Commits the database transaction.
        /// Try/Catch exception handling should always be used when committing or rolling back a SqlTransaction.
        /// </summary>
        public void Commit()
        {
            _transaction.Commit();
        }

        /// <summary>
        /// Rolls back a transaction from a pending state.
        /// Try/Catch exception handling should always be used when committing or rolling back a SqlTransaction.
        /// </summary>
        public void Rollback()
        {
            _transaction.Rollback();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _transaction?.Dispose();
                _connection.Dispose();

                _disposed = true;
            }
        }
    }
}
