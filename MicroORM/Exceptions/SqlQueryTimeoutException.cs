using System;
using System.Runtime.Serialization;

namespace DanilovSoft.MicroORM
{
    [Serializable]
    public class SqlQueryTimeoutException : TimeoutException
    {
        public SqlQueryTimeoutException()
        {

        }

        public SqlQueryTimeoutException(string message) : base(message)
        {

        }

        internal SqlQueryTimeoutException(Exception innerException, int timeoutSec) : this($"Timeout expired after {timeoutSec} seconds", innerException)
        {

        }

        public SqlQueryTimeoutException(string message, Exception innerException) : base(message, innerException)
        {
            
        }

        protected SqlQueryTimeoutException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            
        }
    }
}
