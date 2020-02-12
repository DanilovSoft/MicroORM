using DanilovSoft.MicroORM;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Test
{
    

    class Program
    {
        private readonly SqlORM _sql = new SqlORM("Server=10.0.0.101; Port=5432; User Id=hh; Password=doDRC1vJRGybvCW6; Database=hh; " +
            "Pooling=true; MinPoolSize=1; MaxPoolSize=100", Npgsql.NpgsqlFactory.Instance);

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        static void Main(string[] args)
        {
            new Program().Main();
        }

        private void Main()
        {
            var b = JsonConvert.DeserializeObject<TestStruct>("{\"Url\": \"http://test\"}");

            var result = _sql.Sql("SELECT 'http://test.com' AS url, point(@0, @1) as location")
                .Parameters(1, 2)
                .Single<TestStruct>();
        }

        private async void TestAsync()
        {
            SqlORM.CloseConnectionPenaltySec = 5;

            try
            {
                try
                {
                    string[] para = new[] { "1", "2" };

                    _sql.Sql("SELECT @, @")
                        .Parameters(para)
                        .Execute();

                    Console.WriteLine("GO");
                    //await task;

                }
                catch (SqlQueryTimeoutException ex)
                {
                    
                }
                DebugOnly.Break();
            }
            catch (Exception ex)
            {
                DebugOnly.Break();
            }
        }

        public static string GetSqlQuery()
        {
            StringBuilder sb = new StringBuilder("SELECT * FROM (VALUES ");
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
    }

    internal readonly struct TestStruct
    {
        [SqlProperty("url")]
        [SqlConverter(typeof(UriTypeConverter))]
        public readonly Uri Url;
    }

    class UserModel
    {
        [DataMember(Name = "name")]
        public string Name { get; private set; }

        [SqlProperty("age")]
        public int Age { get; private set; }

        [SqlProperty("url")]
        [SqlConverter(typeof(UriTypeConverter))]
        public Uri Url { get; private set; }

        [SqlProperty("location")]
        [SqlConverter(typeof(LocationConverter))]
        public Point Location { get; set; }
    }

    class LocationConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(NpgsqlTypes.NpgsqlPoint);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var point = (NpgsqlTypes.NpgsqlPoint)value;
            return new Point((int)point.X, (int)point.Y);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
