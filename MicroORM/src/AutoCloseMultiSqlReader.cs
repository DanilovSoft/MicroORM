using System.Data.Common;
using System.Diagnostics;

namespace DanilovSoft.MicroORM
{
    internal sealed class AutoCloseMultiSqlReader : MultiSqlReader
    {
        private readonly DbConnection _con;

        public AutoCloseMultiSqlReader(DbCommand command, SqlORM sqlOrm) : base(command, sqlOrm)
        {
            var con = command.Connection;
            Debug.Assert(con != null);
            _con = con;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _con.Dispose();
        }
    }
}
