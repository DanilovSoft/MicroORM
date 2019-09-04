using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Runtime.Serialization;
using System.Text;

namespace DanilovSoft.MicroORM.ObjectMapping
{
    internal sealed class ObjectMapper<T>
    {
        private readonly static Type _thisType = typeof(T);
        private readonly static StreamingContext _defaultStreamingContext = new StreamingContext();
        private readonly ContractActivator _activator;
        private readonly TypeContract _typeContract;

        // ctor.
        public ObjectMapper()
        {
            _activator = DynamicActivator.GetActivator(_thisType);
            _typeContract = _activator.GetTypeContract();
        }

        public object ReadObject(DbDataReader reader)
        {
            object obj = _activator.CreateInstance();

            _activator.OnDeserializingHandle?.Invoke(obj, _defaultStreamingContext);

            if (reader.FieldCount > 0)
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string columnName = reader.GetName(i);
                    Type columnType = reader.GetFieldType(i);
                    object value = reader[i];
                    if (value == DBNull.Value)
                        value = null;

                    if (_typeContract.TryGetOrmProperty(columnName, out OrmProperty ormProperty))
                    {
                        ormProperty.SetValue(obj, value, columnType, columnName);
                    }
                }
            }

            _activator.OnDeserializedHandle?.Invoke(obj, _defaultStreamingContext);

            return obj;
        }
    }
}
