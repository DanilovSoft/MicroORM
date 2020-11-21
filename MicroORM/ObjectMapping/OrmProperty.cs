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
        public readonly TypeConverter? Converter;
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
                Converter = StaticCache.TypeConverters.GetOrAdd(attribute.ConverterType, ConverterValueFactory);
            }

            Action<object, object?>? setAction = DynamicReflectionDelegateFactory.CreateSet<object>(memberInfo);
            if (setAction != null)
            {
                SetValueHandler = new SetValueDelegate(setAction);
            }

            IsNonNullable = NonNullableConvention.IsNonNullableReferenceType(memberInfo);
        }

        private static TypeConverter ConverterValueFactory(Type converterType)
        {
            var ctor = DynamicReflectionDelegateFactory.CreateDefaultConstructor<TypeConverter>(converterType);
            return ctor.Invoke();
        }

        /// <param name="sqlColumnName">Используется только для ошибок.</param>
        public object? Convert(object? value, Type columnSourceType, string sqlColumnName)
        {
            if (Converter != null)
            {
                if (Converter.CanConvertFrom(columnSourceType))
                {
                    return Converter.ConvertFrom(value);
                }
                else
                // Безусловно вызываем конвертацию.
                {
                    return Converter.ConvertTo(value, MemberType);
                }
            }
            else
            {
                return SqlTypeConverter.ChangeType(value, MemberType, columnSourceType, sqlColumnName);
            }
        }

        /// <param name="sqlColumnName">Используется только для ошибок.</param>
        public void ConvertAndSetValue(object obj, object? value, Type columnSourceType, string sqlColumnName)
        {
            Debug.Assert(SetValueHandler != null);

            object? finalValue = Convert(value, columnSourceType, sqlColumnName);

            SetValueHandler.Invoke(obj, finalValue);
        }
    }
}
