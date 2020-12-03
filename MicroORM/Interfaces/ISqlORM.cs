using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM
{
    public interface ISqlORM
    {
        SqlQuery Sql(string query, params object[] parameters);
        SqlQuery SqlInterpolated(FormattableString query, char parameterPrefix = '@');
    }
}
