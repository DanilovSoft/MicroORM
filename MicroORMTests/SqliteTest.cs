using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DanilovSoft.MicroORM;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MicroORMTests
{
    [TestClass]
    public class UnitTest1
    {
        private static readonly SqlORM _sql = new SqlORM("Data Source=db.sqlite", System.Data.SQLite.SQLiteFactory.Instance);

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

        [TestMethod]
        public void TestList()
        {
            List<RowModel> list = _sql.Sql(GetSqlQuery())
                .List<RowModel>();
        }

        [TestMethod]
        public void TestScalar()
        {
            string result = _sql.Sql("SELECT @0")
                .Parameter("OK")
                .Scalar<string>();

            Assert.AreEqual("OK", result);
        }

        [TestMethod]
        public void TestNamedParameterAndScalar()
        {
            byte result = _sql.Sql("SELECT @count")
                .Parameter("count", 128)
                .Scalar<byte>(); // автоматическая конвертация

            Assert.AreEqual(128, result);
        }

        [TestMethod]
        public void TestConverter()
        {
            var result = _sql.Sql("SELECT point(@0, @1) AS location")
                .Parameters(1, 2)
                .Single<UserModel>();

            Assert.AreEqual(1, result.Location.X);
            Assert.AreEqual(2, result.Location.Y);
        }

        [TestMethod]
        public void TestTransactionWithMultiResult()
        {
            using (var multiResult = _sql.Sql("SELECT @0 AS row1; SELECT unnest(array['1', '2'])")
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

        [TestMethod]
        public void TestSelector()
        {
            var result = _sql.Sql("SELECT 1 AS col1")
                .Single(x => new
                {
                    col1 = (int)x["col1"]
                });

            Assert.AreEqual(1, result.col1);
        }

        

        [TestMethod]
        public void TestParametersFromObject()
        {
            var result = _sql.Sql("SELECT @name AS name, @count AS count")
                .ParametersFromObject(new { count = 128, name = "Alfred" })
                .SingleOrDefault<UserModel>();

            Assert.AreEqual("Alfred", result.Name);
        }

        [TestMethod]
        public void TestAnonimouseType()
        {
            var result = _sql.Sql("SELECT @name AS name, @age AS age")
                .Parameter("name", "Alfred")
                .Parameter("age", 30)
                .Single(new { name = "", age = 0 });

            Assert.AreEqual("Alfred", result.name);
            Assert.AreEqual(30, result.age);
        }

        [TestMethod]
        public void TestAnonimouseRows()
        {
            var result = _sql.Sql("SELECT @name AS qwer, @age AS a")
                .Parameter("name", "Alfred")
                .Parameter("age", 30)
                .List(new { name = 0, age = "" });

            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public async Task TestTimeout()
        {
            try
            {
                await _sql.Sql("SELECT pg_sleep(10)")
                    .Timeout(5) // таймаут запроса
                    .ToAsync()
                    .Execute();
            }
            catch (SqlQueryTimeoutException ex)
            {
                
            }
        }

        [TestMethod]
        public async Task TestUserCancelled()
        {
            var cts = new CancellationTokenSource();

            try
            {
                var task = _sql.Sql("SELECT pg_sleep(10)")
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
    }

    class UserModel
    {
        [DataMember(Name = "name")]
        public string Name { get; private set; }

        [SqlProperty("age")]
        public int Age { get; private set; }

        [SqlProperty("location")]
        [SqlConverter(typeof(LocationConverter))]
        public Point Location { get; private set; }
    }

    class LocationConverter : TypeConverter
    {
        //public object Convert(object value, Type destinationType)
        //{
        //    var point = (NpgsqlTypes.NpgsqlPoint)value;
        //    return new Point((int)point.X, (int)point.Y);
        //}

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var point = (NpgsqlTypes.NpgsqlPoint)value;
            return new Point((int)point.X, (int)point.Y);
        }
    }

    [DebuggerDisplay("{DebugDisplay,nq}")]
    class RowModel
    {
        private string DebugDisplay => "{" + $"Col1 = {Col1}, col2 = {col2}, Col3 = {Col3}" + "}";

        [DataMember(Name = "col1")]
        public string Col1 { get; private set; }

        [SqlConverter(typeof(IntConverter))]
        public string col2 { get; private set; }

        [SqlProperty("col3")]
        public readonly string Col3 = "";

        [SqlIgnore]
        public readonly int Col4;

        [OnDeserializing]
        private void OnDeserializing(StreamingContext _)
        {
            //Debug.WriteLine("OnDeserializing");
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext _)
        {
            //Debug.WriteLine("OnDeserialized");
        }
    }

    class IntConverter : TypeConverter
    {
        //public object Convert(object value, Type destinationType)
        //{
        //    return value.ToString();
        //}

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return value.ToString();
        }
    }

    public class BestPriceItem
    {
        [SqlProperty("item_id")]
        public int ItemID { get; private set; }

        [SqlProperty("supplier_id")]
        public int SupplierID { get; private set; }

        [SqlProperty("selling_price")]
        public double SellingPrice { get; private set; }

        [SqlProperty("buying_price")]
        public double? BuyingPrice { get; private set; }

        [SqlProperty("stock_level")]
        public string StockLevel { get; private set; }

        [SqlProperty("price_added_date")]
        public DateTime PriceAddedDate { get; private set; }

        [SqlProperty("is_valid_to_times")]
        public DateTime IsValidToTimes { get; private set; }
    }
}
