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

        // ctor.
        public OrmProperty(MemberInfo memberInfo)
        {
            if(memberInfo is PropertyInfo propertyInfo)
            {
                MemberType = propertyInfo.PropertyType;
            }
            else
            {
                var fieldInfo = (FieldInfo)memberInfo;
                MemberType = fieldInfo.FieldType;
            }

            var attribute = memberInfo.GetCustomAttribute<SqlConverterAttribute>();
            if (attribute == null)
            {
                Converter = null;
            }
            else
            {
                Converter = StaticCache.TypeConverters.GetOrAdd(attribute.ConverterType, ConverterValueFactory);
            }

            Action<object, object?>? setAction = DynamicReflectionDelegateFactory.CreateSet<object>(memberInfo);
            if (setAction != null)
            {
                SetValueHandler = new SetValueDelegate(setAction);
            }
            else
            {
                SetValueHandler = null;
            }
        }

        private static TypeConverter ConverterValueFactory(Type converterType)
        {
            var ctor = DynamicReflectionDelegateFactory.CreateDefaultConstructor<TypeConverter>(converterType);
            return ctor.Invoke();
        }

        public object? Convert(object? value, Type columnSourceType, string columnName)
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
                return SqlTypeConverter.ChangeType(value, MemberType, columnSourceType, columnName);
            }
        }

        public void ConvertAndSetValue(object obj, object? value, Type columnSourceType, string columnName)
        {
            Debug.Assert(SetValueHandler != null);

            object? finalValue = Convert(value, columnSourceType, columnName);

            SetValueHandler.Invoke(obj, finalValue);
        }
    }
}
