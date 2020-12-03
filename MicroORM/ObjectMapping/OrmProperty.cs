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
        private SetValueDelegate? SetValueHandler { get; }
        public TypeConverter? TypeConverter { get; }
        public Type MemberType { get; }
        public bool IsNonNullable { get; }
        public string PropertyName { get; }
        public bool IsWritable => SetValueHandler != null;

        // ctor.
        public OrmProperty(MemberInfo memberInfo)
        {
            Debug.Assert(memberInfo != null);

            PropertyName = memberInfo.Name;
            MemberType = memberInfo.GetMemberType();
            
            var attribute = memberInfo.GetCustomAttribute<TypeConverterAttribute>();
            if (attribute != null)
            {
                if (Type.GetType(attribute.ConverterTypeName) is Type converterType)
                {
                    TypeConverter = StaticCache.TypeConverters.GetOrAdd(converterType, ConverterValueFactory);
                }
                else
                    throw new MicroOrmException($"Unable to resolve converter type {attribute.ConverterTypeName}");
            }

            if (DynamicReflectionDelegateFactory.CreateSet<object>(memberInfo) is Action<object, object?> setValueDelegate)
            {
                SetValueHandler = new SetValueDelegate(setValueDelegate);
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
            if (SetValueHandler != null)
            {
                object? clrValue = ConvertSqlToClrValue(sqlRawValue, sqlColumnType, sqlColumnName);

                SetValueHandler.Invoke(instance, clrValue);
            }
            else
                throw new MicroOrmException($"Property '{PropertyName}' is not writable.");
        }

        public void SetClrValue(object instance, object? clrValue)
        {
            if (SetValueHandler != null)
            {
                Debug.Assert(clrValue != DBNull.Value);

                SetValueHandler.Invoke(instance, clrValue);
            }
            else
                throw new MicroOrmException($"Property '{PropertyName}' is not writable.");
        }
    }
}
