using MicroORM;
using MicroORM.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using TestCommon;

namespace TestCurVersion
{
    class Program
    {
        static SqlORM sql = new SqlORM("Server=10.0.0.101; Port=5432; Database=postgres; User Id=mikrobill; Password=mikrobill; Pooling=true; MinPoolSize=1; MaxPoolSize=100; ConnectionIdleLifetime=0", Npgsql.NpgsqlFactory.Instance);

        static void Main(string[] args)
        {
            string query = Class1.GetSqlQuery();

            try
            {
                var sw = Stopwatch.StartNew();

                var result = sql.Sql(query)
                    .List<SqlExtendedData>();

                sw.Stop();

                Console.WriteLine(sw.Elapsed);

                sw.Restart();

                


                Console.ReadKey();
            }
            catch (Exception ex)
            {
                if(Debugger.IsAttached)
                    Debugger.Break();

                throw;
            }
        }
    }

    abstract class BaseClass
    {
        [SqlProperty]
        private int col1;


        protected virtual int col2 { get; set; }


        [OnDeserialized]
        void OnDeserialized(StreamingContext _)
        {

        }
    }

    class SqlData : BaseClass
    {
        [SqlIgnore]
        public int col1 { get; set; }

        //private int col2;
        public static int col3 { get; set; }

        protected override int col2 { get => base.col2; set => base.col2 = value; }


        [OnDeserialized]
        void OnDeserialized(StreamingContext _)
        {

        }
    }


    class SqlExtendedData : SqlData
    {
        [OnDeserialized]
        void OnDeserialized(StreamingContext _)
        {

        }
    }
}
