using System;

namespace System.Threading
{
    /// <summary>
    /// Планирует отложенный запуск задачи. Открытые члены класса являются потокобезопасными.
    /// </summary>
    internal sealed class DelayedAction
    {
        private readonly Action<object> _callback;
        private readonly object _token;
        private readonly Timer _timer;
        /// <summary>
        /// Не запланирован: -1;
        /// Запланирован: 0;
        /// Сработал: 1;
        /// Отменен: 2;
        /// </summary>
        private int _state = -1;
        public TimeSpan DueTime { get; }

        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public DelayedAction(Action<object> callback, object token)
        {
            _callback = callback; // ?? throw new ArgumentNullException(nameof(callback));
            _token = token;

            // This is to avoid the potential
            // for a timer to be fired before the returned value is assigned to the variable,
            // potentially causing the callback to reference a bogus value (if passing the timer to the callback). 

            _timer = new Timer(OnTimer);  
        }

        /// <summary>
        /// Возвращает True если действие было успешно запланировано. False если текущий экземпляр уже был отменен.
        /// </summary>
        /// <param name="dueTime"></param>
        /// <returns></returns>
        public bool Start(TimeSpan dueTime)
        {
            // Запланировать таймер если он не был запланирован, не был отменен и не был сработан (не -1, не 1 и не 2).
            if (Interlocked.CompareExchange(ref _state, 0, -1) == -1)
            {
                // Запланировать однократный запуск.
                return _timer.Change(dueTime, Timeout.InfiniteTimeSpan);
            }
            return false;
        }

        /// <summary>
        /// Пытается отменить запланированную задачу.
        /// </summary>
        /// <returns>True если удалось отменить запланированную задачу.</returns>
        /// <param name="wait">True если нужно дожидаться завершения задачи. Одновременно поддерживается только один поток.</param>
        public bool TryCancel(bool wait)
        {
            if (wait)
            {
                return InnerTryCancelWait();
            }
            else
            {
                return InnerTryCancel();
            }
        }

        private bool InnerTryCancel()
        {
            /* Атомарно отменяем таймер */
            int state = InterlockedCancel();

            if (state == 0)
            /* Успели отменить */
            {
                /* Единожды освобождаем ресурс*/
                _timer.Dispose();

                /* Задача отменена */
                return true;
            }
            else
            {
                /* Не успели отменить */
                return false;
            }
        }

        /* Атомарно отменяем таймер */
        private int InterlockedCancel()
        {
            return Interlocked.Exchange(ref _state, 2);
        }

        private bool InnerTryCancelWait()
        {
            // Атомарно отменяем таймер.
            int state = InterlockedCancel();

            if (state == -1 || state == 0)
            // Успели отменить или таймер не был запланирован.
            {
                // Эксклюзивно освобождаем ресурс.
                _timer.Dispose();

                // Задача отменена.
                return true;
            }
            else if (state == 1)
            // Таймер уже сработал.
            {
                // Дожидаемся завершения таймера.
                lock (_timer)
                {
                    return false;
                }
            }
            else
            // Таймер уже был отменен другим потоком.
            {
                return true;
            }
        }

        private void OnTimer(object state)
        {
            // NOTE: state содержит собственный экземпляр таймера.

            lock (_timer)
            {
                if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
                // Таймер успешно сработал.
                {
                    // Единожды освобождаем ресурс.
                    _timer.Dispose();

                    // Выполняем запланированную задачу.
                    _callback(_token);
                }
            }
        }
    }
}
