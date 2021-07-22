using System;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace DanilovSoft.MicroORM
{
    /// <summary>
    /// If there is nothing to cancel, nothing happens. 
    /// However, if there is a command in process, and the attempt to cancel fails, no exception is generated.
    /// 
    /// Не спасает от не явного разъединения!
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    internal readonly struct CancelCommandRequest : IDisposable
    {
        private readonly CancellationTokenRegistration _cancellationTokenRegistration;

        public CancelCommandRequest(DbCommand command, CancellationToken cancellationToken)
        {
            _cancellationTokenRegistration = cancellationToken.Register(CloseConnection, state: command, useSynchronizationContext: false);
        }

        private static void CloseConnection(object? state)
        {
            var command = state as DbCommand;
            Debug.Assert(command != null);
            Debug.WriteLine("DbCommand.Cancel()");

            // If there is nothing to cancel, nothing happens. 
            // However, if there is a command in process, and the attempt to cancel fails, no exception is generated.

            // Отправляет серверу SQL запрос на отмену.
            // К примеру после DbCommand.Cancel() PostgreSql присылает ошибку '57014: canceling statement due to user request'
            // Не спасает от не явного разъединения!
            command.Cancel();
        }

        public void Dispose()
        {
            _cancellationTokenRegistration.Dispose();
        }
    }
}
