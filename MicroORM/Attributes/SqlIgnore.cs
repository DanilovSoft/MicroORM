using System;

namespace DanilovSoft.MicroORM;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class SqlIgnoreAttribute : Attribute
{

}
