using System.Data.Common;
using System.Diagnostics;

namespace DanilovSoft.MicroORM;

internal sealed class AutoCloseMultiSqlReader : MultiSqlReader
{
    private readonly DbConnection _dbConnection;

    public AutoCloseMultiSqlReader(DbCommand command, SqlORM parent) : base(command, parent)
    {
        var dbConnection = command.Connection;
        Debug.Assert(dbConnection != null);
        _dbConnection = dbConnection;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _dbConnection.Dispose();
    }
}
