namespace DanilovSoft.MicroORM
{
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal sealed class ConnectionFactoryWrapper : DbProviderFactory
    {
        private readonly DbConnection _connection;

        public ConnectionFactoryWrapper(DbConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public override DbCommand? CreateCommand()
        {
            return _connection.CreateCommand();
        }

        public override DbConnection? CreateConnection()
        {
            return _connection;
        }
    }
}
