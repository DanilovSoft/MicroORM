using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Threading
{
    /// <summary>
    /// Планирует отложенный запуск задачи. Открытые члены класса являются потокобезопасными.
    /// </summary>
    [SuppressMessage("Design", "CA1001:Типы, владеющие высвобождаемыми полями, должны быть высвобождаемыми", Justification = "Таймер гарантированно освобождается")]
    internal sealed class DelayedAction<TState>
    {
        private readonly Timer _timer;
        private readonly TState _token;
        private readonly Action<TState> _callback;
        /// <summary>
        /// -1 — Не запланирован.
        /// 0 — Запланирован.
        /// 1 — Сработал.
        /// 2 — Отменен.
        /// </summary>
        private int _state = -1;

        public DelayedAction(Action<TState> callback, TState token, int dueTimeSec)
        {
            Debug.Assert(callback != null);

            _callback = callback;
            _token = token;
            DueTimeSec = dueTimeSec;

            // Создать таймер в выключенном состоянии что-бы предотвратить его срабатывание до установки значения поля _timer.
            _timer = new Timer(static s => ((DelayedAction<TState>)s!).OnTimer(), state: this, Timeout.Infinite, Timeout.Infinite);
        }

        internal int DueTimeSec { get; }


        /// <remarks>Может сработать одновременно с TryCancel.</remarks>
        [SuppressMessage("Reliability", "CA2002:Не блокируйте слабо идентифицируемые объекты", Justification = "Никто не блокирует этот объект")]
        private void OnTimer()
        {
            lock (this)
            {
                if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
                // Таймер успешно сработал.
                {
                    // Можем эксклюзивно освободить ресурс.
                    _timer.Dispose();

                    // Выполняем запланированную задачу.
                    _callback(_token);
                }
            }
        }

        /// <remarks>Может сработать одновременно с TryCancel.</remarks>
        [SuppressMessage("Reliability", "CA2002:Не блокируйте слабо идентифицируемые объекты", Justification = "Никто не блокирует этот объект")]
        public void TryStart()
        {
            // Запланировать таймер если он не был запланирован и не был отменен.
            if (Interlocked.CompareExchange(ref _state, 0, -1) == -1)
            {
                // Нас может обогнать поток TryCancel и выполнить Dispose.
                lock (this)
                {
                    // Проверяем что мы обогнали TryCancel.
                    if (Volatile.Read(ref _state) == 0)
                    {
                        // Запланировать однократный запуск.
                        _timer.Change(DueTimeSec * 1000, -1);
                    }
                }
            }
        }

        /// <summary>
        /// Пытается отменить запланированную задачу.
        /// </summary>
        /// <remarks>Может сработать одновременно с TryStart.</remarks>
        /// <returns>True если удалось отменить запланированную задачу.</returns>
        [SuppressMessage("Reliability", "CA2002:Не блокируйте слабо идентифицируемые объекты", Justification = "Никто не блокирует этот объект")]
        public bool TryCancel()
        {
            // Атомарно отменяем таймер.
            var state = Interlocked.Exchange(ref _state, 2);

            if (state == -1 || state == 0)
            // Таймер не был запланирован или не успел сработать.
            {
                // Нас может обогнать поток TryStart и выполнить Change.
                lock (this)
                {
                    // Можем эксклюзивно освободить ресурс.
                    _timer.Dispose();
                }

                // Задача отменена.
                return true;
            }
            else if (state == 1)
            // Таймер уже сработал.
            {
                // Дожидаемся завершения делегата таймера.
                lock (this)
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
    }
}
