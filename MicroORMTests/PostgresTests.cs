using DanilovSoft.MicroORM;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestClass]
    public class PostgresTests
    {
        private static readonly SqlORM _sql = new SqlORM("Server=10.0.0.101; Port=5432; User Id=test; Password=test; Database=hh; " +
            "Pooling=true; MinPoolSize=1; MaxPoolSize=100", System.Data.SQLite.SQLiteFactory.Instance);

        [TestMethod]
        public void PostgresScalarArray()
        {
            var result = _sql.Sql("SELECT unnest(array['1', '2', '3'])")
                .ScalarArray<decimal>(); // + конвертация

            Assert.AreEqual(1, result[0]);
            Assert.AreEqual(2, result[1]);
            Assert.AreEqual(3, result[2]);
        }
    }
}
