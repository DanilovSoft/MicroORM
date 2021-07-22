using System.Data.Common;

namespace DanilovSoft.MicroORM
{
    /// <summary>
    /// Закрывает соединение при Dispose.
    /// </summary>
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
