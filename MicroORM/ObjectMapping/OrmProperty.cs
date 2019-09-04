using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DanilovSoft.MicroORM.ObjectMapping
{
    internal sealed class OrmProperty
    {
        private static readonly ConcurrentDictionary<Type, ISqlConverter> _converters = new ConcurrentDictionary<Type, ISqlConverter>();
        private readonly MemberInfo _memberInfo;
        public readonly SetMemberValueDelegate SetValueHandler;
        public readonly ISqlConverter Converter;
        public readonly Type MemberType;

        // ctor.
        public OrmProperty(MemberInfo memberInfo)
        {
            _memberInfo = memberInfo;

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

        private ISqlConverter ConverterValueFactory(Type converterType)
        {
            var ctor = DynamicReflectionDelegateFactory.Instance.CreateDefaultConstructor<ISqlConverter>(converterType);
            return ctor.Invoke();
        }

        private object Convert(object value, Type columnType, string columnName)
        {
            if (Converter != null)
            {
                return Converter.Convert(value, MemberType);
            }
            else
            {
                return SqlTypeConverter.ChangeType(value, MemberType, columnType, columnName);
            }
        }

        public void SetValue(object obj, object value, Type columnType, string columnName)
        {
            object finalValue = Convert(value, columnType, columnName);

            SetValueHandler.Invoke(obj, finalValue);
        }
    }
}
