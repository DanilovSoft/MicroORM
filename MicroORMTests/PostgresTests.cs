using DanilovSoft.MicroORM;
using MicroORMTests;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UnitTests
{
    public class PostgresTests
    {
        private static readonly SqlORM _orm = new SqlORM("Server=10.0.0.99; Port=5432; User Id=test; Password=test; Database=hh; " +
            "Pooling=true; MinPoolSize=1; MaxPoolSize=100", Npgsql.NpgsqlFactory.Instance);

        [Test]
        public void ScalarArray()
        {
            decimal[] result = _orm.Sql("SELECT unnest(array['1', '2', '3'])")
                .ScalarArray<decimal>(); // + конвертация

            Assert.AreEqual(1, result[0]);
            Assert.AreEqual(2, result[1]);
            Assert.AreEqual(3, result[2]);
        }

        [Test]
        public void TestConverter()
        {
            UserDbo result = _orm.Sql("SELECT point(@0, @1) AS location")
                .Parameters(1, 2)
                .Single<UserDbo>();

            Assert.AreEqual(1, result.Location.X);
            Assert.AreEqual(2, result.Location.Y);
        }

        [Test]
        public async Task TestTimeout5Sec()
        {
            try
            {
                await _orm.Sql("SELECT pg_sleep(10)")
                    .Timeout(timeoutSec: 5) // таймаут запроса
                    .ToAsync()
                    .Execute();
            }
            catch (SqlQueryTimeoutException)
            {

            }
        }

        [Test]
        public void TestTransactionWithMultiResult()
        {
            using (var multiResult = _orm.Sql("SELECT @0 AS row1; SELECT unnest(array['1', '2'])")
                .Parameters(1)
                .MultiResult())
            {

                int row1 = multiResult.Scalar<int>();
                string[] row2 = multiResult.ScalarArray<string>();

                Assert.AreEqual(1, row1);
                Assert.AreEqual("1", row2[0]);
                Assert.AreEqual("2", row2[1]);
            }
        }

        [Test]
        public async Task TestUserCancelled1Sec()
        {
            var cts = new CancellationTokenSource();

            try
            {
                var task = _orm.Sql("SELECT pg_sleep(10)")
                    .Timeout(5) // таймаут запроса
                    .ToAsync()
                    .Execute(cts.Token);

                await Task.Delay(1000);

                cts.Cancel();
                await task;
            }
            catch (OperationCanceledException)
            {

            }
        }

        private string GetSqlQuery()
        {
            var sb = new StringBuilder("SELECT * FROM (VALUES ");
            int n = 1;
            for (int i = 0; i < 1_0; i++)
            {
                sb.Append($"({n++},{n++},{n++}),");
            }
            sb.Remove(sb.Length - 1, 1);
            sb.Append(") AS q (col1, col2, col3)");

            //  SELECT * FROM (VALUES (1,2,3), (4,5,6), (7,8,9)) AS q (col1, col2, col3);
            return sb.ToString();
        }

        [Test]
        public void TestList()
        {
            string query = GetSqlQuery();
            List<RowModel> list = _orm.Sql(query)
                .List<RowModel>();
        }
    }
}
