using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace DanilovSoft.MicroORM.ObjectMapping
{
    internal delegate void OnDeserializedDelegate(object obj, StreamingContext streamingContext);
    internal delegate void OnDeserializingDelegate(object obj, StreamingContext streamingContext);
    internal delegate void SetValueDelegate(object obj, object? clrValue);
    internal delegate object CreateInstanceDelegate<T>();
}
