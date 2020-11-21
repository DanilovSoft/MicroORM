﻿using System;
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
            object obj = _activator.CreateInstance();

            _activator.OnDeserializingHandle?.Invoke(obj, DefaultStreamingContext);

            if (reader.FieldCount > 0)
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    // Имя колонки в БД.
                    string sqlColumnName = reader.GetName(i);
                    string convertedSqlColumnName = _sqlOrm.UseSnakeCaseNamingConvention
                        ? sqlColumnName.SnakeToPascalCase()
                        : sqlColumnName;

                    if (_activator.Contract.TryGetOrmPropertyFromLazy(convertedSqlColumnName, out OrmProperty? ormProperty))
                    {
                        Type sqlColumnType = reader.GetFieldType(i);
                        object? value = reader[i];

                        if (value == DBNull.Value)
                        {
                            if (!ormProperty.IsNonNullable)
                            {
                                value = null;
                            }
                            else
                                ThrowHelper.ThrowCantSetNull(ormProperty.PropertyName, sqlColumnName);
                        }

                        ormProperty.ConvertAndSetValue(obj, value, sqlColumnType, sqlColumnName);
                    }
                }
            }

            _activator.OnDeserializedHandle?.Invoke(obj, DefaultStreamingContext);

            return obj;
        }

        private object ReadToNonEmptyCtor(DbDataReader reader)
        {
            Debug.Assert(_activator != null);
            Debug.Assert(_activator.ConstructorArguments != null);

            // Что-бы сконструировать структуру, сначала нужно подготовить параметры его конструктора.
            object?[] propValues = new object[_activator.ConstructorArguments.Count];

            // Будем запоминать индексы замапленых параметров что-бы в конце определить все ли замапились.
            Span<bool> mapped = stackalloc bool[_activator.ConstructorArguments.Count];
            
            // Односторонний маппингт: БД -> DTO, поэтому пляшем от БД.
            for (int i = 0; i < reader.FieldCount; i++)
            {
                // Имя колонки в БД.
                string sqlColumnName = reader.GetName(i);

                string convertedSqlColumnName = _sqlOrm.UseSnakeCaseNamingConvention
                    ? sqlColumnName.SnakeToPascalCase()
                    : sqlColumnName;

                // Допускается иметь в SQL больше полей чем в ДТО.
                if (_activator.ConstructorArguments.TryGetValue(convertedSqlColumnName, out ConstructorArgument? ctorArg))
                {
                    object? value = reader[i];
                    Type columnSqlType = reader.GetFieldType(i);

                    if (value == DBNull.Value)
                    {
                        if (!ctorArg.IsNonNullable)
                        {
                            value = null;
                        }
                        else
                            ThrowHelper.ThrowCantSetNull(ctorArg.ParameterName, sqlColumnName);
                    }

                    object? finalValue;
                    if (_activator.Contract.TryGetOrmPropertyFromLazy(convertedSqlColumnName, out OrmProperty? ormProperty))
                    {
                        finalValue = ormProperty.Convert(value, columnSqlType, sqlColumnName);
                    }
                    else
                    {
                        // конвертируем значение.
                        finalValue = SqlTypeConverter.ChangeType(value, ctorArg.ParameterType, columnSqlType, sqlColumnName);
                    }
                    propValues[ctorArg.Index] = finalValue;

                    // Запомним что этот параметр мы замапили.
                    mapped[ctorArg.Index] = true;
                }
            }

            // Не допускается иметь в DTO больше полей чем в SQL.
            foreach ((string paramName, var arg) in _activator.ConstructorArguments)
            {
                if (!mapped[arg.Index])
                {
                    throw new MicroOrmException($"В Sql не найдена колонка соответствующая свойству \"{paramName}\"");
                }
            }

            object obj = _activator.CreateInstance(propValues);

            _activator.OnDeserializedHandle?.Invoke(obj, DefaultStreamingContext);

            return obj;
        }
    }
}
