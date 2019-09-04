using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM
{
    [Serializable]
    public class MicroORMException : InvalidOperationException
    {
        public MicroORMException()
        {

        }

        public MicroORMException(string message) : base(message)
        {

        }

        public MicroORMException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
