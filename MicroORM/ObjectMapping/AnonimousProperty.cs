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
    [DebuggerDisplay(@"\{Index = {Index}, ParameterType = {ParameterType.FullName}\}")]
    internal sealed class ConstructorArgument
    {
        public readonly int Index;
        public readonly Type ParameterType;
        public readonly bool IsNonNullable;
        public readonly string ParameterName;

        public ConstructorArgument(int index, PropertyInfo property)
        {
            Index = index;
            ParameterName = property.Name;
            ParameterType = property.PropertyType;

            IsNonNullable = NonNullableConvention.IsNonNullableReferenceType(property);
        }

        public ConstructorArgument(int index, ParameterInfo parameterInfo)
        {
            Index = index;
            ParameterName = parameterInfo.Name!;
            ParameterType = parameterInfo.ParameterType;

            IsNonNullable = NonNullableConvention.IsNonNullableReferenceType(parameterInfo);
        }
    }
}
