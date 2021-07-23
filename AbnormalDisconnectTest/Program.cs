using System;
using System.Threading;
using DanilovSoft.MicroORM;

class Program
{
    private static readonly SqlORM _orm = new SqlORM("Server=mt.tiraz.net; Port=5432; User Id=postgres; Password=test; Database=postgres; " +
        "Pooling=true; MinPoolSize=1; MaxPoolSize=100", Npgsql.NpgsqlFactory.Instance);

    static void Main()
    {
        Console.WriteLine("Этот тест сработает только когда БД находится в интернете.");

        // Подготовим одно живое соединение.
        _orm.Sql("SELECT 1").Execute();

        try
        {
            var task = _orm.Sql("SELECT pg_sleep(60)")
                .Timeout(timeoutSec: 20) // таймаут запроса
                .ToAsync()
                .ExecuteAsync();

            Console.WriteLine("Пора выдернуть Ethernet кабель. На это есть 20 секунд");

            task.GetAwaiter().GetResult();
        }
        catch (SqlQueryTimeoutException)
        {
            Console.WriteLine("Успешно сработал таймаут");
        }
        Thread.Sleep(-1);
    }
}
