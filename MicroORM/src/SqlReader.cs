using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DanilovSoft.MicroORM.ObjectMapping;
using static DanilovSoft.MicroORM.ExceptionMessages;
#if NET45
using ArrayClass = DanilovSoft.System.Array;
#else
using SystemArray = System.Array;
#endif


namespace DanilovSoft.MicroORM
{
    public abstract class SqlReader : ISqlReader, IAsyncSqlReader
    {
        internal abstract ICommandReader GetCommandReader();
        internal abstract ValueTask<ICommandReader> GetCommandReaderAsync(CancellationToken cancellationToken);
        protected int QueryTimeoutSec { get; set; } = SqlORM.DefaultQueryTimeoutSec;
        private readonly int _closeConnectionPenaltySec = SqlORM.CloseConnectionPenaltySec;
        private IAsyncSqlReader AsAsync => this;

        internal SqlReader()
        {

        }

        [DebuggerStepThrough]
        public IAsyncSqlReader ToAsync() => this;

        [DebuggerStepThrough]
        private static void NullIfDBNull(ref object? value)
        {
            if (value == DBNull.Value)
            {
                value = null;
            }
        }

        public int Execute()
        {
            return Wrapper(reader => reader.RecordsAffected);
        }


        public DataTable Table()
        {
            return Wrapper(Table);
        }
        private DataTable Table(DbDataReader reader)
        {
            var table = new DataTable("Table1");
            DataTable? toDispose = table;
            try
            {
                table.LoadData(reader);
                toDispose = null;
                return table;
            }
            finally
            {
                toDispose?.Dispose();
            }
        }


        public object? Scalar()
        {
            return Wrapper(Scalar);
        }
        private object? Scalar(DbDataReader reader)
        {
            reader.Read();
            object? value = reader.GetValue(0);
            NullIfDBNull(ref value);
            return value;
        }
        private object? Scalar<T>(DbDataReader reader)
        {
            reader.Read();
            object? value = reader.GetValue(0);
            NullIfDBNull(ref value);
            return SqlTypeConverter.ChangeType(value, typeof(T), reader.GetFieldType(0), reader.GetName(0));
        }
        public T Scalar<T>()
        {
            return (T)Wrapper(Scalar<T>);
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
            return Wrapper(ScalarList);
        }
        public T[] ScalarArray<T>()
        {
            List<T> list = ScalarList<T>();
            if (list.Count > 0)
                return list.ToArray();
            
            return SystemArray.Empty<T>();
        }
        public List<T> ScalarList<T>()
        {
            return Wrapper(ScalarList<T>);
        }
        private List<T> ScalarList<T>(DbDataReader reader)
        {
            var list = new List<T>();
            while (reader.Read())
            {
                object value = reader.GetValue(0);
                NullIfDBNull(ref value);
                T convertedValue = SqlTypeConverter.ChangeType<T>(value: value, columnType: reader.GetFieldType(0), columnName: reader.GetName(0));
                list.Add(convertedValue);
            }
            return list;
        }
        private List<object> ScalarList(DbDataReader reader)
        {
            var list = new List<object>();
            while (reader.Read())
            {
                object value = reader.GetValue(0);
                NullIfDBNull(ref value);
                list.Add(value);
            }
            return list;
        }


        public T ScalarOrDefault<T>()
        {
            return (T)Wrapper(ScalarOrDefault<T>);
        }
        private object ScalarOrDefault<T>(DbDataReader reader)
        {
            if (reader.Read())
            {
                object value = reader.GetValue(0);
                NullIfDBNull(ref value);
                Type columnType = reader.GetFieldType(0);
                string columnName = reader.GetName(0);
                value = SqlTypeConverter.ChangeType(value, typeof(T), columnType, columnName);
                return value;
            }
            else
            {
                return default(T);
            }
        }


