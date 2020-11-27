using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace DanilovSoft.MicroORM.ObjectMapping
{
    /// <summary>
    /// Хранится в словаре поэтому извлекается быстрее как класс, а не как структура.
    /// </summary>
    internal sealed class OrmProperty
    {
        public readonly SetValueDelegate? SetValueHandler;
        public readonly TypeConverter? TypeConverter;
        public readonly Type MemberType;
        public readonly bool IsNonNullable;
        public readonly string PropertyName;

        // ctor.
        public OrmProperty(MemberInfo memberInfo)
        {
            Debug.Assert(memberInfo != null);

            PropertyName = memberInfo.Name;
            MemberType = memberInfo.GetMemberType();
            
            var attribute = memberInfo.GetCustomAttribute<SqlConverterAttribute>();
            if (attribute != null)
            {
                TypeConverter = StaticCache.TypeConverters.GetOrAdd(attribute.ConverterType, ConverterValueFactory);
            }

            Action<object, object?>? setAction = DynamicReflectionDelegateFactory.CreateSet<object>(memberInfo);
            if (setAction != null)
            {
                SetValueHandler = new SetValueDelegate(setAction);
            }
            else
            {
                Debug.Assert(false, "Это свойство только для чтения");
                throw new InvalidOperationException();
            }

            IsNonNullable = NonNullableConvention.IsNonNullableReferenceType(memberInfo);
        }

        private static TypeConverter ConverterValueFactory(Type converterType)
        {
            var ctor = DynamicReflectionDelegateFactory.CreateDefaultConstructor<TypeConverter>(converterType);
            return ctor.Invoke();
        }

        /// <param name="sqlColumnName">Используется только для ошибок.</param>
        /// <exception cref="MicroOrmException"/>
        /// <returns>CLR значение.</returns>
        public object? ConvertSqlToClrValue(object sqlRawValue, Type sqlColumnType, string sqlColumnName)
        {
            object? sqlValue = SqlTypeConverter.ConvertNullableRawSqlType(sqlRawValue, sqlColumnName, IsNonNullable, PropertyName, "property");

            if (TypeConverter != null)
            {
                if (TypeConverter.CanConvertFrom(sqlColumnType))
                {
                    return TypeConverter.ConvertFrom(sqlValue);
                }
                else
                // Безусловно вызываем конвертацию.
                {
                    return TypeConverter.ConvertTo(sqlValue, MemberType);
                }
            }
            else
            {
                return SqlTypeConverter.ConvertSqlToClrType(sqlValue, sqlColumnType, sqlColumnName, MemberType);
            }
        }

        /// <param name="sqlRawValue">Может быть <see cref="DBNull"/>.</param>
        /// <param name="sqlColumnName">Используется только для ошибок.</param>
        public void ConvertAndSetValue(object instance, object sqlRawValue, Type sqlColumnType, string sqlColumnName)
        {
            Debug.Assert(SetValueHandler != null);

            object? clrValue = ConvertSqlToClrValue(sqlRawValue, sqlColumnType, sqlColumnName);

            SetValueHandler.Invoke(instance, clrValue);
        }

        public void SetClrValue(object instance, object? clrValue)
        {
            Debug.Assert(clrValue != DBNull.Value);
            Debug.Assert(SetValueHandler != null);

            SetValueHandler.Invoke(instance, clrValue);
        }
    }
}
