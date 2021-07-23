using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DanilovSoft.MicroORM.Helpers
{
    public static class NullableHelper
    {
        [return: NotNullIfNotNull("value")]
        public static T? SetNull<T>(ref T? value) where T : class
        {
            var itemRefCopy = value;
            value = null;
            return itemRefCopy;
        }

        [return: NotNullIfNotNull("value")]
        public static T? SetNull<T>(ref T? value) where T : struct
        {
            var itemRefCopy = value;
            value = null;
            return itemRefCopy;
        }

        /// <summary>
        /// Бросает исключение если значение <paramref name="value"/> оказалось Null.
        /// </summary>
        /// <remarks>Дополнительно останавливает отладчик и проваливает тест с соответствующим сообщением.</remarks>
        /// <exception cref="ArgumentNullException"/>
        public static T AssertNotNull<T>([NotNull] T? value, string? message = null, [CallerMemberName] string? propertyName = null)
        {
            if (value == null)
            {
                message ??= $"The property '{propertyName}' turned out to be not initialized and had a Null value";
                throw new ArgumentNullException(message);
            }

            return value;
        }

        /// <summary>
        /// Бросает исключение если значение <paramref name="value"/> оказалось Null.
        /// </summary>
        /// <remarks>Дополнительно останавливает отладчик и проваливает тест с соответствующим сообщением.</remarks>
        /// <exception cref="ArgumentNullException"/>
        public static T AssertNotNull<T>([NotNull] T? value, string? message = null, [CallerMemberName] string? propertyName = null) where T : struct
        {
            if (value == null)
            {
                message ??= $"The property '{propertyName}' turned out to be not initialized and had a Null value";
                throw new ArgumentNullException(message);
            }

            return value.Value;
        }
    }
}