        public T Single<T>()
        {
            return (T)Wrapper(Single<T>);
        }
        public T Single<T>(T @object) where T : class
        {
            return Wrapper(AnonymousSingle<T>);
        }
        public T Single<T>(Action<T, DbDataReader> selector) where T : class
        {
            return Wrapper(Wrap, selector) as T;

            object Wrap(DbDataReader reader, Action<T, DbDataReader> sel)
            {
                return Single(reader, sel);
            }
        }
        public T Single<T>(Func<DbDataReader, T> selector)
        {
            return Wrapper(Wrap, selector);

            T Wrap(DbDataReader reader, Func<DbDataReader, T> sel)
            {
                return Single(reader, sel);
            }
        }
        private object Single<T>(DbDataReader reader)
        {
            reader.Read();
            var toObject = new ObjectMapper<T>(reader);
            return toObject.ReadObject();
        }
        private T AnonymousSingle<T>(DbDataReader reader) where T : class
        {
            reader.Read();
            var toObject = new AnonymousObjectMapper<T>(reader);
            return toObject.ReadObject();
        }
        private T Single<T>(DbDataReader reader, Func<DbDataReader, T> selector)
        {
            reader.Read();
            T result = selector(reader);
            return result;
        }
        private object Single<T>(DbDataReader reader, Action<T, DbDataReader> selector) where T : class
        {
            T item = Single<T>(reader) as T;
            Debug.Assert(item != null);
            selector(item, reader);
            return item;
        }


        public T SingleOrDefault<T>()
        {
            return (T)Wrapper(SingleOrDefault<T>);
        }
        public T SingleOrDefault<T>(T @object) where T : class
        {
            return Wrapper(AnonymousSingleOrDefault<T>);
        }
        public T SingleOrDefault<T>(Action<T, DbDataReader> selector) where T : class
        {
            return Wrapper(Wrap, selector) as T;

            object Wrap(DbDataReader reader, Action<T, DbDataReader> sel)
            {
                return SingleOrDefault(reader, sel);
            }
        }
        public T SingleOrDefault<T>(Func<DbDataReader, T> selector)
        {
            return Wrapper(Wrap, selector);

            T Wrap(DbDataReader reader, Func<DbDataReader, T> sel)
            {
                return SingleOrDefault(reader, sel);
            }
        }
        private object? SingleOrDefault<T>(DbDataReader reader)
        {
            if (reader.Read())
            {
                var toObject = new ObjectMapper<T>(reader);
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
                var toObject = new AnonymousObjectMapper<T>(reader);
                return toObject.ReadObject();
            }
            else
            {
                return default;
            }
        }
        private T SingleOrDefault<T>(DbDataReader reader, Func<DbDataReader, T> selector)
        {
            if (reader.Read())
            {
                T result = selector(reader);
                return result;
            }
            else
            {
                return default;
            }
        }
        private object SingleOrDefault<T>(DbDataReader reader, Action<T, DbDataReader> selector)
        {
            var item = (T)SingleOrDefault<T>(reader);
            selector(item, reader);
            return item;
        }


        #region IAnonymousReader

        IAsyncAnonymousReader<T> IAsyncSqlReader.AsAnonymous<T>(T anonymousType)
        {
            return new Anonimous<T>(this);
        }

        public IAnonymousReader<T> AsAnonymous<T>(T anonymousType) where T : class
        {
            return new Anonimous<T>(this);
        }

        private List<TResult> FromAnonList<TAnon, TResult>(Func<TAnon, TResult> selector) where TAnon : class
        {
            return Wrapper(Wrap, selector);

            List<TResult> Wrap(DbDataReader reader, Func<TAnon, TResult> sel)
            {
                return AnonumouseList(reader, sel);
            }
        }

        private TResult[] FromAnonArray<TAnon, TResult>(Func<TAnon, TResult> selector) where TAnon : class
        {
            var list = FromAnonList(selector);
            if (list.Count > 0)
                return list.ToArray();

            return System.Array.Empty<TResult>();
        }

        private Task<TResult[]> FromAnonArrayAsync<TAnon, TResult>(Func<TAnon, TResult> selector) where TAnon : class
        {
            return FromAnonArrayAsync(selector, CancellationToken.None);
        }

        private async Task<TResult[]> FromAnonArrayAsync<TAnon, TResult>(Func<TAnon, TResult> selector, CancellationToken cancellationToken) where TAnon : class
        {
            var list = await FromAnonListAsync(selector, cancellationToken).ConfigureAwait(false);
            if (list.Count > 0)
                return list.ToArray();

            return SystemArray.Empty<TResult>();
        }

        private Task<List<TResult>> FromAnonListAsync<TAnon, TResult>(Func<TAnon, TResult> selector) where TAnon : class
        {
            return FromAnonListAsync(selector, CancellationToken.None);
        }

