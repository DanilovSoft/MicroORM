using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public ConstructorArgument(int index, Type type)
        {
            Index = index;
            ParameterType = type;
        }
    }
}
