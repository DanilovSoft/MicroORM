using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Runtime.Serialization;
using DanilovSoft.MicroORM;
using NUnit.Framework;

namespace MicroORMTests
{
    //class TestClass
    //{
    //    public string Name { get; set; }
    //}

    public class SqliteTest
    {
        private static readonly SqlORM Orm = new("Data Source=:memory:;Version=3;New=True;", System.Data.SQLite.SQLiteFactory.Instance);

        [Test]
        public void Interpolated()
        {
            var result = Orm.SqlInterpolated($"SELECT {123}")
                .Scalar<int>();

            Assert.AreEqual(123, result);
        }

        [Test]
        public void ScalarNotNull()
        {
            string result = Orm.Sql("SELECT @0")
                .Parameter("OK")
                .Scalar<string>();

            Assert.AreEqual("OK", result);
        }

        [Test]
        public void ScalarNull()
        {
            string result = Orm.Sql("SELECT @0")
                .Parameter(null)
                .Scalar<string>();

            Assert.AreEqual(null, result);
        }

        [Test]
        public void TestNamedParameterAndScalar()
        {
            using (var t = Orm.OpenTransaction())
            {
                byte result = t.Sql("SELECT @count")
                    .Parameter("count", 128)
                    .Scalar<byte>(); // автоматическая конвертация.

                t.Commit();
                Assert.AreEqual(128, result);
            }
        }

        [Test]
        public void NonNullableProperty()
        {
            try
            {
                UserDbo? result = Orm.Sql("SELECT @name AS name, @count AS count")
                    .ParametersFromObject(new { count = 128, name = default(string) })
                    .SingleOrDefault<UserDbo>();
            }
            catch (MicroOrmException)
            {
                Assert.Pass();
            }
            Assert.Fail();
        }

        [Test]
        public void ParametersFromObject()
        {
            UserDbo result = Orm.Sql("SELECT @name AS name, @count AS count, @age AS age")
                .ParametersFromObject(new { count = 128, name = "Alfred", age = 25 })
                .Single<UserDbo>();

            Assert.AreEqual("Alfred", result.Name);
            Assert.AreEqual(25, result.Age);
        }

        [Test]
        public void TestAnonimouseType()
        {
            var result = Orm.Sql("SELECT @name AS name, @age AS age")
                .Parameter("name", "Alfred")
                .Parameter("age", 30)
                .Single(new { name = "", age = 0 });

            Assert.AreEqual("Alfred", result.name);
            Assert.AreEqual(30, result.age);
        }

        [Test]
        public void AnonimouseRowsCount()
        {
            var result = Orm.SqlInterpolated($"SELECT {"Alfred"} AS name, {30} AS age")
                .List(new { name = "", age = 0 });

            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void NotMappedAnonimouseProperty()
        {
            try
            {
                Orm.SqlInterpolated($"SELECT {"Alfred"} AS name, {30} AS aaaaaa")
                    .Single(new { name = "", age = 0 });
            }
            catch (MicroOrmException)
            {
                Assert.Pass();
            }
            Assert.Fail();
        }

        [Test]
        public void TestNullParametersArray()
        {
            var query = Orm.Sql("");

            try
            {
                query.Parameters(anonymousParameters: null!);
            }
            catch (ArgumentNullException)
            {
                Assert.Pass();
            }
        }
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

        [TypeConverter(typeof(IntConverter))]
        public string col2 { get; private set; }

        [SqlProperty("col3")]
        public readonly string Col3 = "";

        [SqlIgnore]
        public readonly int Col4 = 0;

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

        public override object? ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return value.ToString();
        }
    }

    //public class BestPriceItem
    //{
    //    [SqlProperty("item_id")]
    //    public int ItemID { get; private set; }

    //    [SqlProperty("supplier_id")]
    //    public int SupplierID { get; private set; }

    //    [SqlProperty("selling_price")]
    //    public double SellingPrice { get; private set; }

    //    [SqlProperty("buying_price")]
    //    public double? BuyingPrice { get; private set; }

    //    [SqlProperty("stock_level")]
    //    public string StockLevel { get; private set; }

    //    [SqlProperty("price_added_date")]
    //    public DateTime PriceAddedDate { get; private set; }

    //    [SqlProperty("is_valid_to_times")]
    //    public DateTime IsValidToTimes { get; private set; }
    //}
}
