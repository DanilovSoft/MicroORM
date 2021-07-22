using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace DanilovSoft.MicroORM
{
    internal static class SqlTypeConverter
    {
        /// <param name="sqlRawValue">Значение которое может быть <see cref="DBNull"/>.</param>
        /// <exception cref="MicroOrmException"/>
        public static object? ConvertSqlToCtorValue(object sqlRawValue, Type sqlColumnType, string sqlColumnName, 
            bool isNonNullable, string parameterName, Type toType)
        {
            object? sqlValue = ConvertNullableRawSqlType(sqlRawValue, sqlColumnName, isNonNullable, parameterName, "parameter");

            return ConvertSqlToClrType(sqlValue, sqlColumnType, sqlColumnName, toType);
        }

        /// <param name="sqlRawValue">Значение которое может быть <see cref="DBNull"/>.</param>
        /// <param name="sqlColumnName">Используется только для ошибок.</param>
        /// <exception cref="MicroOrmException"/>
        public static T? ConvertRawSqlToClrType<T>(object sqlRawValue, Type sqlColumnType, string sqlColumnName)
        {
            //bool isNonNullable = NonNullableConvention.IsNonNullableReferenceType(typeof(T));

            return (T?)ConvertRawSqlToClrType(sqlRawValue, sqlColumnType, sqlColumnName, toType: typeof(T));
        }

        /// <param name="sqlRawValue">Значение которое может быть <see cref="DBNull"/>.</param>
        /// <param name="sqlColumnName">Используется только для ошибок.</param>
        /// <exception cref="MicroOrmException"/>
        public static object? ConvertRawSqlToClrType(object sqlRawValue, Type sqlColumnType, string sqlColumnName, Type toType)
        {
            // Здесь не проверяется NonNullable (!)
            object? sqlValue = ConvertNullableRawSqlType(sqlRawValue);

            return ConvertSqlToClrType(sqlValue, sqlColumnType, sqlColumnName, toType);
        }

        /// <param name="sqlValue">Значение которое не может быть <see cref="DBNull"/>.</param>
        /// <param name="sqlColumnName">Используется только для ошибок.</param>
        /// <exception cref="MicroOrmException"/>
        public static object? ConvertSqlToClrType(object? sqlValue, Type sqlColumnType, string sqlColumnName, Type toType)
        {
            Debug.Assert(sqlValue != DBNull.Value);

            bool isAssignable = toType.IsAssignableFrom(sqlColumnType);
            if (!isAssignable || sqlValue == null)
            {
                Type? nullableType = Nullable.GetUnderlyingType(toType);
                if (nullableType == null)
                {
                    if (!toType.IsValueType || sqlValue != null)
                    {
                        if (toType != sqlColumnType)
                        {
                            sqlValue = ChangeType(sqlValue, toType, sqlColumnName);
                        }
                    }
                    else
                    {
                        throw new MicroOrmException($"Error converting value {{null}} to type '{toType.FullName}'. Column name '{sqlColumnName}'.");
                    }
                }
                else
                {
                    if (sqlValue != null && nullableType != sqlColumnType)
                    {
                        sqlValue = ChangeType(sqlValue, nullableType, sqlColumnName);
                    }
                }
            }
            return sqlValue;
        }

        /// <param name="sqlColumnName">Используется только для ошибок.</param>
        private static object? ChangeType(object? sqlValue, Type toType, string sqlColumnName)
        {
            try
            {
                if (!toType.IsEnum)
                {
                    return Convert.ChangeType(sqlValue, toType, CultureInfo.InvariantCulture);
                }
                else
                {
                    if (sqlValue is string sValue)
                    {
                        return Enum.Parse(toType, sValue);
                    }
                    else
                    {
                        return Enum.ToObject(toType, sqlValue);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new MicroOrmException($"Error converting value '{sqlValue}' to type '{toType.FullName}'. Column name '{sqlColumnName}'.", ex);
            }
        }

        /// <exception cref="MicroOrmException"/>
        /// <returns><paramref name="sqlRawValue"/> который может иметь Null вместо DBNull.</returns>
        public static object? ConvertNullableRawSqlType(object sqlRawValue, string sqlColumnName, bool isNonNullable, string memberName, string memberType)
        {
            if (sqlRawValue != DBNull.Value)
                return sqlRawValue;

            if (!isNonNullable)
            {
                return null;
            }
            else
                ThrowHelper.ThrowCantSetNull(memberName, sqlColumnName, memberType);
            
            return sqlRawValue;
        }

        /// <exception cref="MicroOrmException"/>
        /// <returns><paramref name="sqlRawValue"/> который может иметь Null вместо DBNull.</returns>
        public static object? ConvertNullableRawSqlType(object sqlRawValue)
        {
            if (sqlRawValue != DBNull.Value)
                return sqlRawValue;
            else
                return null;
        }
    }
}
