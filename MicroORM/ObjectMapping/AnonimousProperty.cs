using System;
using System.Collections.Generic;
using System.Text;

namespace DanilovSoft.MicroORM.ObjectMapping
{
    internal sealed class AnonimousProperty
    {
        public readonly int Index;
        public readonly Type Type;

        public AnonimousProperty(int index, Type type)
        {
            Index = index;
            Type = type;
        }
    }
}
