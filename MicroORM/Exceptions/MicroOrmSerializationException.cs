using System;
using System.Collections.Generic;
using System.Text;

namespace DanilovSoft.MicroORM
{
    [Serializable]
    public class MicroOrmSerializationException : MicroOrmException
    {
        public MicroOrmSerializationException()
        {

        }

        public MicroOrmSerializationException(string message) : base(message)
        {

        }

        public MicroOrmSerializationException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
