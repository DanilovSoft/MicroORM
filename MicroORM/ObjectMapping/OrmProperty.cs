using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace DanilovSoft.MicroORM.ObjectMapping
{
    internal sealed class OrmProperty
    {
        private static readonly ConcurrentDictionary<Type, TypeConverter> _converters = new ConcurrentDictionary<Type, TypeConverter>();
        //private readonly MemberInfo _memberInfo;
        public readonly SetMemberValueDelegate SetValueHandler;
        public readonly TypeConverter Converter;
        public readonly Type MemberType;

        // ctor.
        public OrmProperty(MemberInfo memberInfo)
        {
            //_memberInfo = memberInfo;

            if(memberInfo is PropertyInfo propertyInfo)
            {
                MemberType = propertyInfo.PropertyType;
            }
            else
            {
                var fieldInfo = (FieldInfo)memberInfo;
                MemberType = fieldInfo.FieldType;
            }

            SetValueHandler = new SetMemberValueDelegate(DynamicReflectionDelegateFactory.Instance.CreateSet<object>(memberInfo));
            var attribute = memberInfo.GetCustomAttribute<SqlConverterAttribute>();
            if (attribute != null)
            {
                Converter = _converters.GetOrAdd(attribute.ConverterType, ConverterValueFactory);
            }
        }

        private TypeConverter ConverterValueFactory(Type converterType)
        {
            var ctor = DynamicReflectionDelegateFactory.Instance.CreateDefaultConstructor<TypeConverter>(converterType);
            return ctor.Invoke();
        }

        private object Convert(object value, Type columnSourceType, string columnName)
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

        public void SetValue(object obj, object value, Type columnSourceType, string columnName)
        {
            object finalValue = Convert(value, columnSourceType, columnName);

            SetValueHandler.Invoke(obj, finalValue);
        }
    }
}
