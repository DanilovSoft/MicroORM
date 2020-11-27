using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

namespace DanilovSoft.MicroORM.ObjectMapping
{
    [StructLayout(LayoutKind.Auto)]
    internal readonly struct ObjectMapper<T>
    {
        private static readonly StreamingContext DefaultStreamingContext;
        private readonly ContractActivator _activator;
        private readonly DbDataReader _reader;
        private readonly SqlORM _sqlOrm;

        // ctor.
        public ObjectMapper(DbDataReader reader, SqlORM sqlOrm)
        {
            _reader = reader;
            _sqlOrm = sqlOrm;

            // Инициализирует из ленивого хранилища.
            _activator = StaticCache.FromLazyActivator(typeof(T));
        }

        public object ReadObject()
        {
            if (_activator.IsEmptyCtor)
            {
                return InnerReadObject(_reader);
            }
            else
            {
                return ReadToNonEmptyCtor(_reader);
            }
        }

        private object InnerReadObject(DbDataReader reader)
        {
            object dbo = _activator.CreateInstance();

            _activator.OnDeserializingHandle?.Invoke(dbo, DefaultStreamingContext);

            if (reader.FieldCount > 0)
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    // Имя колонки в БД.
                    string sqlColumnName = reader.GetName(i);

                    if (_activator.Contract.TryGetOrmProperty(sqlColumnName, out OrmProperty? ormProperty))
                    {
                        MapDboProperty(reader, dbo, i, sqlColumnName, ormProperty);
                    }
                    else if (_sqlOrm.UsePascalCaseNamingConvention)
                    {
                        if (_activator.Contract.TryGetOrmProperty(sqlColumnName.SnakeToPascalCase(), out ormProperty))
                        {
                            MapDboProperty(reader, dbo, i, sqlColumnName, ormProperty);
                        }
                    }
                }
            }

