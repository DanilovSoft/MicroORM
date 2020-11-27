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

                    if (_activator.Contract.TryGetOrmPropertyFromLazy(sqlColumnName, out OrmProperty? ormProperty))
                    {
                        MapDboProperty(reader, dbo, i, sqlColumnName, ormProperty);
                    }
                    else if (_sqlOrm.UsePascalCaseNamingConvention)
                    {
                        if (_activator.Contract.TryGetOrmPropertyFromLazy(sqlColumnName.SnakeToPascalCase(), out ormProperty))
                        {
                            MapDboProperty(reader, dbo, i, sqlColumnName, ormProperty);
                        }
                    }
                }
            }

            _activator.OnDeserializedHandle?.Invoke(dbo, DefaultStreamingContext);

            return dbo;
        }

        private static void MapDboProperty(DbDataReader reader, object dbo, int index, string sqlColumnName, OrmProperty ormProperty)
        {
            Type sqlColumnType = reader.GetFieldType(index);
            object? value = reader[index];

            if (value == DBNull.Value)
            {
                if (!ormProperty.IsNonNullable)
                {
                    value = null;
                }
                else
                    ThrowHelper.ThrowCantSetNull(ormProperty.PropertyName, sqlColumnName);
            }

            ormProperty.ConvertAndSetValue(dbo, value, sqlColumnType, sqlColumnName);
        }

        private object ReadToNonEmptyCtor(DbDataReader reader)
        {
            Debug.Assert(_activator != null);
            Debug.Assert(_activator.ConstructorArguments != null);

            // Что-бы сконструировать структуру, сначала нужно подготовить параметры его конструктора.
            object?[] ctorParamValues = new object[_activator.ConstructorArguments.Count];

            // Будем запоминать индексы замапленых параметров что-бы в конце определить все ли замапились.
            Span<bool> mapped = stackalloc bool[_activator.ConstructorArguments.Count];
            
            // Односторонний маппингт: БД -> DTO, поэтому пляшем от БД.
            for (int i = 0; i < reader.FieldCount; i++)
            {
                // Имя колонки в БД.
                string sqlColumnName = reader.GetName(i);

                // Допускается иметь в SQL больше полей чем в ДТО.
                if (_activator.ConstructorArguments.TryGetValue(sqlColumnName, out ConstructorArgument? ctorArg))
                {
                    MapDboProperty(reader, i, sqlColumnName, ctorArg, ctorParamValues, mapped);
                }
                else if (_sqlOrm.UsePascalCaseNamingConvention)
                {
                    if (_activator.ConstructorArguments.TryGetValue(sqlColumnName.SnakeToPascalCase(), out ctorArg))
                    {
                        MapDboProperty(reader, i, sqlColumnName, ctorArg, ctorParamValues, mapped);
                    }
                }
            }

            // Не допускается иметь в конструкторе DBO больше параметров чем полей в SQL.
            foreach ((string paramName, var arg) in _activator.ConstructorArguments)
            {
                if (!mapped[arg.Index])
                {
                    throw new MicroOrmException($"В результате SQL запроса не найдена колонка соответствующая аргументу конструктора \"{paramName}\"");
                }
            }

            object obj = _activator.CreateInstance(ctorParamValues);

            _activator.OnDeserializedHandle?.Invoke(obj, DefaultStreamingContext);

            return obj;
        }

        private void MapDboProperty(DbDataReader reader, int index, string sqlColumnName, ConstructorArgument ctorArg, object?[] propValues, Span<bool> mapped)
        {
            object? value = reader[index];
            Type columnSqlType = reader.GetFieldType(index);

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
            if (_activator.Contract.TryGetOrmPropertyFromLazy(sqlColumnName, out OrmProperty? ormProperty))
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
}
