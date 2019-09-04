using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM
{
    internal sealed class AutoCloseMultiSqlReader : MultiSqlReader
    {
        private readonly DbConnection _con;

        public AutoCloseMultiSqlReader(DbCommand command) : base(command)
        {
            _con = command.Connection;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _con.Dispose();
        }
    }
}
