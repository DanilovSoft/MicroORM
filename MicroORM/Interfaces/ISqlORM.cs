using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM
{
    public interface ISqlORM
    {
        SqlQuery Sql(string commandText, params object[] parameters);
    }
}
