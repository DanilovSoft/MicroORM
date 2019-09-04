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
    /// <summary>
    /// If there is nothing to cancel, nothing happens. 
    /// However, if there is a command in process, and the attempt to cancel fails, no exception is generated.
    /// 
    /// Не спасает от не явного разъединения!
    /// </summary>
    internal sealed class CancelCommandRequest : IDisposable
    {
        private readonly DbCommand _command;
        private readonly CancellationTokenRegistration _cancellationTokenRegistration;

        public CancelCommandRequest(DbCommand command, CancellationToken cancellationToken)
        {
            _command = command;
            _cancellationTokenRegistration = cancellationToken.Register(CloseConnection, useSynchronizationContext: false);
        }

        private void CloseConnection()
        {
            Debug.WriteLine("DbCommand.Cancel()");
            
            // If there is nothing to cancel, nothing happens. 
            // However, if there is a command in process, and the attempt to cancel fails, no exception is generated.
            
            // Отправляет серверу SQL запрос на отмену.
            // К примеру после DbCommand.Cancel() PostgreSql присылает ошибку '57014: canceling statement due to user request'
            // Не спасает от не явного разъединения!
            _command.Cancel();
        }

        public void Dispose()
        {
            _cancellationTokenRegistration.Dispose();
        }
    }
}
