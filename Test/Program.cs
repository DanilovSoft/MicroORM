using ConsoleTest;
using DanilovSoft.MicroORM;
using DanilovSoft.MicroORM.ObjectMapping;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Data.SqlClient;
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
    public sealed record GalleryRec(int gid, DateTime posted_date, string title, string token, short pages);

    class Program
    {
        public const string PgConnectionString = "Server=10.0.0.99; Port=5432; User Id=test; Password=test; Database=hh; " +
            "Pooling=true; MinPoolSize=1; MaxPoolSize=10";

        //public const string PgConnectionString = "Server=10.0.0.99;Port=5432;User Id = test; Password=test;Database=test;Pooling=true;" +
        //    "MinPoolSize=10;MaxPoolSize=16;CommandTimeout=30;Timeout=30";

        private readonly SqlORM _sqlite = new SqlORM("Data Source=:memory:;Version=3;New=True;", System.Data.SQLite.SQLiteFactory.Instance);
        private static readonly SqlORM _pgOrm = new SqlORM(PgConnectionString, Npgsql.NpgsqlFactory.Instance, usePascalCaseNamingConvention: false);

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        static void Main()
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

            const string Query = @"SELECT g.gid, g.posted_date, gi.title, gi.token, g.pages, g.rating,
g.category, f.file_name, gt.file_extension, gt.width, gt.height
FROM gallery g
JOIN gallery_info gi ON g.gid = gi.gid
JOIN gallery_thumb gt ON g.gid = gt.gid
JOIN page_file f ON gt.fid = f.fid
WHERE g.visible = 'yes'
ORDER BY g.posted_date DESC
LIMIT 25";

            var list = _pgOrm.Sql(Query).List<GalleryRec>();
            var ef = new EfDbContext();
            ef.Set<GalleryRec>().FromSqlRaw(Query).ToList();
           
            //_pgOrm.Sql(Query).List<TestStruct>();


            //Npgsql.NpgsqlDataReader reader;
            //reader.GetInt32(0);

            for (int i = 0; i < 10; i++)
            {
                var sw = Stopwatch.StartNew();
                var list2 = ef.Set<GalleryRec>().FromSqlRaw(Query).ToList();
                sw.Stop();
                Console.WriteLine($"EF: {sw.ElapsedMilliseconds:0} msec");
            }

            for (int i = 0; i < 10; i++)
            {
                var sw = Stopwatch.StartNew();
                list = _pgOrm.Sql(Query).List<GalleryRec>();
                sw.Stop();

                Console.WriteLine($"MicroORM: {sw.ElapsedMilliseconds:0} msec");
            }

            Console.ReadKey();
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

        private async Task MainAsync()
        {
            try
            {
                await _pgOrm.Sql("SELECT 1").ToAsync().Scalar();
            }
            catch (Exception ex)
            {
                throw;
            }
            

            while (true)
            {
                await _pgOrm.Sql("SELECT 1").ToAsync().Scalar();
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
