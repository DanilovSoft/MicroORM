using System.Runtime.CompilerServices;

internal static class GlobalVars
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? SetNull<T>(ref T? value) where T : class
    {
        var refCopy = value;
        value = null;
        return refCopy;
    }
}
