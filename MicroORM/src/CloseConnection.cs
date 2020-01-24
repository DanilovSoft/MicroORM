using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace DanilovSoft.MicroORM
{
    /// <summary>
    /// Аварийный контроль соединения.
    /// </summary>
    internal sealed class CloseConnection : IDisposable
    {
        private readonly CancellationTokenRegistration _tokenRegistration;
        private readonly int _closeConnectionPenaltySec;
        private readonly Action _socketClosedCallback;
        private readonly DelayedAction _delayedAction;

        public CloseConnection(int closeConnectionPenaltySec, ICommandReader commandReader, CancellationToken cancellationToken, Action socketClosedCallback)
        {
            _closeConnectionPenaltySec = closeConnectionPenaltySec;
            _socketClosedCallback = socketClosedCallback;

            _delayedAction = new DelayedAction(OnDelayedAction, commandReader.Connection);

            // Подписываемся на отмену.
            _tokenRegistration = cancellationToken.Register(OnCancellationToken, useSynchronizationContext: false);
        }

        private void OnCancellationToken()
        {
            Debug.WriteLine($"Закрытие сокета через {_closeConnectionPenaltySec} секунд");

            // Выждать фору.
            _delayedAction.Start(TimeSpan.FromMilliseconds(_closeConnectionPenaltySec * 1000));
        }

        private static void OnDelayedAction(object state)
        {
            var dbСon = (DbConnection)state;
            Debug.WriteLine("DbConnection.Close()");

            try
            {
                //_dbСon.LingerState = new LingerOption(true, 0);

                // An application can call Close more than one time. No exception is generated.
                // блокирует выполнение примерно на 30 секунд пытаясь грациозно закрыть сокет.
                dbСon.Close();
            }
            catch (Exception ex)
            // если закрыть грациозно не получилось то происходит исключение
            {
                Debug.WriteLine($"DbConnection.Close() Exception: {ex.Message}");
            }
        }

        /// <exception cref="OperationCanceledException"/>
        public void Dispose()
        {
            // Отменить таймер.
            bool canceled = _delayedAction.TryCancel(wait: true);

            // Отписаться от токена отмены.
            _tokenRegistration.Dispose();

            if(!canceled)
            // Соединение закрыто таймером.
            {
                // Оповещаем подписчика что сокет был аварийно закрыт.
                _socketClosedCallback();
            }
        }
    }
}
