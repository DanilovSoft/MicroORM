using System;
using System.Collections.Generic;
using System.Text;

namespace DanilovSoft.MicroORM
{
    [Serializable]
    public class MicroOrmException : Exception
    {
        public MicroOrmException()
        {

        }

        public MicroOrmException(string message) : base(message)
        {

        }

        public MicroOrmException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
