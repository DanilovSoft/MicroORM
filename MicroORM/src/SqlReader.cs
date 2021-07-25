using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.PortableExecutable;
using System.Threading;
using System.Threading.Tasks;
using DanilovSoft.MicroORM.Helpers;
using DanilovSoft.MicroORM.ObjectMapping;
using static DanilovSoft.MicroORM.ExceptionMessages;
using SystemArray = System.Array;


namespace DanilovSoft.MicroORM
{
    public abstract class SqlReader : ISqlReader
    {
        private readonly int _closeConnectionPenaltySec = SqlORM.CloseConnectionPenaltySec;
        private readonly SqlORM _sqlOrm;

        internal SqlReader(SqlORM sqlOrm)
        {
            Debug.Assert(sqlOrm != null);

            _sqlOrm = sqlOrm;
        }

        protected int QueryTimeoutSec { get; set; } = SqlORM.DefaultQueryTimeoutSec;
        internal abstract ICommandReader GetCommandReader();
        internal abstract ValueTask<ICommandReader> GetCommandReaderAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Если <paramref name="sqlRawValue"/> является DBNull то заменяется на Null.
        /// </summary>
        [DebuggerStepThrough]
        private static object? NullIfDBNull(object sqlRawValue)
        {
            return sqlRawValue == DBNull.Value 
                ? null 
                : sqlRawValue;
        }

        public int Execute()
        {
            return Wrapper(static reader => reader.RecordsAffected);
        }


        public DataTable Table()
        {
            return Wrapper(static r => Table(r));
        }

        private static DataTable Table(DbDataReader reader)
        {
            var table = new DataTable("Table1");
            try
            {
                table.LoadData(reader);
                return NullableHelper.SetNull(ref table);
            }
            finally
            {
                table?.Dispose();
            }
        }


        public object? Scalar()
        {
            return Wrapper(static r => Scalar(r));
        }
        
        private static object? Scalar(DbDataReader reader)
        {
            reader.Read();
            object sqlRawValue = reader.GetValue(0);
            return NullIfDBNull(sqlRawValue);
        }

        private static object? Scalar<T>(DbDataReader reader)
        {
            reader.Read();
            object sqlRawValue = reader.GetValue(0);
            return SqlTypeConverter.ConvertRawSqlToClrType(sqlRawValue, reader.GetFieldType(0), reader.GetName(0), toType: typeof(T));
        }

        public T Scalar<T>()
        {
            // Имея только T невозможно определить null-ref конвенцию поэтому разрешаем возврат null.

            return (T)Wrapper(static r => Scalar<T>(r))!;
        }

        public object?[] ScalarArray()
        {
            List<object?> list = ScalarList();

            return list.Count > 0 
                ? list.ToArray() 
                : SystemArray.Empty<object>();
        }
        
        public List<object?> ScalarList()
        {
            return Wrapper(static r => ScalarList(r));
        }
        
        public T[] ScalarArray<T>()
        {
            var list = ScalarList<T>();

            if (list.Count > 0)
            {
                return list.ToArray();
            }
            
            return SystemArray.Empty<T>();
        }

        public List<T> ScalarList<T>()
        {
            return Wrapper(static r => ScalarList<T>(r));
        }

        private static List<T> ScalarList<T>(DbDataReader reader)
        {
            var list = new List<T>();
            while (reader.Read())
            {
                object sqlRawValue = reader.GetValue(0);
                var convertedValue = SqlTypeConverter.ConvertRawSqlToClrType<T>(sqlRawValue, reader.GetFieldType(0), reader.GetName(0));
                list.Add(convertedValue);
            }
            return list;
        }

        private static List<object?> ScalarList(DbDataReader reader)
        {
            var list = new List<object?>();
            while (reader.Read())
            {
                object sqlRawValue = reader.GetValue(0);
                object? sqlValue = NullIfDBNull(sqlRawValue);
                list.Add(sqlValue);
            }
            return list;
        }

        public T? ScalarOrDefault<T>()
        {
            return (T?)Wrapper(static r => ScalarOrDefault<T>(r));
        }
        private static object? ScalarOrDefault<T>(DbDataReader reader)
        {
            if (reader.Read())
            {
                object sqlRawValue = reader.GetValue(0);
                return SqlTypeConverter.ConvertRawSqlToClrType(sqlRawValue, reader.GetFieldType(0), reader.GetName(0), toType: typeof(T));
            }
            else
            {
                return default(T);
            }
        }


