namespace DanilovSoft.MicroORM
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal static class ThrowHelper
    {
        [DoesNotReturn]
        [DebuggerStepThrough]
        public static void ThrowCantSetNull(string parameterName, string sqlColumnName)
            => throw new MicroOrmException($"Failed to set Null value for property '{parameterName}' " +
                $"because it does not allow Null values. Column name '{sqlColumnName}'.");
    }
}
