namespace DanilovSoft.MicroORM
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    internal static class ThrowHelper
    {
        /// <exception cref="MicroOrmException"/>
        [DoesNotReturn]
        [DebuggerStepThrough]
        public static void ThrowCantSetNull(string memberName, string sqlColumnName, string memberType)
        {
            throw new MicroOrmException($"Failed to set Null value for {memberType} '{memberName}' " +
                $"because it does not allow Null values. SQL column name '{sqlColumnName}'.");
        }
    }
}
