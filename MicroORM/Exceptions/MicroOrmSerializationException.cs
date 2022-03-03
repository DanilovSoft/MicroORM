using System;
using System.Runtime.Serialization;

namespace DanilovSoft.MicroORM;

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

    protected MicroOrmSerializationException(SerializationInfo serializationInfo, StreamingContext streamingContext)
    {

    }
}
