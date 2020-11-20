﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM
{
    internal sealed class AutoCloseMultiSqlReader : MultiSqlReader
    {
        private readonly DbConnection _con;

        public AutoCloseMultiSqlReader(DbCommand command, SqlORM sqlOrm) : base(command, sqlOrm)
        {
            var con = command.Connection;
            Debug.Assert(_con != null);
            _con = con;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _con.Dispose();
        }
    }
}
