using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM.ObjectMapping
{
    internal sealed class AnonymousObjectMapper<T>
    {
        private static readonly Type _thisType = typeof(T);
        private readonly ContractActivator _activator;

        public AnonymousObjectMapper()
        {
            _activator = DynamicActivator.GetAnonimousActivator(_thisType);
        }

        public T ReadObject(DbDataReader reader)
        {
            // Что-бы сконструировать анонимный тип, сначала нужно подготовить параметры его конструктора.
            object[] propValues = new object[_activator.AnonimousProperties.Count];

            for (int i = 0; i < reader.FieldCount; i++)
            {
                // Имя колонки в БД.
                string columnName = reader.GetName(i);

                if(_activator.AnonimousProperties.TryGetValue(columnName, out AnonimousProperty anonProp))
                {
                    object value = reader[i];
                    Type columnType = reader.GetFieldType(i);

                    if (value == DBNull.Value)
                        value = null;

                    // конвертируем значение.
                    object finalValue = SqlTypeConverter.ChangeType(value, anonProp.Type, columnType, columnName);

                    propValues[anonProp.Index] = finalValue;
                }
            }

            var obj = (T)_activator.CreateAnonimousInstance(propValues);
            return obj;
        }
    }
}
