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
        [DebuggerStepThrough]
        public static Type GetMemberType(this MemberInfo memberInfo)
        {
            return (memberInfo as PropertyInfo)?.PropertyType ?? ((FieldInfo)memberInfo).FieldType;
        }
    }
}