        public T Single<T>()
        {
            return (T)Wrapper(static (r, s) => s.Single<T>(r), this);
        }

        public T Single<T>(T anonymousType) where T : class
        {
            return Wrapper(static (r, state) => state.AnonymousSingle<T>(r), this);
        }

        private object Single<T>(DbDataReader reader) // T - сложный тип и не может быть Null.
        {
            reader.Read();
            var toObject = new ObjectMapper<T>(reader, _sqlOrm);
            return toObject.ReadObject();
        }

        private T AnonymousSingle<T>(DbDataReader reader) where T : class
        {
            reader.Read();
            var toObject = new ObjectMapper<T>(reader, _sqlOrm);
            return toObject.ReadAsAnonymousObject<T>();
        }
        
        public T? SingleOrDefault<T>()
        {
            return (T?)Wrapper(static (r, state) => state.SingleOrDefault<T>(r), this);
        }

        public T? SingleOrDefault<T>(T anonymousType) where T : class
        {
            return Wrapper(static (r, state) => state.AnonymousSingleOrDefault<T>(r), this);
        }
        
        private object? SingleOrDefault<T>(DbDataReader reader)
        {
            if (reader.Read())
            {
                var toObject = new ObjectMapper<T>(reader, _sqlOrm);
                return toObject.ReadObject();
            }
            else
            {
                return default(T);
            }
        }

        private T? AnonymousSingleOrDefault<T>(DbDataReader reader) where T : class
        {
            if (reader.Read())
            {
                var toObject = new ObjectMapper<T>(reader, _sqlOrm);
                return toObject.ReadAsAnonymousObject<T>();
            }
            else
            {
                return null;
            }
        }

        #region IAnonymousReader

        public IAsyncAnonymousReader<T> AsAnonymousAsync<T>(T anonymousType) where T : class
        {
            return new Anonimous<T>(this);
        }

        [SuppressMessage("Usage", "CA1801:Проверьте неиспользуемые параметры", Justification = "Из параметра извлекается анонимный тип")]
        public IAnonymousReader<T> AsAnonymous<T>(T anonymousType) where T : class
        {
            return new Anonimous<T>(this);
        }

        private List<TResult> FromAnonList<TAnon, TResult>(Func<TAnon, TResult> selector) where TAnon : class
        {
            return Wrapper(static (reader, state) => state.Item1.AnonumouseList(reader, state.selector), 
                state: (this, selector));
        }

        private TResult[] FromAnonArray<TAnon, TResult>(Func<TAnon, TResult> selector) where TAnon : class
        {
            var list = FromAnonList(selector);

            if (list.Count > 0)
            {
                return list.ToArray();
            }

            return SystemArray.Empty<TResult>();
        }

        private Task<TResult[]> FromAnonArrayAsync<TAnon, TResult>(Func<TAnon, TResult> selector) where TAnon : class
        {
            return FromAnonArrayAsync(selector, CancellationToken.None);
        }

        private async Task<TResult[]> FromAnonArrayAsync<TAnon, TResult>(Func<TAnon, TResult> selector, CancellationToken cancellationToken) where TAnon : class
        {
            var list = await FromAnonListAsync(selector, cancellationToken).ConfigureAwait(false);

            if (list.Count > 0)
            {
                return list.ToArray();
            }

            return SystemArray.Empty<TResult>();
        }

        private Task<List<TResult>> FromAnonListAsync<TAnon, TResult>(Func<TAnon, TResult> selector) where TAnon : class
        {
            return FromAnonListAsync(selector, CancellationToken.None);
        }

        private Task<List<TResult>> FromAnonListAsync<TAnon, TResult>(Func<TAnon, TResult> selector, CancellationToken cancellationToken) where TAnon : class
        {
            return WrapperAsync(static (r, state, canc) => state.This.AnonumouseListAsync(r, state.selector, canc), 
                state: (This: this, selector), 
                cancellationToken);
        }

        #endregion

        public List<T> ToList<T>()
        {
            return Wrapper(List<T>);
        }
        public List<T> ToList<T>(T anonymousType) where T : class
        {
            return Wrapper(AnonumouseList<T>);
        }
        //public List<T> List<T>(Func<DbDataReader, T> selector)
        //{
        //    return Wrapper(Wrap, selector);

