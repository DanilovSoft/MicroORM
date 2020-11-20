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
        private readonly DbConnection _connection;
        private readonly SqlORM _sqlOrm;
        private DbTransaction? _transaction;
        private bool _disposed;

        internal SqlTransaction(SqlORM sqlOrm)
        {
            _sqlOrm = sqlOrm;
            var connection = sqlOrm.Factory.CreateConnection();
            Debug.Assert(connection != null);
            _connection = connection;

            _connection.ConnectionString = sqlOrm.ConnectionString;
        }

        public void OpenTransaction()
        {
            _connection.Open();
            _transaction = _connection.BeginTransaction();
        }

        public ValueTask OpenTransactionAsync(CancellationToken cancellationToken)
        {
            Task task = _connection.OpenAsync(cancellationToken);
            if (task.IsCompletedSuccessfully())
            {
                _transaction = _connection.BeginTransaction();
                return default;
            }
            else
            {
                return WaitAsync(task);
                async ValueTask WaitAsync(Task task)
                {
                    await task.ConfigureAwait(false);
                    _transaction = _connection.BeginTransaction();
                }
            }
        }

        public ValueTask OpenTransactionAsync()
        {
            return OpenTransactionAsync(CancellationToken.None);
        }

        /// <exception cref="MicroOrmException"/>
        /// <exception cref="ArgumentNullException"/>
        public SqlQuery Sql(string query, params object?[] parameters)
        {
            if (_transaction != null)
            {
                SqlQuery sql = new SqlQueryTransaction(_sqlOrm, _transaction, query);
                sql.Parameters(parameters);
                return sql;
            }
            else
                throw new MicroOrmException("Transaction is not open.");
        }

        /// <summary>
        /// Commits the database transaction.
        /// Try/Catch exception handling should always be used when committing or rolling back a SqlTransaction.
        /// </summary>
        public void Commit()
        {
            if (_transaction != null)
            {
                _transaction.Commit();
            }
            else
                throw new MicroOrmException("Transaction is not open");
        }

        /// <summary>
        /// Rolls back a transaction from a pending state.
        /// Try/Catch exception handling should always be used when committing or rolling back a SqlTransaction.
        /// </summary>
        public void Rollback()
        {
            if (_transaction != null)
            {
                _transaction.Rollback();
            }
            else
                throw new MicroOrmException("Transaction is not open");
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
