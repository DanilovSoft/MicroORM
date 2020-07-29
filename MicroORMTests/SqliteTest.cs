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
    public class SqliteTest
    {
        private static readonly SqlORM _orm = new SqlORM("Data Source=:memory:;Version=3;New=True;", System.Data.SQLite.SQLiteFactory.Instance);

        [TestMethod]
        public void TestScalar()
        {
            string result = _orm.Sql("SELECT @0")
                .Parameter("OK")
                .Scalar<string>();

            Assert.AreEqual("OK", result);
        }

        [TestMethod]
        public void TestNamedParameterAndScalar()
        {
            using (var t = _orm.OpenTransaction())
            {
                byte result = t.Sql("SELECT @count")
                    .Parameter("count", 128)
                    .Scalar<byte>(); // автоматическая конвертация.

                t.Commit();
                Assert.AreEqual(128, result);
            }
        }

        [TestMethod]
        public void TestSelector()
        {
            var result = _orm.Sql("SELECT 1 AS col1")
                .Single(x => new
                {
                    col1 = Convert.ChangeType(x["col1"], typeof(int))
                });

            Assert.AreEqual(1, result.col1);
        }

        [TestMethod]
        public void TestParametersFromObject()
        {
            var result = _orm.Sql("SELECT @name AS name, @count AS count")
                .ParametersFromObject(new { count = 128, name = "Alfred" })
                .SingleOrDefault<UserModel>();

            Assert.AreEqual("Alfred", result.Name);
        }

        [TestMethod]
        public void TestAnonimouseType()
        {
            var result = _orm.Sql("SELECT @name AS name, @age AS age")
                .Parameter("name", "Alfred")
                .Parameter("age", 30)
                .Single(new { name = "", age = 0 });

            Assert.AreEqual("Alfred", result.name);
            Assert.AreEqual(30, result.age);
        }

        [TestMethod]
        public void TestAnonimouseRows()
        {
            var result = _orm.Sql("SELECT @name AS qwer, @age AS a")
                .Parameter("name", "Alfred")
                .Parameter("age", 30)
                .List(new { name = 0, age = "" });

            Assert.AreEqual(1, result.Count);
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
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return true;
        }

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
