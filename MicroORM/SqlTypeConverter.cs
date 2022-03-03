using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using DanilovSoft.MicroORM.Helpers;

namespace DanilovSoft.MicroORM;

internal static class SqlTypeConverter
{
    /// <param name="sqlRawValue">Значение которое может быть <see cref="DBNull"/>.</param>
    /// <exception cref="MicroOrmException"/>
    public static object? ConvertSqlToCtorValue(object sqlRawValue, Type sqlColumnType, string sqlColumnName,
        bool isNonNullable, string parameterName, Type toType)
    {
        var sqlValue = ConvertNullableRawSqlType(sqlRawValue, sqlColumnName, isNonNullable, parameterName, "parameter");

        return ConvertSqlToClrType(sqlValue, sqlColumnType, sqlColumnName, toType);
    }

    /// <param name="sqlRawValue">Значение которое может быть <see cref="DBNull"/>.</param>
    /// <param name="sqlColumnName">Используется только для ошибок.</param>
    /// <exception cref="MicroOrmException"/>
    public static T ConvertRawSqlToClrType<T>(object sqlRawValue, Type sqlColumnType, string sqlColumnName)
    {
        var result = ConvertRawSqlToClrType(sqlRawValue, sqlColumnType, sqlColumnName, toType: typeof(T));
        return (T)result!;
    }

    /// <param name="sqlRawValue">Значение которое может быть <see cref="DBNull"/>.</param>
    /// <param name="sqlColumnName">Используется только для ошибок.</param>
    /// <exception cref="MicroOrmException"/>
    public static object? ConvertRawSqlToClrType(object sqlRawValue, Type sqlColumnType, string sqlColumnName, Type toType)
    {
        // Здесь не проверяется NonNullable (!)
        var sqlValue = ConvertNullableRawSqlType(sqlRawValue);

        return ConvertSqlToClrType(sqlValue, sqlColumnType, sqlColumnName, toType);
    }

    /// <param name="sqlValue">Значение которое не может быть <see cref="DBNull"/>.</param>
    /// <param name="sqlColumnName">Используется только для ошибок.</param>
    /// <exception cref="MicroOrmException"/>
    public static object? ConvertSqlToClrType(object? sqlValue, Type sqlColumnType, string sqlColumnName, Type toType)
    {
        Debug.Assert(sqlValue != DBNull.Value);

        var isAssignable = toType.IsAssignableFrom(sqlColumnType);

        if (!isAssignable || sqlValue == null)
        {
            var underlyingNullableValueType = Nullable.GetUnderlyingType(toType);

            if (underlyingNullableValueType == null)
            {
                if (!toType.IsValueType || sqlValue != null)
                {
                    if (toType == sqlColumnType)
                    {
                        return sqlValue;
                    }
                    else
                    {
                        return SqlValueToClr(sqlValue, toType, sqlColumnName);
                    }
                }
                else
                {
                    return ThrowCantMapNullToNotNull(toType, sqlColumnName);
                }
            }
            else
            {
                if (sqlValue == null || underlyingNullableValueType == sqlColumnType)
                {
                    return sqlValue;
                }
                else
                {
                    return SqlValueToNullableClr(sqlValue, underlyingNullableValueType, sqlColumnName);
                }
            }
        }
        else
        {
            return sqlValue;
        }
    }

    /// <exception cref="MicroOrmException"/>
    /// <returns><paramref name="sqlRawValue"/> который может иметь <see langword="null"/> вместо DBNull.</returns>
    public static object? ConvertNullableRawSqlType(object sqlRawValue, string sqlColumnName, bool isNonNullable, string memberName, string memberType)
    {
        if (sqlRawValue != DBNull.Value)
        {
            return sqlRawValue;
        }

        if (!isNonNullable)
        {
            return null;
        }
        else
        {
            ThrowHelper.ThrowCantSetNull(memberName, sqlColumnName, memberType);
        }

        return sqlRawValue;
    }

    /// <exception cref="MicroOrmException"/>
    /// <returns><paramref name="sqlRawValue"/> который может иметь Null вместо DBNull.</returns>
    public static object? ConvertNullableRawSqlType(object sqlRawValue)
    {
        if (sqlRawValue != DBNull.Value)
        {
            return sqlRawValue;
        }
        else
        {
            return null;
        }
    }

    /// <returns>Nothing, it's always throw.</returns>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object? ThrowCantMapNullToNotNull(Type clrType, string sqlColumnName)
    {
        throw new MicroOrmException($"Error converting value {{null}} to type '{clrType.FullName}'. Column name '{sqlColumnName}'.");
    }

    /// <param name="sqlColumnName">Используется только для ошибок.</param>
    private static object? SqlValueToClr(object? sqlValue, Type clrType, string sqlColumnName)
    {
        try
        {
            if (!clrType.IsEnum)
            {
                return Convert.ChangeType(sqlValue, clrType, CultureInfo.InvariantCulture);
            }
            else
            {
                if (sqlValue is string sValue)
                {
                    return Enum.Parse(clrType, sValue, ignoreCase: true);
                }
                else if (sqlValue != null)
                {
                    return Enum.ToObject(clrType, sqlValue);
                }
            }
        }
        catch (Exception ex)
        {
            throw CreateConvertException(sqlValue, clrType, sqlColumnName, ex);
        }

        // sqlValue оказался null.
        return ThrowCantMapNullToNotNull(clrType, sqlColumnName);
    }

    /// <param name="sqlColumnName">Используется только для ошибок.</param>
    private static object? SqlValueToNullableClr(object sqlValue, Type underlyingNullableValueType, string sqlColumnName)
    {
        try
        {
            if (!underlyingNullableValueType.IsEnum)
            {
                return Convert.ChangeType(sqlValue, underlyingNullableValueType, CultureInfo.InvariantCulture);
            }
            else
            {
                if (sqlValue is string sValue)
                {
                    return Enum.Parse(underlyingNullableValueType, sValue, ignoreCase: true);
                }
                else
                {
                    return Enum.ToObject(underlyingNullableValueType, sqlValue);
                }
            }
        }
        catch (Exception ex)
        {
            throw CreateConvertException(sqlValue, underlyingNullableValueType, sqlColumnName, ex);
        }
    }

    private static MicroOrmException CreateConvertException(object? sqlValue, Type toType, string sqlColumnName, Exception innerException)
    {
        return new MicroOrmException($"Error converting value '{sqlValue}' to type '{toType.FullName}'. Column name '{sqlColumnName}'.", innerException);
    }
}
