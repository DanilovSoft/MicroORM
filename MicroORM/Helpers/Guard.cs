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
    public static void ThrowIfNull([NotNull] object? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is not null)
        {
            return;
        }

        ThrowHelper.ThrowArgumentNull(paramName);
    }
}
