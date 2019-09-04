using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SqlConverterAttribute : Attribute
    {
        internal readonly Type ConverterType;

        public SqlConverterAttribute(Type converter)
        {
            ConverterType = converter;
        }
    }
}
