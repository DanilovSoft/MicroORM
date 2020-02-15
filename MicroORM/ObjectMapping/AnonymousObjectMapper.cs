using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM.ObjectMapping
{
    [DebuggerDisplay(@"\{Маппер анонимного типа\}")]
    internal readonly struct AnonymousObjectMapper<T> where T : class
    {
        private static readonly Type ThisType = typeof(T);
        private readonly ContractActivator _activator;
        private readonly DbDataReader _reader;

        public AnonymousObjectMapper(DbDataReader reader)
        {
            _reader = reader;
            _activator = StaticCache.FromLazyAnonimousActivator(ThisType);
        }

        public T ReadObject()
        {
            // Что-бы сконструировать анонимный тип, сначала нужно подготовить параметры его конструктора.
            object[] propValues = new object[_activator.ConstructorArguments.Count];

            for (int i = 0; i < _reader.FieldCount; i++)
            {
                // Имя колонки в БД.
                string columnName = _reader.GetName(i);

                if(_activator.ConstructorArguments.TryGetValue(columnName, out ConstructorArgument anonProp))
                {
                    object value = _reader[i];
                    Type columnType = _reader.GetFieldType(i);

                    if (value == DBNull.Value)
                        value = null;

                    // конвертируем значение.
                    propValues[anonProp.Index] = SqlTypeConverter.ChangeType(value, anonProp.ParameterType, columnType, columnName);
                }
            }

            // Анонимный тип является классом поэтому можем сразу кастовать в строгий тип.
            var obj = _activator.CreateInstance(propValues) as T;
            return obj;
        }
    }
}
