namespace DanilovSoft.MicroORM
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    internal static class MemberInfoExtensions
    {
        //[DebuggerStepThrough]
        public static Type GetMemberType(this MemberInfo memberInfo) => memberInfo switch
        {
            PropertyInfo p => p.PropertyType,
            FieldInfo f => f.FieldType,
            _ => throw new NotSupportedException()
        };
    }
}
