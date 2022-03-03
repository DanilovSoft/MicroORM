using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DanilovSoft.MicroORM.Helpers;

internal static class Guard
{
    /// <exception cref="ArgumentNullException"/>
    [DebuggerStepThrough]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull([NotNull] object? value, [CallerArgumentExpression("value")] string? paramName = null)
    {
        if (value is not null)
        {
            return;
        }
        ThrowHelper.ThrowArgumentNull(paramName);
    }

    /// <exception cref="ArgumentNullException"/>
    [DebuggerStepThrough]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfEmpty(string value, [CallerArgumentExpression("value")] string? paramName = null)
    {
        if (value.Length > 0)
        {
            return;
        }
        ThrowHelper.ThrowArgumentEmpty(paramName);
    }
}