        //    static List<T> Wrap(DbDataReader reader, Func<DbDataReader, T> sel)
        //    {
        //        return List(reader, sel);
        //    }
        //}
        //public List<T> List<T>(Action<T, DbDataReader> selector) where T : class
        //{
        //    return Wrapper(Wrap, selector);

        //    List<T> Wrap(DbDataReader reader, Action<T, DbDataReader> sel)
        //    {
        //        return List(reader, sel);
        //    }
        //}
        private List<TAnon> AnonumouseList<TAnon>(DbDataReader reader) where TAnon : class
        {
            var list = new List<TAnon>();
            if (reader.Read())
            {
                var toObject = new ObjectMapper<TAnon>(reader, _sqlOrm);
                do
                {
                    TAnon rowObj = toObject.ReadAsAnonymousObject<TAnon>();
                    list.Add(rowObj);

                } while (reader.Read());
            }
            return list;
        }
        private List<TResult> AnonumouseList<TAnon, TResult>(DbDataReader reader, Func<TAnon, TResult> selector) where TAnon : class
        {
            var list = new List<TResult>();
            if (reader.Read())
            {
                var toObject = new ObjectMapper<TAnon>(reader, _sqlOrm);
                do
                {
                    TResult result = AnonToResult(toObject, selector);
                    list.Add(result);
                } while (reader.Read());
            }
            return list;

            static TResult AnonToResult(ObjectMapper<TAnon> toObject, Func<TAnon, TResult> selector)
            {
                TAnon row = toObject.ReadAsAnonymousObject<TAnon>();
                TResult result = selector(row);
                return result;
            }
        }
        private List<T> List<T>(DbDataReader reader)
        {
            var list = new List<T>();
            if (reader.Read())
            {
                var toObject = new ObjectMapper<T>(reader, _sqlOrm);
                do
                {
                    var result = (T)toObject.ReadObject();
                    list.Add(result);

                } while (reader.Read());
            }
            return list;
        }
        //private static List<T> List<T>(DbDataReader reader, Func<DbDataReader, T> selector)
        //{
        //    List<T> list = new List<T>();
        //    while (reader.Read())
        //    {
        //        T result = selector(reader);
        //        list.Add(result);
        //    }
        //    return list;
        //}
        //private List<T> List<T>(DbDataReader reader, Action<T, DbDataReader> selector)
        //{
        //    List<T> list = new List<T>();
        //    if (reader.Read())
        //    {
        //        var toObject = new ObjectMapper<T>(reader, _sqlORM);
        //        do
        //        {
        //            var result = (T)toObject.ReadObject();
        //            selector(result, reader);
        //            list.Add(result);

        //        } while (reader.Read());
        //    }
        //    return list;
        //}


        public T[] ToArray<T>()
        {
            List<T> list = ToList<T>();
            if (list.Count > 0)
                return list.ToArray();
            
            return SystemArray.Empty<T>();
        }
        public T[] ToArray<T>(T anonymousType) where T : class
        {
            List<T> list = ToList(anonymousType);
            if (list.Count > 0)
                return list.ToArray();
            
            return SystemArray.Empty<T>();
        }
        
        public TCollection ToCollection<TItem, TCollection>() where TCollection : ICollection<TItem>, new()
        {
            var items = ToList<TItem>();
            var col = new TCollection();
            col.AddRange(items);
            return col;
        }

        // асинхронные

        public Task<TCollection> ToCollectionAsync<TItem, TCollection>() where TCollection : ICollection<TItem>, new()
        {
            return ToCollectionAsync<TItem, TCollection>(CancellationToken.None);
        }
        public Task<TCollection> ToCollectionAsync<TItem, TCollection>(CancellationToken cancellationToken) where TCollection : ICollection<TItem>, new()
        {
            return WrapperAsync(CollectionAsync<TItem, TCollection>, cancellationToken);
        }
        //Task<TCollection> IAsyncSqlReader.Collection<TItem, TCollection>(Action<TItem, DbDataReader> selector)
        //{
        //    return AsAsync.Collection<TItem, TCollection>(selector, CancellationToken.None);
        //}
        //Task<TCollection> IAsyncSqlReader.Collection<TItem, TCollection>(Action<TItem, DbDataReader> selector, CancellationToken cancellationToken)
        //{
        //    return WrapperAsync(Wrap, selector, cancellationToken);

