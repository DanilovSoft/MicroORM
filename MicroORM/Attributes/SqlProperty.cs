using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace DanilovSoft.MicroORM
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class SqlPropertyAttribute : Attribute
    {
        internal readonly string? Name;

        public SqlPropertyAttribute()
        {
            Name = null;
        }

        public SqlPropertyAttribute(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}
