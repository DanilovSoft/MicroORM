using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM
{
    internal class CommandReader : ICommandReader
    {
        private DbDataReader? _reader;
        private DbCommand? _command;
        private DbConnection? _connection;

        internal CommandReader(DbCommand command)
        {
            Debug.Assert(command.Connection != null, "Команда была создана на основе соединения");

            _command = command;
            _connection = command.Connection;
        }

        /// <exception cref="ObjectDisposedException"/>
        public DbConnection Connection
        { 
            get
            {
                CheckDisposed();
                return _connection;
            }
        }

        /// <exception cref="ObjectDisposedException"/>
        public DbCommand Command
        { 
            get
            {
                CheckDisposed();
                return _command;
            }
        }

        /// <exception cref="ObjectDisposedException"/>
        public DbDataReader GetReader()
        {
            CheckDisposed();

            if (_reader == null)
            {
                _reader = _command.ExecuteReader(CommandBehavior.SequentialAccess);
                return _reader;
            }
            else
                return _reader;
        }

        /// <exception cref="ObjectDisposedException"/>
        public ValueTask<DbDataReader> GetReaderAsync(CancellationToken cancellationToken)
        {
            CheckDisposed();

            if (_reader == null)
            {
                return new ValueTask<DbDataReader>(task: GetReaderCoreAsync(cancellationToken));
            }
            else
            {
                return ValueTask.FromResult(_reader);
            }
        }

        private Task<DbDataReader> GetReaderCoreAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(_command != null);

            // Только Sqlite может завершиться синхронно по этому лучше предпочтём Task чем ValueTask.
            var task = _command.ExecuteReaderAsync(cancellationToken);

            if (task.IsCompletedSuccessfully)
            {
                _reader = task.Result;
                return Task.FromResult(_reader);
            }
            else
            {
                return WaitAsync(task, this);

                static async Task<DbDataReader> WaitAsync(Task<DbDataReader> task, CommandReader commandReader)
                {
                    DbDataReader reader = await task.ConfigureAwait(false);
                    commandReader._reader = reader;
                    return reader;
                }
            }
        }

        public virtual void Dispose()
        {
            _reader?.Dispose();
            _command?.Dispose();

            _command = null;
            _reader = null;
        }

        /// <summary>
        /// Диспозит DbCommand и соединение.
        /// </summary>
        private protected void DisposeConnection()
        {
            _connection?.Dispose();
            _connection = null;
        }

        /// <exception cref="ObjectDisposedException"/>
        [MemberNotNull(nameof(_command))]
        [MemberNotNull(nameof(_connection))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckDisposed()
        {
            if (_command != null)
            {
                Debug.Assert(_connection != null);

                return;
            }
            ThrowHelper.ThrowObjectDisposed(GetType().Name);
        }
    }
}
