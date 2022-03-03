using System;

namespace DanilovSoft.MicroORM;

public interface ISqlORM
{
    SqlQuery Sql(string query, params object[] parameters);
    SqlQuery SqlInterpolated(FormattableString query, char parameterPrefix = '@');
}
