﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM
{
    internal interface ICommandReader : IDisposable
    {
        DbDataReader GetReader();
        ValueTask<DbDataReader> GetReaderAsync(CancellationToken cancellationToken);
        DbConnection Connection { get; }
        DbCommand Command { get; }
    }
}