        private Task<List<TResult>> FromAnonListAsync<TAnon, TResult>(Func<TAnon, TResult> selector, CancellationToken cancellationToken) where TAnon : class
        {
            return WrapperAsync(Wrap, selector, cancellationToken);

            Task<List<TResult>> Wrap(DbDataReader reader, Func<TAnon, TResult> sel, CancellationToken token)
            {
                return AnonumouseListAsync(reader, sel, token);
            }
        }

        #endregion

        public List<T> List<T>()
        {
            return Wrapper(List<T>);
        }
        public List<T> List<T>(T anonymousType) where T : class
        {
            return Wrapper(AnonumouseList<T>);
        }
        public List<T> List<T>(Func<DbDataReader, T> selector)
        {
            return Wrapper(Wrap, selector);

            List<T> Wrap(DbDataReader reader, Func<DbDataReader, T> sel)
            {
                return List(reader, sel);
            }
        }
        public List<T> List<T>(Action<T, DbDataReader> selector) where T : class
        {
            return Wrapper(Wrap, selector);

            List<T> Wrap(DbDataReader reader, Action<T, DbDataReader> sel)
            {
                return List(reader, sel);
            }
        }
        private List<TAnon> AnonumouseList<TAnon>(DbDataReader reader) where TAnon : class
        {
            var list = new List<TAnon>();
            if (reader.Read())
            {
                var toObject = new AnonymousObjectMapper<TAnon>(reader);
                do
                {
                    TAnon rowObj = toObject.ReadObject();
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
                var toObject = new AnonymousObjectMapper<TAnon>(reader);
                do
                {
                    TResult result = AnonToResult(toObject, selector);
                    list.Add(result);
                } while (reader.Read());
            }
            return list;

            static TResult AnonToResult(AnonymousObjectMapper<TAnon> toObject, Func<TAnon, TResult> selector)
            {
                TAnon row = toObject.ReadObject();
                TResult result = selector(row);
                return result;
            }
        }
        private List<T> List<T>(DbDataReader reader)
        {
            var list = new List<T>();
            if (reader.Read())
            {
                var toObject = new ObjectMapper<T>(reader);
                do
                {
                    var result = (T)toObject.ReadObject();
                    list.Add(result);

                } while (reader.Read());
            }
            return list;
        }
        private List<T> List<T>(DbDataReader reader, Func<DbDataReader, T> selector)
        {
            List<T> list = new List<T>();
            while (reader.Read())
            {
                T result = selector(reader);
                list.Add(result);
            }
            return list;
        }
        private List<T> List<T>(DbDataReader reader, Action<T, DbDataReader> selector)
        {
            List<T> list = new List<T>();
            if (reader.Read())
            {
                var toObject = new ObjectMapper<T>(reader);
                do
                {
                    var result = (T)toObject.ReadObject();
                    selector(result, reader);
                    list.Add(result);

                } while (reader.Read());
            }
            return list;
        }


        public T[] Array<T>()
        {
            List<T> list = List<T>();
            if (list.Count > 0)
                return list.ToArray();
            
            return SystemArray.Empty<T>();
        }
        public T[] Array<T>(T anonymousType) where T : class
        {
            List<T> list = List(anonymousType);
            if (list.Count > 0)
                return list.ToArray();
            
            return SystemArray.Empty<T>();
        }
        public T[] Array<T>(Func<DbDataReader, T> selector)
        {
            List<T> list = List(selector);
            if (list.Count > 0)
                return list.ToArray();
            
            return SystemArray.Empty<T>();
        }
        public T[] Array<T>(Action<T, DbDataReader> selector) where T : class
        {
            List<T> list = List(selector);
            if (list.Count > 0)
                return list.ToArray();
            
            return SystemArray.Empty<T>();
        }


        public TCollection Collection<TItem, TCollection>() where TCollection : ICollection<TItem>, new()
        {
            var items = List<TItem>();
            var col = new TCollection();
            col.AddRange(items);
            return col;
        }
        public TCollection Collection<TItem, TCollection>(Action<TItem, DbDataReader> selector) where TCollection : ICollection<TItem>, new() where TItem : class
        {
            List<TItem> items = List(selector);
            var col = new TCollection();
            col.AddRange(items);
            return col;
        }


        // асинхронные

        Task<TCollection> IAsyncSqlReader.Collection<TItem, TCollection>()
        {
            return AsAsync.Collection<TItem, TCollection>(CancellationToken.None);
        }
        Task<TCollection> IAsyncSqlReader.Collection<TItem, TCollection>(CancellationToken cancellationToken)
        {
            return WrapperAsync(CollectionAsync<TItem, TCollection>, cancellationToken);
        }
        Task<TCollection> IAsyncSqlReader.Collection<TItem, TCollection>(Action<TItem, DbDataReader> selector)
        {
            return AsAsync.Collection<TItem, TCollection>(selector, CancellationToken.None);
        }
        Task<TCollection> IAsyncSqlReader.Collection<TItem, TCollection>(Action<TItem, DbDataReader> selector, CancellationToken cancellationToken)
        {
            return WrapperAsync(Wrap, selector, cancellationToken);

            Task<TCollection> Wrap(DbDataReader reader, Action<TItem, DbDataReader> sel, CancellationToken token)
            {
                return CollectionAsync<TItem, TCollection>(reader, sel, token);
            }
        }
        private async Task<TCollection> CollectionAsync<TItem, TCollection>(DbDataReader reader, CancellationToken cancellationToken) where TCollection : ICollection<TItem>, new()
        {
            var list = new TCollection();
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var toObject = new ObjectMapper<TItem>(reader);
                do
                {
                    TItem item = (TItem)toObject.ReadObject();
                    list.Add(item);

                } while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false));
            }
            return list;
        }
        private async Task<TCollection> CollectionAsync<TItem, TCollection>(DbDataReader reader, Action<TItem, DbDataReader> selector, CancellationToken cancellationToken) where TCollection : ICollection<TItem>, new()
        {
            var list = new TCollection();
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var toObject = new ObjectMapper<TItem>(reader);
                do
                {
                    var item = (TItem)toObject.ReadObject();
                    selector(item, reader);
                    list.Add(item);

                } while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false));
            }
            return list;
        }


        Task<DataTable> IAsyncSqlReader.Table()
        {
            return WrapperAsync(TableAsync, CancellationToken.None);
        }
        Task<DataTable> IAsyncSqlReader.Table(CancellationToken cancellationToken)
        {
            return WrapperAsync(TableAsync, cancellationToken);
        }
        private Task<DataTable> TableAsync(DbDataReader reader, CancellationToken cancellationToken)
        {
            var table = new DataTable("Table1");
            DataTable? toDispose = table;
            try
            {
                Task task = table.LoadAsync(reader, cancellationToken);
                toDispose = null;
                if (task.IsCompletedSuccessfully())
                {
                    return Task.FromResult(table);
                }
                else
                {
                    return WaitAsync(task, table);

                    static async Task<DataTable> WaitAsync(Task task, DataTable table)
                    {
                        DataTable? toDispose = table;
                        try
                        {
                            await task.ConfigureAwait(false);
                            toDispose = null;
                            return table;
                        }
                        finally
                        {
                            toDispose?.Dispose();
                        }
                    }
                }
            }
            finally
            {
                toDispose?.Dispose();
            }
        } 


        Task<object?> IAsyncSqlReader.Scalar()
        {
            return WrapperAsync(ScalarAsync, CancellationToken.None);
        }
        Task<object?> IAsyncSqlReader.Scalar(CancellationToken cancellationToken)
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
                object? value = reader.GetValue(0);
                NullIfDBNull(ref value);
                return value;
            }
        }
        private async Task<T> ScalarAsync<T>(DbDataReader reader, CancellationToken cancellationToken)
        {
            await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            object value = reader.GetValue(0);
            NullIfDBNull(ref value);
            Type columnType = reader.GetFieldType(0);
            string columnName = reader.GetName(0);
            value = SqlTypeConverter.ChangeType(value, typeof(T), columnType, columnName);
            return (T)value;
        }
        Task<T> IAsyncSqlReader.Scalar<T>()
        {
            return WrapperAsync(ScalarAsync<T>, CancellationToken.None);
        }
        Task<T> IAsyncSqlReader.Scalar<T>(CancellationToken cancellationToken)
        {
            return WrapperAsync(ScalarAsync<T>, cancellationToken);
        }

        async Task<object?[]> IAsyncSqlReader.ScalarArray()
        {
            List<object?> list = await AsAsync.ScalarList().ConfigureAwait(false);

            return list.Count > 0 
                ? list.ToArray() 
                : SystemArray.Empty<object>();
        }
        Task<List<object?>> IAsyncSqlReader.ScalarList()
        {
            return AsAsync.ScalarList(CancellationToken.None);
        }
        async Task<T[]> IAsyncSqlReader.ScalarArray<T>()
        {
            var list = await AsAsync.ScalarList<T>().ConfigureAwait(false);

            if (list.Count > 0)
                return list.ToArray();

            return SystemArray.Empty<T>();
        }
        Task<List<T>> IAsyncSqlReader.ScalarList<T>()
        {
            return AsAsync.ScalarList<T>(CancellationToken.None);
        }
        async Task<object?[]> IAsyncSqlReader.ScalarArray(CancellationToken cancellationToken)
        {
            var list = await AsAsync.ScalarList(cancellationToken).ConfigureAwait(false);

            if (list.Count > 0)
                return list.ToArray();

            return SystemArray.Empty<object>();
        }

        async Task<T[]> IAsyncSqlReader.ScalarArray<T>(CancellationToken cancellationToken)
        {
            var list = await AsAsync.ScalarList<T>(cancellationToken).ConfigureAwait(false);

            return list.Count > 0 
                ? list.ToArray() 
                : SystemArray.Empty<T>();
        }
        Task<List<object?>> IAsyncSqlReader.ScalarList(CancellationToken cancellationToken)
        {
            return WrapperAsync(ScalarListAsync, cancellationToken);
        }
        Task<List<T>> IAsyncSqlReader.ScalarList<T>(CancellationToken cancellationToken)
        {
            return WrapperAsync(ScalarListAsync<T>, cancellationToken);
        }
        private async Task<List<T>> ScalarListAsync<T>(DbDataReader reader, CancellationToken cancellationToken)
        {
            var list = new List<T>();
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                object value = reader.GetValue(0);
                NullIfDBNull(ref value);
                T convertedValue = SqlTypeConverter.ChangeType<T>(value, columnType: reader.GetFieldType(0), columnName: reader.GetName(0));
                list.Add(convertedValue);
            }
            return list;
        }
        private async Task<List<object?>> ScalarListAsync(DbDataReader reader, CancellationToken cancellationToken)
        {
            var list = new List<object?>();
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                object? value = reader.GetValue(0);
                NullIfDBNull(ref value);
                list.Add(value);
            }
            return list;
        }


        Task<T> IAsyncSqlReader.ScalarOrDefault<T>()
        {
            return WrapperAsync(ScalarOrDefaultAsyncInternal<T>, CancellationToken.None);
        }
        Task<T> IAsyncSqlReader.ScalarOrDefault<T>(CancellationToken cancellationToken)
        {
            return WrapperAsync(ScalarOrDefaultAsyncInternal<T>, cancellationToken);
        }

        private async Task<T> ScalarOrDefaultAsyncInternal<T>(DbDataReader reader, CancellationToken cancellationToken)
        {
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                object? value = reader.GetValue(0);
                Type columnType = reader.GetFieldType(0);
                string columnName = reader.GetName(0);
                NullIfDBNull(ref value);
                value = SqlTypeConverter.ChangeType(value, typeof(T), columnType, columnName);
                return (T)value;
            }
            else
            {
                return default;
            }
        }


        Task<List<T>> IAsyncSqlReader.List<T>()
        {
            return WrapperAsync(ListAsync<T>, CancellationToken.None);
        }
        Task<List<T>> IAsyncSqlReader.List<T>(CancellationToken cancellationToken)
        {
            return WrapperAsync(ListAsync<T>, cancellationToken);
        }
        Task<List<T>> IAsyncSqlReader.List<T>(T anonymousType) where T : class
        {
            return WrapperAsync(AnonymousListAsync<T>, CancellationToken.None);
        }
        Task<List<T>> IAsyncSqlReader.List<T>(T anonymousType, CancellationToken cancellationToken)
        {
            return WrapperAsync(AnonymousListAsync<T>, cancellationToken);
        }
        Task<List<T>> IAsyncSqlReader.List<T>(Action<T, DbDataReader> selector)
        {
            return AsAsync.List(selector, CancellationToken.None);
        }
        Task<List<T>> IAsyncSqlReader.List<T>(Action<T, DbDataReader> selector, CancellationToken cancellationToken)
        {
            return WrapperAsync(Wrap, selector, cancellationToken);

            Task<List<T>> Wrap(DbDataReader reader, Action<T, DbDataReader> sel, CancellationToken token)
            {
                return ListAsync(reader, sel, token);
            }
        }
        Task<List<T>> IAsyncSqlReader.List<T>(Func<DbDataReader, T> selector)
        {
            return AsAsync.List(selector, CancellationToken.None);
        }
        Task<List<T>> IAsyncSqlReader.List<T>(Func<DbDataReader, T> selector, CancellationToken cancellationToken)
        {
            return WrapperAsync(Wrap, selector, cancellationToken);

            Task<List<T>> Wrap(DbDataReader reader, Func<DbDataReader, T> sel, CancellationToken token)
            {
                return ListAsync(reader, sel, token);
            }
        }
        private async Task<List<T>> ListAsync<T>(DbDataReader reader, Func<DbDataReader, T> selector, CancellationToken cancellationToken)
        {
            var list = new List<T>();
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                T result = selector(reader);
                list.Add(result);
            }
            return list;
        }
        private async Task<List<T>> AnonymousListAsync<T>(DbDataReader reader, CancellationToken cancellationToken) where T : class
        {
            var list = new List<T>();
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var toObject = new AnonymousObjectMapper<T>(reader);
                do
                {
                    T result = toObject.ReadObject();
                    list.Add(result);

                } while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false));
            }
            return list;
        }
        private async Task<List<TResult>> AnonumouseListAsync<TAnon, TResult>(DbDataReader reader, Func<TAnon, TResult> selector, CancellationToken cancellationToken) where TAnon : class
        {
            var list = new List<TResult>();
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var toObject = new AnonymousObjectMapper<TAnon>(reader);
                do
                {
                    TResult result = AnonToResult(toObject, selector);
                    list.Add(result);

                } while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false));
            }
            return list;

            static TResult AnonToResult(AnonymousObjectMapper<TAnon> toObject, Func<TAnon, TResult> selector)
            {
                TAnon anonObj = toObject.ReadObject();
                TResult result = selector(anonObj);
                return result;
            }
        }
        private async Task<List<T>> ListAsync<T>(DbDataReader reader, CancellationToken cancellationToken)
        {
            var list = new List<T>();
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var toObject = new ObjectMapper<T>(reader);
                do
                {
                    var result = (T)toObject.ReadObject();
                    list.Add(result);

                } while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false));
            }
            return list;
        }
        private async Task<List<T>> ListAsync<T>(DbDataReader reader, Action<T, DbDataReader> selector, CancellationToken cancellationToken) where T : class
        {
            var list = new List<T>();
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var toObject = new ObjectMapper<T>(reader);
                do
                {
                    var result = (T)toObject.ReadObject();
                    selector(result, reader);
                    list.Add(result);

                } while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false));
            }
            return list;
        }


        Task<T[]> IAsyncSqlReader.Array<T>()
        {
            return AsAsync.Array<T>(CancellationToken.None);
        }
        Task<T[]> IAsyncSqlReader.Array<T>(T anonymousType)
        {
            return AsAsync.Array(anonymousType, CancellationToken.None);
        }
        Task<T[]> IAsyncSqlReader.Array<T>(Action<T, DbDataReader> selector, CancellationToken cancellationToken)
        {
            return AsAsync.Array(selector, CancellationToken.None);
        }
        async Task<T[]> IAsyncSqlReader.Array<T>(Action<T, DbDataReader> selector)
        {
            List<T> list = await AsAsync.List(selector).ConfigureAwait(false);

            if (list.Count > 0)
                return list.ToArray();

            return SystemArray.Empty<T>();
        }
        async Task<T[]> IAsyncSqlReader.Array<T>(CancellationToken cancellationToken)
        {
            List<T> list = await AsAsync.List<T>(cancellationToken).ConfigureAwait(false);

            return list.Count > 0 
                ? list.ToArray() 
                : SystemArray.Empty<T>();
        }
        Task<T[]> IAsyncSqlReader.Array<T>(T anonymousType, CancellationToken cancellationToken)
        {
            Task<List<T>> task = AsAsync.List(anonymousType, cancellationToken);
            if (task.IsCompletedSuccessfully())
            {
                List<T> list = task.Result;
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
        async Task<T[]> IAsyncSqlReader.Array<T>(Func<DbDataReader, T> selector)
        {
            List<T> list = await AsAsync.List(selector).ConfigureAwait(false);

            if (list.Count > 0)
                return list.ToArray();

            return SystemArray.Empty<T>();
        }
        async Task<T[]> IAsyncSqlReader.Array<T>(Func<DbDataReader, T> selector, CancellationToken cancellationToken)
        {
            List<T> list = await AsAsync.List(selector, cancellationToken).ConfigureAwait(false);

            if (list.Count > 0)
                return list.ToArray();

            return SystemArray.Empty<T>();
        }


        Task<T> IAsyncSqlReader.Single<T>()
        {
            return AsAsync.Single<T>(CancellationToken.None);
        }
        Task<T> IAsyncSqlReader.Single<T>(T anonymousType)
        {
            return AsAsync.Single(anonymousType, CancellationToken.None);
        }
        Task<T> IAsyncSqlReader.Single<T>(CancellationToken cancellationToken)
        {
            return WrapperAsync(SingleAsync<T>, cancellationToken);
        }
        Task<T> IAsyncSqlReader.Single<T>(T anonymousType, CancellationToken cancellationToken)
        {
            return WrapperAsync(AnonymousSingleAsync<T>, cancellationToken);
        }
        Task<T> IAsyncSqlReader.Single<T>(Action<T, DbDataReader> selector)
        {
            return AsAsync.Single(selector, CancellationToken.None);
        }
        Task<T> IAsyncSqlReader.Single<T>(Action<T, DbDataReader> selector, CancellationToken cancellationToken)
        {
            return WrapperAsync(Wrap, selector, cancellationToken);

            Task<T> Wrap(DbDataReader reader, Action<T, DbDataReader> sel, CancellationToken token)
            {
                return SingleAsync(reader, sel, token);
            }
        }
        Task<T> IAsyncSqlReader.Single<T>(Func<DbDataReader, T> selector)
        {
            return AsAsync.Single(selector, CancellationToken.None);
        }
        Task<T> IAsyncSqlReader.Single<T>(Func<DbDataReader, T> selector, CancellationToken cancellationToken)
        {
            return WrapperAsync(Wrap, selector, cancellationToken);

            Task<T> Wrap(DbDataReader reader, Func<DbDataReader, T> sel, CancellationToken token)
            {
                return SingleAsync(reader, sel, token);
            }
        }
        private Task<T> SingleAsync<T>(DbDataReader reader, CancellationToken cancellationToken)
        {
            Task<bool> task = reader.ReadAsync(cancellationToken);
            if (task.IsCompletedSuccessfully())
            {
                var objectMapper = new ObjectMapper<T>(reader);
                T value = Map(reader);
                return Task.FromResult(value);
            }
            else
            {
                return WaitAsync(task, reader);

                static async Task<T> WaitAsync(Task<bool> task, DbDataReader reader)
                {
                    await task.ConfigureAwait(false);
                    return Map(reader);
                }
            }

            static T Map(DbDataReader reader)
            {
                var objectMapper = new ObjectMapper<T>(reader);
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

                static async Task<T> WaitAsync(Task<bool> task, DbDataReader reader)
                {
                    await task.ConfigureAwait(false);
                    return Map(reader);
                }
            }

            static T Map(DbDataReader reader)
            {
                var toObject = new AnonymousObjectMapper<T>(reader);
                return toObject.ReadObject();
            }
        }
        private async Task<T> SingleAsync<T>(DbDataReader reader, Action<T, DbDataReader> selector, CancellationToken cancellationToken) where T : class
        {
            await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            var toObject = new ObjectMapper<T>(reader);
            var item = (T)toObject.ReadObject();
            selector(item, reader);
            return item;
        }
        private async Task<T> SingleAsync<T>(DbDataReader reader, Func<DbDataReader, T> selector, CancellationToken cancellationToken)
        {
            await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            T result = selector(reader);
            return result;
        }


        Task<T> IAsyncSqlReader.SingleOrDefault<T>()
        {
            return AsAsync.SingleOrDefault<T>(CancellationToken.None);
        }
        Task<T> IAsyncSqlReader.SingleOrDefault<T>(T anonymousType)
        {
            return AsAsync.SingleOrDefault(anonymousType, CancellationToken.None);
        }
        Task<T> IAsyncSqlReader.SingleOrDefault<T>(CancellationToken cancellationToken)
        {
            return WrapperAsync(SingleOrDefaultAsync<T>, cancellationToken);
        }
        Task<T> IAsyncSqlReader.SingleOrDefault<T>(T anonymousType, CancellationToken cancellationToken)
        {
            return WrapperAsync(AnonymousSingleOrDefaultAsync<T>, cancellationToken)!;
        }
        Task<T> IAsyncSqlReader.SingleOrDefault<T>(Action<T, DbDataReader> selector)
        {
            return AsAsync.SingleOrDefault(selector, CancellationToken.None);
        }
        Task<T> IAsyncSqlReader.SingleOrDefault<T>(Action<T, DbDataReader> selector, CancellationToken cancellationToken)
        {
            return WrapperAsync(Wrap, selector, cancellationToken);

            Task<T> Wrap(DbDataReader reader, Action<T, DbDataReader> sel, CancellationToken token)
            {
                return SingleOrDefaultAsync(reader, sel, token);
            }
        }
        Task<T> IAsyncSqlReader.SingleOrDefault<T>(Func<DbDataReader, T> selector)
        {
            return AsAsync.SingleOrDefault(selector, CancellationToken.None);
        }
        Task<T> IAsyncSqlReader.SingleOrDefault<T>(Func<DbDataReader, T> selector, CancellationToken cancellationToken)
        {
            return WrapperAsync(Wrap, selector, cancellationToken);

            Task<T> Wrap(DbDataReader reader, Func<DbDataReader, T> sel, CancellationToken token)
            {
                return SingleOrDefaultAsync(reader, sel, token);
            }
        }
        private async Task<T> SingleOrDefaultAsync<T>(DbDataReader reader, Func<DbDataReader, T> selector, CancellationToken cancellationToken)
        {
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                T result = selector(reader);
                return result;
            }
            else
            {
                return default;
            }
        }
        private async Task<T> SingleOrDefaultAsync<T>(DbDataReader reader, CancellationToken cancellationToken)
        {
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var objectMapper = new ObjectMapper<T>(reader);
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

                static async Task<T?> WaitAsync(Task<bool> task, DbDataReader reader)
                {
                    bool hasRows = await task.ConfigureAwait(false);
                    return MapAnonymousObject(hasRows, reader);
                }
            }

            static T? MapAnonymousObject(bool hasRows, DbDataReader reader)
            {
                if (hasRows)
                {
                    var toObject = new AnonymousObjectMapper<T>(reader);
                    return toObject.ReadObject();
                }
                else
                {
                    return default;
                }
            }
        }
        private async Task<T> SingleOrDefaultAsync<T>(DbDataReader reader, Action<T, DbDataReader> selector, CancellationToken cancellationToken)
        {
            T item = await SingleOrDefaultAsync<T>(reader, cancellationToken).ConfigureAwait(false);
            selector(item, reader);
            return item;
        }


        Task<int> IAsyncSqlReader.Execute()
        {
            return WrapperAsync(ExecuteAsync, CancellationToken.None);
        }
        Task<int> IAsyncSqlReader.Execute(CancellationToken cancellationToken)
        {
            return WrapperAsync(ExecuteAsync, cancellationToken);
        }
        private Task<int> ExecuteAsync(DbDataReader reader, CancellationToken cancellationToken)
        {
            return Task.FromResult(reader.RecordsAffected);
        }

        private Task<T> WrapperAsync<T>(Func<DbDataReader, CancellationToken, Task<T>> selector, CancellationToken cancellationToken)
        {
            return WrapperAsync(Wrap, selector, cancellationToken);
        }

        [DebuggerStepThrough]
        private static Task<T> Wrap<T>(DbDataReader reader, Func<DbDataReader, CancellationToken, Task<T>> sel, CancellationToken cancellationToken)
        {
            return sel(reader, cancellationToken);
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
            return Wrapper(Wrap, selector);

            static T Wrap(DbDataReader reader, Func<DbDataReader, T> sel)
            {
                return sel(reader);
            }
        }
        private T Wrapper<T, TArg>(Func<DbDataReader, TArg, T> selector, TArg state)
        {
            using (ICommandReader comReader = GetCommandReader())
            {
                DbDataReader reader = comReader.GetReader();
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
