using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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
