using DanilovSoft.MicroORM;
using DanilovSoft.MicroORM.ObjectMapping;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
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
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        private readonly SqlORM _sql = new SqlORM("Data Source = db.sqlite", System.Data.SQLite.SQLiteFactory.Instance);

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        static void Main(string[] args)
        {
            new Program().Main();
        }

        [StructLayout(LayoutKind.Auto)]
        internal readonly struct TestStruct
        {
            public string N1 { get; }
            public string N2 { get; }
            //public string N3 { get; }

            public TestStruct(string n1, string n2, string n3)
            {
                N1 = n1;
                N2 = n2;
                //N3 = n3;
            }
        }

        private void Main()
        {
            var row = _sql.Sql("SELECT 'http://test.ru' AS url, 'Grace' AS name")
                .Single(new { url = "" });
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

    [StructLayout(LayoutKind.Auto)]
    internal readonly struct TestStruct
    {
        [SqlProperty("url")]
        [SqlConverter(typeof(UriTypeConverter))]
        public Uri Url { get; }

        [SqlProperty("name")]
        private readonly string _name;

        public static int Test { get; set; }

        public object this[int index]
        {
            get => Test;
        }

        public TestStruct(string name,  Uri url)
        {
            _name = name;
            Url = url;
        }

        public static TestStruct Create(string name, Uri url)
        {
            return new TestStruct(name, url);
        }
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