            _activator.OnDeserializedHandle?.Invoke(dbo, DefaultStreamingContext);
            return dbo;
        }

        private static void MapDboProperty(DbDataReader reader, object dbo, int ordinal, string sqlColumnName, OrmProperty ormProperty)
        {
            object sqlRawValue = ReadSqlRawValue(reader, ordinal, out Type sqlColumnType);
            ormProperty.ConvertAndSetValue(dbo, sqlRawValue, sqlColumnType, sqlColumnName);
        }

        /// <returns>Значение которое может быть <see cref="DBNull"/>.</returns>
        private static object ReadSqlRawValue(DbDataReader reader, int ordinal, out Type sqlColumnType)
        {
            sqlColumnType = reader.GetFieldType(ordinal);
            object sqlRawValue = reader[ordinal];
            return sqlRawValue;
        }

        [StructLayout(LayoutKind.Auto)]
        private readonly struct PropertyToSet
        {
            /// <summary>
            /// Не может быть <see cref="DBNull"/>.
            /// </summary>
            public readonly object? ClrValue;
            public readonly OrmProperty OrmProperty;

            public PropertyToSet(object? clrValue, OrmProperty ormProperty)
            {
                Debug.Assert(clrValue != DBNull.Value);

                ClrValue = clrValue;
                OrmProperty = ormProperty;
            }
        }

        private object ReadToNonEmptyCtor(DbDataReader reader)
        {
            Debug.Assert(_activator != null);
            Debug.Assert(_activator.ConstructorArguments != null);

            // Что-бы сконструировать структуру, сначала нужно подготовить параметры его конструктора.
            object?[] ctorParamClrValues = new object[_activator.ConstructorArguments.Count];

            // Будем запоминать индексы замапленых параметров что-бы в конце определить все ли замапились.
            Span<bool> mapped = stackalloc bool[_activator.ConstructorArguments.Count];

            // После конструктора нужно инициализировать свойства/поля.
            List<PropertyToSet>? sqlAccumulatedValues = null;

            // Односторонний маппингт: БД -> DTO, поэтому пляшем от БД.
            for (int i = 0; i < reader.FieldCount; i++)
            {
                // Имя колонки в БД.
                string sqlColumnName = reader.GetName(i);

                // Допускается иметь в SQL больше полей чем в ДТО.
                if (_activator.ConstructorArguments.TryGetValue(sqlColumnName, out ConstructorArgument? ctorArg))
                {
                    ctorParamClrValues[ctorArg.ParameterIndex] = AccumulateCtorParameter(reader, i, sqlColumnName, ctorArg);
                    mapped[ctorArg.ParameterIndex] = true;  // Запомним что этот параметр мы замапили.
                }
                else if (_sqlOrm.UsePascalCaseNamingConvention)
                {
                    string pascalSqlColumn = sqlColumnName.SnakeToPascalCase();

                    // Параметр с маленькой буквы.
                    if (_activator.ConstructorArguments.TryGetValue(pascalSqlColumn, out ctorArg))
                    {
                        ctorParamClrValues[ctorArg.ParameterIndex] = AccumulateCtorParameter(reader, i, sqlColumnName, ctorArg);
                        mapped[ctorArg.ParameterIndex] = true;  // Запомним что этот параметр мы замапили.
                    }
                    else
                    {
                        // Параметр с большой буквы.
                        if (_activator.ConstructorArguments.TryGetValue(pascalSqlColumn.ToLowerFirstLetter(), out ctorArg))
                        {
                            ctorParamClrValues[ctorArg.ParameterIndex] = AccumulateCtorParameter(reader, i, sqlColumnName, ctorArg);
                            mapped[ctorArg.ParameterIndex] = true;  // Запомним что этот параметр мы замапили.
                        }
                        else
                        // Ищем не в конструкторе, а среди свойств/полей.
                        {
                            if (!_activator.Contract.TryGetOrmProperty(sqlColumnName, out OrmProperty? ormProperty))
                            {
                                if (_sqlOrm.UsePascalCaseNamingConvention)
                                    _activator.Contract.TryGetOrmProperty(sqlColumnName.SnakeToPascalCase(), out ormProperty);
                            }

                            if (ormProperty != null)
                                AccumulateSqlValue(reader, i, sqlColumnName, ormProperty, ref sqlAccumulatedValues);
                        }
                    }
                }
                else
                // Ищем не в конструкторе, а среди свойств/полей.
                {
                    if (_activator.Contract.TryGetOrmProperty(sqlColumnName, out OrmProperty? ormProperty))
                    {
                        AccumulateSqlValue(reader, i, sqlColumnName, ormProperty, ref sqlAccumulatedValues);
                    }
                }
            }

            ValidateAllParametersMapped(mapped, _activator.ConstructorArguments.Values);

            object dbo = _activator.CreateInstance(ctorParamClrValues);
            _activator.OnDeserializingHandle?.Invoke(dbo, DefaultStreamingContext);

            // Инициализация свойств найденных в SQL.
            if (sqlAccumulatedValues != null)
            {
                foreach (var acumValue in sqlAccumulatedValues)
                    acumValue.OrmProperty.SetClrValue(dbo, acumValue.ClrValue);
            }

            _activator.OnDeserializedHandle?.Invoke(dbo, DefaultStreamingContext);
            return dbo;
        }

        /// <param name="sqlColumnName">Используется только для ошибок.</param>
        /// <exception cref="MicroOrmException"/>
        private static void AccumulateSqlValue(DbDataReader reader, int ordinal, string sqlColumnName, OrmProperty ormProperty, ref List<PropertyToSet>? sqlAccumulatedValues)
        {
            object sqlRawValue = ReadSqlRawValue(reader, ordinal, out Type sqlColumnType);
            object? clrValue = ormProperty.ConvertSqlToClrValue(sqlRawValue, sqlColumnType, sqlColumnName);

            sqlAccumulatedValues ??= new();
            sqlAccumulatedValues.Add(new PropertyToSet(clrValue, ormProperty));
        }

        /// <exception cref="MicroOrmException"/>
        private static void ValidateAllParametersMapped(Span<bool> mapped, IEnumerable<ConstructorArgument> constructorArguments)
        {
            // Не допускается иметь в конструкторе DBO больше параметров чем полей в SQL.
            foreach (var arg in constructorArguments)
            {
                if (mapped[arg.ParameterIndex])
                    continue;

                throw new MicroOrmException($"В результате SQL запроса не найдена колонка соответствующая аргументу конструктора \"{arg.ParameterName}\"");
            }
        }

        /// <exception cref="MicroOrmException"/>
        /// <returns>CLR значение.</returns>
        private object? AccumulateCtorParameter(DbDataReader reader, int ordinal, string sqlColumnName, ConstructorArgument ctorArg)
        {
            object sqlRawValue = ReadSqlRawValue(reader, ordinal, out Type sqlColumnType);

            if (_activator.Contract.TryGetOrmProperty(sqlColumnName, out OrmProperty? ormProperty))
            {
                return ormProperty.ConvertSqlToClrValue(sqlRawValue, sqlColumnType, sqlColumnName);
            }
            else
            {
                // конвертируем значение.
                return SqlTypeConverter.ConvertSqlToCtorValue(sqlRawValue, sqlColumnType, sqlColumnName, ctorArg.IsNonNullable, ctorArg.ParameterName, ctorArg.ParameterType);
            }
        }
    }
}
