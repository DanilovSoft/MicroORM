using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace DanilovSoft.MicroORM.ObjectMapping
{
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

        /// <summary>
        /// Конструктор для анонимного типа.
        /// </summary>
        public ConstructorArgument(int parameterIndex, PropertyInfo property)
        {
            ParameterIndex = parameterIndex;
            ParameterName = property.Name;
            ParameterType = property.PropertyType;

            IsNonNullable = NonNullableConvention.IsNonNullableReferenceType(property);
        }

        public ConstructorArgument(ParameterInfo parameterInfo)
        {
            ParameterIndex = parameterInfo.Position;
            ParameterName = parameterInfo.Name!;
            ParameterType = parameterInfo.ParameterType;

            IsNonNullable = NonNullableConvention.IsNonNullableReferenceType(parameterInfo);
        }
    }
}
