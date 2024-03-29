﻿using System;
using System.Runtime.Serialization;

namespace DanilovSoft.MicroORM
{
    [Serializable]
    public class ConnectionClosedAbnormallyException : TimeoutException
    {
        public ConnectionClosedAbnormallyException()
        {

        }

        public ConnectionClosedAbnormallyException(string message) : base(message)
        {

        }

        internal ConnectionClosedAbnormallyException(Exception innerException, int commandTimeoutSec, int penaltyTimeoutSec) 
            : this($"Connection was closed abnormally after command timeout of {commandTimeoutSec} seconds with additional penalty time of {penaltyTimeoutSec} seconds", innerException)
        {

        }

        public ConnectionClosedAbnormallyException(string message, Exception innerException) : base(message, innerException)
        {

        }

        protected ConnectionClosedAbnormallyException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            
        }
    }
}