        //    static Task<TCollection> Wrap(DbDataReader reader, Action<TItem, DbDataReader> sel, CancellationToken token)
        //    {
        //        return CollectionAsync<TItem, TCollection>(reader, sel, token);
        //    }
        //}
        private async Task<TCollection> CollectionAsync<TItem, TCollection>(DbDataReader reader, CancellationToken cancellationToken) where TCollection : ICollection<TItem>, new()
        {
            var list = new TCollection();
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var toObject = new ObjectMapper<TItem>(reader, _sqlOrm);
                do
                {
                    TItem item = (TItem)toObject.ReadObject();
                    list.Add(item);

                } while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false));
            }
            return list;
        }
        //private static async Task<TCollection> CollectionAsync<TItem, TCollection>(DbDataReader reader, Action<TItem, DbDataReader> selector, CancellationToken cancellationToken) where TCollection : ICollection<TItem>, new()
        //{
        //    var list = new TCollection();
        //    if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        //    {
        //        var toObject = new ObjectMapper<TItem>(reader);
        //        do
        //        {
        //            var item = (TItem)toObject.ReadObject();
        //            selector(item, reader);
        //            list.Add(item);

        //        } while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false));
        //    }
        //    return list;
        //}


        public Task<DataTable> TableAsync()
        {
            return WrapperAsync(TableAsync, CancellationToken.None);
        }
        public Task<DataTable> TableAsync(CancellationToken cancellationToken)
        {
            return WrapperAsync(TableAsync, cancellationToken);
        }
        private Task<DataTable> TableAsync(DbDataReader reader, CancellationToken cancellationToken)
        {
            var table = new DataTable("Table1");
            
            Task task = table.LoadAsync(reader, cancellationToken);

            if (task.IsCompletedSuccessfully())
            {
                return Task.FromResult(table);
            }
            else
            {
                return WaitAsync(task, table);

                static async Task<DataTable> WaitAsync(Task task, DataTable table)
                {
                    var tableCopy = table;
                    try
                    {
                        await task.ConfigureAwait(false);
                        return NullableHelper.SetNull(ref tableCopy);
                    }
                    finally
                    {
                        tableCopy?.Dispose();
                    }
                }
            }
        }


        public Task<object?> ScalarAsync()
        {
            return WrapperAsync(ScalarAsync, CancellationToken.None);
        }
        public Task<object?> ScalarAsync(CancellationToken cancellationToken)
        {
            return WrapperAsync(ScalarAsync, cancellationToken);
        }
        private Task<object?> ScalarAsync(DbDataReader reader, CancellationToken cancellationToken)
        {
            Task<bool> task = reader.ReadAsync(cancellationToken);
            if (task.IsCompletedSuccessfully())
            {
                object? value = Read(reader);
                return Task.FromResult(value);
            }
            else
            {
                return WaitAsync(task, reader);
                static async Task<object?> WaitAsync(Task<bool> task, DbDataReader reader)
                {
                    await task.ConfigureAwait(false);
                    return Read(reader);
                }
            }

            static object? Read(DbDataReader reader)
            {
                object sqlRawValue = reader.GetValue(0);
                return NullIfDBNull(sqlRawValue);
            }
        }
        private static async Task<T> ScalarAsync<T>(DbDataReader reader, CancellationToken cancellationToken)
        {
            await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            object sqlRawValue = reader.GetValue(0);
            return (T)SqlTypeConverter.ConvertRawSqlToClrType(sqlRawValue, reader.GetFieldType(0), reader.GetName(0), typeof(T))!;
        }
        public Task<T> ScalarAsync<T>()
        {
            return WrapperAsync(ScalarAsync<T>, CancellationToken.None);
        }
        public Task<T> ScalarAsync<T>(CancellationToken cancellationToken)
        {
            return WrapperAsync(ScalarAsync<T>, cancellationToken);
        }

        public async Task<object?[]> ScalarArrayAsync()
        {
            List<object?> list = await ScalarListAsync().ConfigureAwait(false);

            return list.Count > 0 
                ? list.ToArray() 
                : SystemArray.Empty<object>();
        }
        public Task<List<object?>> ScalarListAsync()
        {
            return ScalarListAsync(CancellationToken.None);
        }
        public async Task<T[]> ScalarArrayAsync<T>()
        {
            var list = await ScalarListAsync<T>().ConfigureAwait(false);

            if (list.Count > 0)
                return list.ToArray();

            return SystemArray.Empty<T>();
        }
        public Task<List<T>> ScalarListAsync<T>()
        {
            return ScalarListAsync<T>(CancellationToken.None);
        }
        public async Task<object?[]> ScalarArrayAsync(CancellationToken cancellationToken)
        {
            var list = await ScalarListAsync(cancellationToken).ConfigureAwait(false);

            if (list.Count > 0)
                return list.ToArray();

            return SystemArray.Empty<object>();
        }

        public async Task<T[]> ScalarArrayAsync<T>(CancellationToken cancellationToken)
        {
            var list = await ScalarListAsync<T>(cancellationToken).ConfigureAwait(false);

            return list.Count > 0 
                ? list.ToArray() 
                : SystemArray.Empty<T>();
        }
        
        public Task<List<object?>> ScalarListAsync(CancellationToken cancellationToken)
        {
            return WrapperAsync(ScalarListAsync, cancellationToken);
        }
        
        public Task<List<T>> ScalarListAsync<T>(CancellationToken cancellationToken)
        {
            return WrapperAsync(ScalarListAsync<T>, cancellationToken);
        }

        private static async Task<List<T>> ScalarListAsync<T>(DbDataReader reader, CancellationToken cancellationToken)
        {
            var list = new List<T>();
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                object sqlRawValue = reader.GetValue(0);
                var convertedValue = SqlTypeConverter.ConvertRawSqlToClrType<T>(sqlRawValue, sqlColumnType: reader.GetFieldType(0), sqlColumnName: reader.GetName(0));
                list.Add(convertedValue);
            }
            return list;
        }

        private async Task<List<object?>> ScalarListAsync(DbDataReader reader, CancellationToken cancellationToken)
        {
            var list = new List<object?>();
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                object sqlRawValue = reader.GetValue(0);
                object? sqlValue = NullIfDBNull(sqlRawValue);
                list.Add(sqlValue);
            }
            return list;
        }


        public Task<T?> ScalarOrDefaultAsync<T>()
        {
            return WrapperAsync(ScalarOrDefaultAsyncInternal<T>, CancellationToken.None);
        }
        public Task<T?> ScalarOrDefaultAsync<T>(CancellationToken cancellationToken)
        {
            return WrapperAsync(ScalarOrDefaultAsyncInternal<T>, cancellationToken);
        }

        private static async Task<T?> ScalarOrDefaultAsyncInternal<T>(DbDataReader reader, CancellationToken cancellationToken)
        {
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                object sqlRawValue = reader.GetValue(0);
                return SqlTypeConverter.ConvertRawSqlToClrType<T>(sqlRawValue, reader.GetFieldType(0), reader.GetName(0))!;
            }
            else
            {
                return default;
            }
        }


        public Task<List<T>> ToListAsync<T>()
        {
            return WrapperAsync(ListAsync<T>, CancellationToken.None);
        }
        public Task<List<T>> ToListAsync<T>(CancellationToken cancellationToken)
        {
            return WrapperAsync(ListAsync<T>, cancellationToken);
        }
        public Task<List<T>> ToListAsync<T>(T anonymousType) where T : class
        {
            return WrapperAsync(AnonymousListAsync<T>, CancellationToken.None);
        }
        public Task<List<T>> ToListAsync<T>(T anonymousType, CancellationToken cancellationToken) where T : class
        {
            return WrapperAsync(AnonymousListAsync<T>, cancellationToken);
        }
        private async Task<List<T>> AnonymousListAsync<T>(DbDataReader reader, CancellationToken cancellationToken) where T : class
        {
            var list = new List<T>();
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var toObject = new ObjectMapper<T>(reader, _sqlOrm);
                do
                {
                    T result = toObject.ReadAsAnonymousObject<T>();
                    list.Add(result);

                } while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false));
            }
            return list;
        }
        private async Task<List<TResult>> AnonumouseListAsync<TAnon, TResult>(DbDataReader reader, Func<TAnon, TResult> selector, 
            CancellationToken cancellationToken) where TAnon : class
        {
            var list = new List<TResult>();
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var toObject = new ObjectMapper<TAnon>(reader, _sqlOrm);
                do
                {
                    TResult result = AnonToResult(toObject, selector);
                    list.Add(result);

                } while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false));
            }
            return list;

            static TResult AnonToResult(ObjectMapper<TAnon> toObject, Func<TAnon, TResult> selector)
            {
                TAnon anonObj = toObject.ReadAsAnonymousObject<TAnon>();
                TResult result = selector(anonObj);
                return result;
            }
        }
        private async Task<List<T>> ListAsync<T>(DbDataReader reader, CancellationToken cancellationToken)
        {
            var list = new List<T>();
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var toObject = new ObjectMapper<T>(reader, _sqlOrm);
                do
                {
                    var result = (T)toObject.ReadObject();
                    list.Add(result);

                } while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false));
            }
            return list;
        }
        
        public Task<T[]> ToArrayAsync<T>()
        {
            return ToArrayAsync<T>(CancellationToken.None);
        }
        
        public Task<T[]> ToArrayAsync<T>(T anonymousType) where T : class
        {
            return ToArrayAsync(anonymousType, CancellationToken.None);
        }
        
        public async Task<T[]> ToArrayAsync<T>(CancellationToken cancellationToken)
        {
            List<T> list = await ToListAsync<T>(cancellationToken).ConfigureAwait(false);

            return list.Count > 0 
                ? list.ToArray() 
                : SystemArray.Empty<T>();
        }
        
        public Task<T[]> ToArrayAsync<T>(T anonymousType, CancellationToken cancellationToken) where T : class
        {
            Task<List<T>> task = ToListAsync(anonymousType, cancellationToken);
            if (task.IsCompletedSuccessfully)
            {
                var list = task.Result;
                var array = ToArray(list);
                return Task.FromResult(array);
            }
            else
            {
                return WaitAsync(task);
                static async Task<T[]> WaitAsync(Task<List<T>> task)
                {
                    List<T> list = await task.ConfigureAwait(false);
                    return ToArray(list);
                }
            }

            static T[] ToArray(List<T> list)
            {
                return list.Count > 0
                    ? list.ToArray()
                    : SystemArray.Empty<T>();
            }
        }
       
        public Task<T> SingleAsync<T>()
        {
            return SingleAsync<T>(CancellationToken.None);
        }
        public Task<T> SingleAsync<T>(T anonymousType) where T : class
        {
            return SingleAsync(anonymousType, CancellationToken.None);
        }
        
        public Task<T> SingleAsync<T>(CancellationToken cancellationToken)
        {
            return WrapperAsync(SingleAsync<T>, cancellationToken);
        }
        
        public Task<T> SingleAsync<T>(T anonymousType, CancellationToken cancellationToken) where T : class
        {
            return WrapperAsync(AnonymousSingleAsync<T>, cancellationToken);
        }
        
        private Task<T> SingleAsync<T>(DbDataReader reader, CancellationToken cancellationToken)
        {
            Task<bool> task = reader.ReadAsync(cancellationToken);
            if (task.IsCompletedSuccessfully())
            {
                var objectMapper = new ObjectMapper<T>(reader, _sqlOrm);
                T value = Map(reader);
                return Task.FromResult(value);
            }
            else
            {
                return WaitAsync(task, reader);

                async Task<T> WaitAsync(Task<bool> task, DbDataReader reader)
                {
                    await task.ConfigureAwait(false);
                    return Map(reader);
                }
            }

            T Map(DbDataReader reader)
            {
                var objectMapper = new ObjectMapper<T>(reader, _sqlOrm);
                return (T)objectMapper.ReadObject();
            }
        }
        private Task<T> AnonymousSingleAsync<T>(DbDataReader reader, CancellationToken cancellationToken) where T : class
        {
            Task<bool> task = reader.ReadAsync(cancellationToken);

            if (task.IsCompletedSuccessfully())
            {
                return Task.FromResult(Map(reader));
            }
            else
            {
                return WaitAsync(task, reader);

                async Task<T> WaitAsync(Task<bool> task, DbDataReader reader)
                {
                    await task.ConfigureAwait(false);
                    return Map(reader);
                }
            }

            T Map(DbDataReader reader)
            {
                var toObject = new ObjectMapper<T>(reader, _sqlOrm);
                return toObject.ReadAsAnonymousObject<T>();
            }
        }
        //private async Task<T> SingleAsync<T>(DbDataReader reader, Action<T, DbDataReader> selector, CancellationToken cancellationToken) where T : class
        //{
        //    await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        //    var toObject = new ObjectMapper<T>(reader, _sqlORM);
        //    var item = (T)toObject.ReadObject();
        //    selector(item, reader);
        //    return item;
        //}
        //private static async Task<T> SingleAsync<T>(DbDataReader reader, Func<DbDataReader, T> selector, CancellationToken cancellationToken)
        //{
        //    await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        //    T result = selector(reader);
        //    return result;
        //}


        public Task<T?> SingleOrDefaultAsync<T>()
        {
            return SingleOrDefaultAsync<T>(CancellationToken.None);
        }
        public Task<T?> SingleOrDefaultAsync<T>(T anonymousType) where T : class
        {
            return SingleOrDefaultAsync(anonymousType, CancellationToken.None);
        }
        public Task<T?> SingleOrDefaultAsync<T>(CancellationToken cancellationToken)
        {
            return WrapperAsync(SingleOrDefaultAsync<T>, cancellationToken);
        }
        public Task<T?> SingleOrDefaultAsync<T>(T anonymousType, CancellationToken cancellationToken) where T : class
        {
            return WrapperAsync(AnonymousSingleOrDefaultAsync<T>, cancellationToken)!;
        }
        private async Task<T?> SingleOrDefaultAsync<T>(DbDataReader reader, CancellationToken cancellationToken)
        {
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var objectMapper = new ObjectMapper<T>(reader, _sqlOrm);
                return (T)objectMapper.ReadObject();
            }
            else
            {
                return default;
            }
        }

        private Task<T?> AnonymousSingleOrDefaultAsync<T>(DbDataReader reader, CancellationToken cancellationToken) where T : class
        {
            Task<bool> task = reader.ReadAsync(cancellationToken);
            if (task.IsCompletedSuccessfully())
            {
                bool hasRows = task.Result;
                var value = MapAnonymousObject(hasRows, reader);
                return Task.FromResult(value);
            }
            else
            {
                return WaitAsync(task, reader);

                async Task<T?> WaitAsync(Task<bool> task, DbDataReader reader)
                {
                    bool hasRows = await task.ConfigureAwait(false);
                    return MapAnonymousObject(hasRows, reader);
                }
            }

            T? MapAnonymousObject(bool hasRows, DbDataReader reader)
            {
                if (hasRows)
                {
                    var toObject = new ObjectMapper<T>(reader, _sqlOrm);
                    return toObject.ReadAsAnonymousObject<T>();
                }
                else
                {
                    return null;
                }
            }
        }

        //private async Task<T> SingleOrDefaultAsync<T>(DbDataReader reader, Action<T, DbDataReader> selector, CancellationToken cancellationToken)
        //{
        //    T item = await SingleOrDefaultAsync<T>(reader, cancellationToken).ConfigureAwait(false);
        //    selector(item, reader);
        //    return item;
        //}


        public Task<int> ExecuteAsync()
        {
            return WrapperAsync(ExecuteAsync, CancellationToken.None);
        }
        public Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            return WrapperAsync(ExecuteAsync, cancellationToken);
        }
        private Task<int> ExecuteAsync(DbDataReader reader, CancellationToken cancellationToken)
        {
            return Task.FromResult(reader.RecordsAffected);
        }

        private Task<T> WrapperAsync<T>(Func<DbDataReader, CancellationToken, Task<T>> selector, CancellationToken cancellationToken)
        {
            return WrapperAsync(static (r, selector, canc) => selector(r, canc), selector, cancellationToken);
        }

        private async Task<T> WrapperAsync<T, TArg>(Func<DbDataReader, TArg, CancellationToken, Task<T>> selector, TArg state, CancellationToken cancellationToken)
        {
            // Смешиваем пользовательский токен и аварийный таймаут.
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                // Установить таймаут.
                linked.CancelAfter(millisecondsDelay: QueryTimeoutSec * 1000);

                // The CommandTimeout property will be ignored during asynchronous method calls such as BeginExecuteReader.
                ICommandReader comReader = await GetCommandReaderAsync(linked.Token).ConfigureAwait(false);
                
                // Аварийный контроль соединения. При не явном дисконнекте выполняет закрытие с дополнительной форой после QueryTimeoutSec.
                var closeConnection = new CloseConnection(_closeConnectionPenaltySec, comReader.Connection, linked.Token);
                try
                {
                    // Отправляет серверу запрос на отмену выполняющегося запроса по таймауту или по запросу пользователя.
                    var cancelCommandRequest = new CancelCommandRequest(comReader.Command, linked.Token);
                    try
                    {
                        // Инициализация запроса и ожидание готовности данных.
                        DbDataReader reader = await comReader.GetReaderAsync(linked.Token).ConfigureAwait(false);
                        
                        Task<T> selectorTask = selector(reader, state, linked.Token);

                        // Получение данных сервера.
                        return await selectorTask.ConfigureAwait(false);
                    }
                    finally
                    {
                        cancelCommandRequest.Dispose();
                        closeConnection.Dispose();
                    }
                }
                catch (Exception ex) when (closeConnection.AbnormallyClosed)
                // Этот случай приоритетнее токенов отмены — проверяется в первую очередь.
                {
                    // Бросить вверх — соединение закрыто аварийно из-за превышенного времени на выполнение запроса + фора на грациозную отмену запроса.
                    throw new ConnectionClosedAbnormallyException(ex, QueryTimeoutSec, _closeConnectionPenaltySec);
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested && linked.IsCancellationRequested)
                // Ошибка может быть любого типа из-за токена таймаута. Токен таймаута приоритетнее пользовательского токена.
                {
                    // Бросить вверх таймаут исключение и вложить порожденное исключение даже если это просто OperationCanceledException токена 'linked'.
                    throw new SqlQueryTimeoutException(ex, QueryTimeoutSec);
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
                // Сработал пользовательский токен отмены и нет поражденного исключения.
                {
                    // Бросить вверх без вложенного исключения — в нём ничего нет.
                    throw new OperationCanceledException(UserCancelMessage, cancellationToken);
                }
                catch (Exception ex) when (cancellationToken.IsCancellationRequested)
                {
                    // Бросить вверх и вложить исключение с подробностями.
                    throw new OperationCanceledException(UserCancelMessage, ex, cancellationToken);
                }
                finally
                {
                    comReader.Dispose();
                }
            }
        }
        
        private T Wrapper<T>(Func<DbDataReader, T> selector)
        {
            return Wrapper(static (reader, s) => s(reader), selector);
        }

        private T Wrapper<T, TArg>(Func<DbDataReader, TArg, T> selector, TArg state)
        {
            using (var commandReader = GetCommandReader())
            {
                var reader = commandReader.GetReader();
                return selector(reader, state);
            }
        }

        [DebuggerStepThrough]
        private readonly struct Anonimous<TAnon> : IAnonymousReader<TAnon>, IAsyncAnonymousReader<TAnon> where TAnon : class
        {
            private readonly SqlReader _self;

            public Anonimous(SqlReader self)
            {
                _self = self;
            }

            public TResult[] Array<TResult>(Func<TAnon, TResult> selector) 
                => _self.FromAnonArray(selector);

            public List<TResult> List<TResult>(Func<TAnon, TResult> selector) 
                => _self.FromAnonList(selector);

            Task<TResult[]> IAsyncAnonymousReader<TAnon>.Array<TResult>(Func<TAnon, TResult> selector)
                => _self.FromAnonArrayAsync(selector);

            Task<TResult[]> IAsyncAnonymousReader<TAnon>.Array<TResult>(Func<TAnon, TResult> selector, CancellationToken cancellationToken)
                => _self.FromAnonArrayAsync(selector, cancellationToken);

            Task<List<TResult>> IAsyncAnonymousReader<TAnon>.List<TResult>(Func<TAnon, TResult> selector)
                => _self.FromAnonListAsync(selector);

            Task<List<TResult>> IAsyncAnonymousReader<TAnon>.List<TResult>(Func<TAnon, TResult> selector, CancellationToken cancellationToken) 
                => _self.FromAnonListAsync(selector, cancellationToken);
        }
    }
}
