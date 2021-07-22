namespace DanilovSoft.MicroORM
{
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;

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
