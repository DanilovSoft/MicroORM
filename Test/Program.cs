using ConsoleTest;
using DanilovSoft.MicroORM;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

class Program
{
    public sealed record GalleryRec(string OrigTitle, int Gid, DateTime PostedDate, string Title);

    public const string ConnectionString = "Server=10.0.0.99;Port=5432;User Id=hh;Password=doDRC1vJRGybvCW6;Database=hh;Pooling=true;MinPoolSize=10;MaxPoolSize=16;CommandTimeout=30;Timeout=30";

    static void Main()
    {
        var orm = new SqlORM(ConnectionString, NpgsqlFactory.Instance);

        ReadOnlyMemory<int> arr = new int[] { 1, 2, 3 };

        var l = orm.Sql("SELECT @1")
            .Parameter(arr)
            .ScalarList();
    }

    public sealed class GalleryDb
    {
        public string OrigTitle { get; set; }

        [Column("title")]
        public string Title { get; set; }

        public DateTime PostedDate { get; set; }

        [Column("gid")]
        public int Gid { get; set; }
    }

    public class GalleryStruct
    {
        public readonly int Gid;
        public readonly string? OrigTitle;

        public Uri Address { get; set; }

        public GalleryStruct(
            int gid, 
            string? origTitle, 
            [TypeConverter(typeof(UriTypeConverter)), SqlProperty("uri")] Uri address)
        {
            Gid = gid;
            OrigTitle = origTitle;
            Address = address;
        }
    }

    public const string PgConnectionString = "Server=10.0.0.99;Port=5432;User Id = test; Password=test;Database=test;Pooling=true;" +
        "MinPoolSize=10;MaxPoolSize=16;CommandTimeout=30;Timeout=30";

    //private readonly SqlORM _sqlite = new("Data Source=:memory:;Version=3;New=True;", System.Data.SQLite.SQLiteFactory.Instance);
    private static readonly SqlORM PgOrm = new(PgConnectionString, NpgsqlFactory.Instance, usePascalCaseNamingConvention: true);

    //private readonly CancellationTokenSource _cts = new();

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

    public static string GetSqlQuery()
    {
        var sb = new StringBuilder("SELECT * FROM (VALUES ");
        var n = 1;
        for (var i = 0; i < 1_0; i++)
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
    [TypeConverter(typeof(UriTypeConverter))]
    public Uri Url { get; }

    [SqlProperty("name")]
    private readonly string _name;

    public static int Test { get; set; }

    public object this[int index]
    {
        get => Test;
    }

    public TestStruct(string name, Uri url)
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
    [TypeConverter(typeof(UriTypeConverter))]
    public Uri Url { get; private set; }

    [SqlProperty("location")]
    [TypeConverter(typeof(LocationConverter))]
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

