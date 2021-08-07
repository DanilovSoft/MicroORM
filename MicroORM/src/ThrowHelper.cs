namespace DanilovSoft.MicroORM
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;

    internal static class ThrowHelper
    {
        /// <exception cref="MicroOrmException"/>
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowCantSetNull(string memberName, string sqlColumnName, string memberType)
        {
            throw new MicroOrmException($"Failed to set Null value for {memberType} '{memberName}' " +
                $"because it does not allow Null values. SQL column name '{sqlColumnName}'.");
        }

        /// <exception cref="ThrowObjectDisposed"/>
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowObjectDisposed(string? objectName)
        {
            throw new ObjectDisposedException(objectName);
        }

        /// <exception cref="ThrowObjectDisposed"/>
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowObjectDisposed<T>()
        {
            throw new ObjectDisposedException(typeof(T).Name);
        }

        /// <exception cref="ArgumentNullException"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AssertNotNull<T>([NotNull] T? value, string? paramName)
        {
            if (value != null)
            {
                return;
            }
            ThrowArgumentNull(paramName);
        }

        /// <exception cref="ArgumentNullException"/>
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentNull(string? paramName)
        {
            throw new ArgumentNullException(paramName);
        }
    }
}
