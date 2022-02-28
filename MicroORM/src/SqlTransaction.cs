﻿using System;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM
{
    public sealed class SqlTransaction : ISqlORM, IDisposable
    {
        private const string NoTransaction = "Transaction is not open";

        private readonly SqlORM _sqlOrm;
        private DbConnection? _connection;
        private DbTransaction? _transaction;

        internal SqlTransaction(SqlORM sqlOrm)
        {
            Debug.Assert(sqlOrm != null);

            _sqlOrm = sqlOrm;
            _connection = sqlOrm.Factory.CreateConnection() ?? throw new MicroOrmException("DbProviderFactory returns null instead of instance of connection");
            _connection.ConnectionString = sqlOrm.ConnectionString;
        }

        /// <exception cref="MicroOrmException"/>
        /// <exception cref="ObjectDisposedException"/>
        public DbTransaction GetDbTransaction()
        {
            CheckDisposed();

            var transaction = _transaction;
            if (transaction != null)
            {
                return transaction;
            }
            else
            {
                return ThrowNotOpen<DbTransaction>();
            }
        }

        /// <exception cref="ObjectDisposedException"/>
        public void OpenTransaction()
        {
            CheckDisposed();

            var connection = _connection;
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }

            _transaction = connection.BeginTransaction();
        }

        /// <exception cref="ObjectDisposedException"/>
        public ValueTask OpenTransactionAsync(CancellationToken cancellationToken)
        {
            CheckDisposed();

            var connection = _connection;
            if (connection.State == System.Data.ConnectionState.Open)
            {
                _transaction = connection.BeginTransaction();
                return default;
            }
            else
            {
                var task = connection.OpenAsync(cancellationToken);

                if (task.IsCompletedSuccessfully)
                {
                    _transaction = connection.BeginTransaction();
                    return default;
                }
                else
                {
                    return WaitAsync(task, connection, this);

                    static async ValueTask WaitAsync(Task task, DbConnection connection, SqlTransaction self)
                    {
                        await task.ConfigureAwait(false);
                        self._transaction = connection.BeginTransaction();
                    }
                }
            }
        }

        /// <exception cref="ObjectDisposedException"/>
        public ValueTask OpenTransactionAsync()
        {
            return OpenTransactionAsync(CancellationToken.None);
        }

        /// <exception cref="MicroOrmException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ObjectDisposedException"/>
        public SqlQuery Sql(string query, params object?[] parameters)
        {
            CheckDisposed();

            if (_transaction != null)
            {
                SqlQuery sql = new SqlQueryTransaction(_sqlOrm, _transaction, query);
                sql.Parameters(parameters);
                return sql;
            }
            else
            {
                return ThrowNotOpen<SqlQuery>();
            }
        }

        /// <exception cref="ObjectDisposedException"/>
        public SqlQuery SqlInterpolated(FormattableString query, char parameterPrefix = '@')
        {
            Debug.Assert(query != null);
            ThrowHelper.AssertNotNull(query, nameof(query));

            CheckDisposed();

            if (_transaction != null)
            {
                var argNames = new object[query.ArgumentCount];
                for (var i = 0; i < query.ArgumentCount; i++)
                {
                    argNames[i] = FormattableString.Invariant($"{parameterPrefix}{i}");
                }

                var formattedQuery = string.Format(CultureInfo.InvariantCulture, query.Format, argNames);

                SqlQuery sql = new SqlQueryTransaction(_sqlOrm, _transaction, formattedQuery);
                sql.Parameters(query.GetArguments());
                return sql;
            }
            else
            {
                return ThrowNotOpen<SqlQuery>();
            }
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
            {
                ThrowNotOpen();
            }
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
            {
                ThrowNotOpen();
            }
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                _transaction?.Dispose();
                _connection.Dispose();
                _transaction = null;
                _connection = null;
            }
        }

        /// <exception cref="ObjectDisposedException"/>
        [MemberNotNull(nameof(_connection))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckDisposed()
        {
            if (_connection != null)
            {
                return;
            }
            ThrowHelper.ThrowObjectDisposed<SqlTransaction>();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowNotOpen()
        {
            throw new MicroOrmException(NoTransaction);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static TReturn ThrowNotOpen<TReturn>()
        {
            throw new MicroOrmException(NoTransaction);
        }
    }
}
