using DanilovSoft.MicroORM;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
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
        private SqlORM _sql = new SqlORM("Server=10.0.0.101; Port=5432; Database=hh; User Id=hh; Password=doDRC1vJRGybvCW6; Pooling=true; MinPoolSize=1; MaxPoolSize=100; ConnectionIdleLifetime=0", Npgsql.NpgsqlFactory.Instance);

        private CancellationTokenSource _cts = new CancellationTokenSource();

        static void Main(string[] args)
        {
            new Program().Main();
        }

        private void Main()
        {
           
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
}
