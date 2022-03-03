using System;

namespace DanilovSoft.MicroORM;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class SqlPropertyAttribute : Attribute
{
    public SqlPropertyAttribute()
    {
        Name = null;
    }

    public SqlPropertyAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public string? Name { get; }
}
