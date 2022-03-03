using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace DanilovSoft.MicroORM.ObjectMapping;

/// <summary>
/// Хранится в словаре поэтому извлекается быстрее как класс, а не структура.
/// </summary>
//[StructLayout(LayoutKind.Auto)]
[DebuggerDisplay(@"\{ParameterIndex = {ParameterIndex}, ParameterType = {ParameterType.FullName}\}")]
internal sealed class ConstructorArgument
{
    public readonly int ParameterIndex;
    public readonly Type ParameterType;
    public readonly bool IsNonNullable;
    public readonly string ParameterName;
    public readonly TypeConverter? TypeConverter;

    ///// <summary>
    ///// Конструктор для анонимного типа.
    ///// </summary>
    //public ConstructorArgument(int parameterIndex, PropertyInfo property)
    //{
    //    ParameterIndex = parameterIndex;
    //    ParameterName = property.Name;
    //    ParameterType = property.PropertyType;

    //    IsNonNullable = NonNullableConvention.IsNonNullableReferenceType(property);
    //}

    public ConstructorArgument(ParameterInfo parameterInfo)
    {
        ParameterIndex = parameterInfo.Position;
        ParameterName = parameterInfo.Name!;
        ParameterType = parameterInfo.ParameterType;

        IsNonNullable = NonNullableConvention.IsNonNullableReferenceType(parameterInfo);

        if (parameterInfo.GetCustomAttribute<TypeConverterAttribute>() is TypeConverterAttribute typeConverter)
        {
            if (Type.GetType(typeConverter.ConverterTypeName) is Type converterType)
            {
                TypeConverter = StaticCache.TypeConverters.GetOrAdd(converterType, ConverterValueFactory);
            }
            else
            {
                throw new MicroOrmException($"Unknown converter tyoe '{typeConverter.ConverterTypeName}'");
            }
        }
    }

    private static TypeConverter ConverterValueFactory(Type converterType)
    {
        var ctor = DynamicReflectionDelegateFactory.CreateDefaultConstructor<TypeConverter>(converterType);
        return ctor.Invoke();
    }
}
