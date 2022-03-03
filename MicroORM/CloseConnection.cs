using System;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;

namespace DanilovSoft.MicroORM;

/// <summary>
/// Аварийный контроль соединения.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal struct CloseConnection : IDisposable
{
    private readonly CancellationTokenRegistration _tokenRegistration;
    private readonly DelayedAction<DbConnection> _delayedAction;

    public CloseConnection(int closeConnectionPenaltySec, DbConnection connection, CancellationToken cancellationToken)
    {
        AbnormallyClosed = false;

        // Подготовим таймер но не запускаем.
        _delayedAction = new DelayedAction<DbConnection>(OnDelayedAction, connection, dueTimeSec: closeConnectionPenaltySec);

        // Подписываемся на отмену. (может сработать сразу поэтому эта операция должна быть в конце).
        _tokenRegistration = cancellationToken.Register(static s => OnCancellationToken(s), state: _delayedAction, useSynchronizationContext: false);
    }

    internal bool AbnormallyClosed { get; private set; }

    private static void OnCancellationToken(object? state)
    {
        var delayedAction = state as DelayedAction<DbConnection>;
        Debug.Assert(delayedAction != null);

        Debug.WriteLine($"Закрытие сокета через {delayedAction.DueTimeSec} секунд");

        // Выждать фору.
        delayedAction.TryStart();
    }

    [SuppressMessage("Design", "CA1031:Не перехватывать исключения общих типов", Justification = "<Ожидание>")]
    private static void OnDelayedAction(DbConnection dbСon)
    {
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
        // Отменить запланированное закрытие соединения.
        var canceled = _delayedAction.TryCancel();

        // Отписаться от токена отмены.
        _tokenRegistration.Dispose();

        if (!canceled)
        // Соединение закрыто таймером или в процессе закрытия.
        {
            // Оповещаем подписчика что сокет был аварийно закрыт.
            AbnormallyClosed = true;
        }
    }
}
