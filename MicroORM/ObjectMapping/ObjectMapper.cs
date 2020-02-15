using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

namespace DanilovSoft.MicroORM.ObjectMapping
{
    [StructLayout(LayoutKind.Auto)]
    internal readonly struct ObjectMapper<T>
    {
        private static readonly Type ThisType = typeof(T);
        private static readonly StreamingContext DefaultStreamingContext = new StreamingContext();
        private readonly ContractActivator _activator;
        private readonly DbDataReader _reader;

        // ctor.
        public ObjectMapper(DbDataReader reader)
        {
            _reader = reader;

            // Инициализирует из ленивого хранилища.
            _activator = StaticCache.FromLazyActivator(ThisType);
        }

        public object ReadObject()
        {
            if (!_activator.IsReadonlyStruct)
            {
                return InnerReadObject(_reader);
            }
            else
            {
                return InnerReadReadonlyObject(_reader);
            }
        }

        private object InnerReadObject(DbDataReader reader)
        {
            object obj = _activator.CreateInstance();

            _activator.OnDeserializingHandle?.Invoke(obj, DefaultStreamingContext);

            if (reader.FieldCount > 0)
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string columnName = reader.GetName(i);
                    Type columnSourceType = reader.GetFieldType(i);
                    object value = reader[i];
                    if (value == DBNull.Value)
                        value = null;

                    if (_activator.Contract.TryGetOrmPropertyFromLazy(columnName, out OrmProperty ormProperty))
                    {
                        ormProperty.ConvertAndSetValue(obj, value, columnSourceType, columnName);
                    }
                }
            }

            _activator.OnDeserializedHandle?.Invoke(obj, DefaultStreamingContext);

            return obj;
        }

        private object InnerReadReadonlyObject(DbDataReader reader)
        {
            // Что-бы сконструировать структуру, сначала нужно подготовить параметры его конструктора.
            object[] propValues = new object[_activator.ConstructorArguments.Count];

            for (int i = 0; i < reader.FieldCount; i++)
            {
                // Имя колонки в БД.
                string columnName = reader.GetName(i);

                if (_activator.ConstructorArguments.TryGetValue(columnName, out ConstructorArgument anonProp))
                {
                    object value = reader[i];
                    Type columnSourceType = reader.GetFieldType(i);

                    if (value == DBNull.Value)
                        value = null;

                    object finalValue;
                    if (_activator.Contract.TryGetOrmPropertyFromLazy(columnName, out OrmProperty ormProperty))
                    {
                        finalValue = ormProperty.Convert(value, columnSourceType, columnName);
                    }
                    else
                    {
                        // конвертируем значение.
                        finalValue = SqlTypeConverter.ChangeType(value, anonProp.ParameterType, columnSourceType, columnName);
                    }
                    propValues[anonProp.Index] = finalValue;
                }
            }

            var obj = _activator.CreateInstance(propValues);
            return obj;
        }
    }
}
