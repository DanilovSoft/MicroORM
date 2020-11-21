using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM
{
    internal static class SqlTypeConverter
    {
        /// <param name="sqlColumnName">Используется только для ошибок.</param>
        public static T ChangeType<T>(object? value, Type columnType, string sqlColumnName)
        {
            object? convertedValue = ChangeType(value, typeof(T), columnType, sqlColumnName);
            return (T)convertedValue!;
        }

        /// <param name="sqlColumnName">Используется только для ошибок.</param>
        public static object? ChangeType(object? value, Type propertyType, Type columnType, string sqlColumnName)
        {
            bool isAssignable = propertyType.IsAssignableFrom(columnType);
            if (!isAssignable || value == null)
            {
                Type? nullableType = Nullable.GetUnderlyingType(propertyType);
                if (nullableType == null)
                {
                    if (!propertyType.IsValueType || value != null)
                    {
                        if (propertyType != columnType)
                        {
                            value = ChangeType(value, propertyType, sqlColumnName);
                        }
                    }
                    else
                    {
                        throw new MicroOrmException($"Error converting value {{null}} to type '{propertyType.FullName}'. Column name '{sqlColumnName}'.");
                    }
                }
                else
                {
                    if (value != null && nullableType != columnType)
                    {
                        value = ChangeType(value, nullableType, sqlColumnName);
                    }
                }
            }
            return value;
        }

        /// <param name="sqlColumnName">Используется только для ошибок.</param>
        private static object? ChangeType(object? value, Type conversionType, string sqlColumnName)
        {
            try
            {
                if (!conversionType.IsEnum)
                {
                    return Convert.ChangeType(value, conversionType, CultureInfo.InvariantCulture);
                }
                else
                {
                    if (value is string sValue)
                    {
                        return Enum.Parse(conversionType, sValue);
                    }
                    else
                    {
                        return Enum.ToObject(conversionType, value);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new MicroOrmException($"Error converting value '{value}' to type '{conversionType.FullName}'. Column name '{sqlColumnName}'.", ex);
            }
        }
    }
}
