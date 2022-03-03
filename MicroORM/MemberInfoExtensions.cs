namespace DanilovSoft.MicroORM;

using System;
using System.Reflection;

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
