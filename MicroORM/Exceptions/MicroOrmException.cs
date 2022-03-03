using System;
using System.Runtime.Serialization;

namespace DanilovSoft.MicroORM;

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

    protected MicroOrmException(SerializationInfo serializationInfo, StreamingContext streamingContext)
    {

    }
}
