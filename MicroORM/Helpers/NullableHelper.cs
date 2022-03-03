using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DanilovSoft.MicroORM.Helpers;

internal static class NullableHelper
{
    [return: NotNullIfNotNull("value")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? SetNull<T>([MaybeNull] ref T? value) where T : class
    {
        var itemRefCopy = value;
        value = null;
        return itemRefCopy;
    }
}
