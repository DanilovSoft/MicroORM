using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM
{
    internal sealed class CommandReaderCloseConnection : CommandReader
    {
        public CommandReaderCloseConnection(DbCommand command) : base(command)
        {

        }

        public override void Dispose()
        {
            base.Dispose();
            Connection.Dispose();
        }
    }
}
