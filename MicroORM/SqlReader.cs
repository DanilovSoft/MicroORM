using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using DanilovSoft.MicroORM.ObjectMapping;
using static DanilovSoft.MicroORM.ExceptionMessages;
using SystemArray = System.Array;

using static DanilovSoft.MicroORM.Helpers.NullableHelper;

namespace DanilovSoft.MicroORM;

public abstract class SqlReader : ISqlReader
{
    private readonly int _closeConnectionPenaltySec = SqlORM.CloseConnectionPenaltySec;
    private readonly SqlORM _parent;

    internal SqlReader(SqlORM parent)
    {
        Debug.Assert(parent != null);

        _parent = parent;
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
        return Wrapper(reader => reader.RecordsAffected);
    }

    public DataTable Table()
    {
        return Wrapper(Table);
    }

    public object? Scalar()
    {
        return Wrapper(Scalar);
    }

    public T Scalar<T>()
    {
        // Имея только T невозможно определить null-ref конвенцию поэтому разрешаем возврат null.

        return (T)Wrapper(Scalar<T>)!;
    }

    public object?[] ScalarArray()
    {
        var list = ScalarList();

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
        var list = ScalarList<T>();

        if (list.Count > 0)
        {
            return list.ToArray();
        }

        return SystemArray.Empty<T>();
    }

    public List<T> ScalarList<T>()
    {
        return Wrapper(ScalarList<T>);
    }

    public T? ScalarOrDefault<T>()
    {
        return (T?)Wrapper(ScalarOrDefault<T>);
    }

    public T Single<T>()
    {
        return (T)Wrapper(Single<T>);
    }

    public T Single<T>(T anonymousType) where T : class
    {
        return Wrapper(AnonymousSingle<T>);
    }

    public T? SingleOrDefault<T>()
    {
        return (T?)Wrapper(SingleOrDefault<T>);
    }

    public T? SingleOrDefault<T>(T anonymousType) where T : class
    {
        return Wrapper(AnonymousSingleOrDefault<T>);
    }

    public IAsyncAnonymousReader<T> AsAnonymousAsync<T>(T anonymousType) where T : class
    {
        return new Anonimous<T>(this);
    }

    public IAnonymousReader<T> AsAnonymous<T>(T anonymousType) where T : class
    {
        return new Anonimous<T>(this);
    }
    
    public List<T> ToList<T>()
    {
        return Wrapper(List<T>);
    }

    public List<T> ToList<T>(T anonymousType) where T : class
    {
        return Wrapper(AnonumouseList<T>);
    }

    public T[] ToArray<T>()
    {
        var list = ToList<T>();
        if (list.Count > 0)
        {
            return list.ToArray();
        }

        return SystemArray.Empty<T>();
    }

    public T[] ToArray<T>(T anonymousType) where T : class
    {
        var list = ToList(anonymousType);
        if (list.Count > 0)
        {
            return list.ToArray();
        }

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
        return WrapperAsync(static (r, state, canc) => state.CollectionAsync<TItem, TCollection>(r, canc), this, cancellationToken);
    }

    public Task<DataTable> TableAsync()
    {
        return WrapperAsync(static (r, state, canc) => TableAsync(r, canc), this, CancellationToken.None);
    }

    public Task<DataTable> TableAsync(CancellationToken cancellationToken)
    {
        return WrapperAsync(static (r, canc) => TableAsync(r, canc), cancellationToken);
    }

    public Task<object?> ScalarAsync()
    {
        return WrapperAsync(static (r, canc) => ScalarAsync(r, canc), CancellationToken.None);
    }

    public Task<object?> ScalarAsync(CancellationToken cancellationToken)
    {
        return WrapperAsync(static (r, canc) => ScalarAsync(r, canc), cancellationToken);
    }

    public Task<T> ScalarAsync<T>()
    {
        return WrapperAsync(static (r, canc) => ScalarAsync<T>(r, canc), CancellationToken.None);
    }

    public Task<T> ScalarAsync<T>(CancellationToken cancellationToken)
    {
        return WrapperAsync(static (r, canc) => ScalarAsync<T>(r, canc), cancellationToken);
    }

    public async Task<object?[]> ScalarArrayAsync()
    {
        var list = await ScalarListAsync().ConfigureAwait(false);

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
        {
            return list.ToArray();
        }

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
        {
            return list.ToArray();
        }

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
        return WrapperAsync(static (r, canc) => ScalarListAsync(r, canc), cancellationToken);
    }

    
    public Task<List<T>> ScalarListAsync<T>(CancellationToken cancellationToken)
    {
        return WrapperAsync(static (r, canc) => ScalarListAsync<T>(r, canc), cancellationToken);
    }

    
    public Task<T?> ScalarOrDefaultAsync<T>()
    {
        return WrapperAsync(static (r, canc) => ScalarOrDefaultCoreAsync<T>(r, canc), CancellationToken.None);
    }

    
    public Task<T?> ScalarOrDefaultAsync<T>(CancellationToken cancellationToken)
    {
        return WrapperAsync(static (r, canc) => ScalarOrDefaultCoreAsync<T>(r, canc), cancellationToken);
    }

    
    public Task<List<T>> ToListAsync<T>()
    {
        return WrapperAsync(static (r, state, canc) => state.ListAsync<T>(r, canc), this, CancellationToken.None);
    }

    
    public Task<List<T>> ToListAsync<T>(CancellationToken cancellationToken)
    {
        return WrapperAsync(static (r, state, canc) => state.ListAsync<T>(r, canc), this, cancellationToken);
    }

    
    public Task<List<T>> ToListAsync<T>(T anonymousType) where T : class
    {
        return WrapperAsync(static (r, state, canc) => state.AnonymousListAsync<T>(r, canc), this, CancellationToken.None);
    }

    
    public Task<List<T>> ToListAsync<T>(T anonymousType, CancellationToken cancellationToken) where T : class
    {
        return WrapperAsync(static (r, state, canc) => state.AnonymousListAsync<T>(r, canc), this, cancellationToken);
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
        var list = await ToListAsync<T>(cancellationToken).ConfigureAwait(false);

        return list.Count > 0
            ? list.ToArray()
            : SystemArray.Empty<T>();
    }

    
    public Task<T[]> ToArrayAsync<T>(T anonymousType, CancellationToken cancellationToken) where T : class
    {
        var task = ToListAsync(anonymousType, cancellationToken);
        if (task.IsCompletedSuccessfully)
        {
            var list = task.Result;
            var array = ToArray(list);
            return Task.FromResult(array);
        }
        
        return Wait(task);
        static async Task<T[]> Wait(Task<List<T>> task)
        {
            var list = await task.ConfigureAwait(false);
            return ToArray(list);
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
        return WrapperAsync(static (r, state, canc) => state.SingleAsync<T>(r, canc), this, cancellationToken);
    }

    
    public Task<T> SingleAsync<T>(T anonymousType, CancellationToken cancellationToken) where T : class
    {
        return WrapperAsync(static (r, state, canc) => state.AnonymousSingleAsync<T>(r, canc), this, cancellationToken);
    }

    
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
        return WrapperAsync(static (r, state, canc) => state.SingleOrDefaultAsync<T>(r, canc), this, cancellationToken);
    }

    
    public Task<T?> SingleOrDefaultAsync<T>(T anonymousType, CancellationToken cancellationToken) where T : class
    {
        return WrapperAsync(static (r, state, canc) => state.AnonymousSingleOrDefaultAsync<T>(r, canc), this, cancellationToken)!;
    }

    
    public Task<int> ExecuteAsync()
    {
        return WrapperAsync(static (r, canc) => ExecuteAsync(r), CancellationToken.None);
    }

    
    public Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        return WrapperAsync(static (r, canc) => ExecuteAsync(r), cancellationToken);
    }

    private static DataTable Table(DbDataReader reader)
    {
        var table = new DataTable("Table1");
        try
        {
            table.LoadData(reader);
            return SetNull(ref table);
        }
        finally
        {
            table?.Dispose();
        }
    }

    private static async Task<T?> ScalarOrDefaultCoreAsync<T>(DbDataReader reader, CancellationToken cancellationToken)
    {
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var sqlRawValue = reader.GetValue(0);
            return SqlTypeConverter.ConvertRawSqlToClrType<T>(sqlRawValue, reader.GetFieldType(0), reader.GetName(0))!;
        }
        else
        {
            return default;
        }
    }

    private static async Task<List<T>> ScalarListAsync<T>(DbDataReader reader, CancellationToken cancellationToken)
    {
        var list = new List<T>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var sqlRawValue = reader.GetValue(0);
            var convertedValue = SqlTypeConverter.ConvertRawSqlToClrType<T>(sqlRawValue, sqlColumnType: reader.GetFieldType(0), sqlColumnName: reader.GetName(0));
            list.Add(convertedValue);
        }
        return list;
    }

    private static object? Scalar(DbDataReader reader)
    {
        reader.Read();
        var sqlRawValue = reader.GetValue(0);
        return NullIfDBNull(sqlRawValue);
    }

    private static object? Scalar<T>(DbDataReader reader)
    {
        reader.Read();
        var sqlRawValue = reader.GetValue(0);
        return SqlTypeConverter.ConvertRawSqlToClrType(sqlRawValue, reader.GetFieldType(0), reader.GetName(0), toType: typeof(T));
    }

    private static List<T> ScalarList<T>(DbDataReader reader)
    {
        var list = new List<T>();
        while (reader.Read())
        {
            var sqlRawValue = reader.GetValue(0);
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
            var sqlRawValue = reader.GetValue(0);
            var sqlValue = NullIfDBNull(sqlRawValue);
            list.Add(sqlValue);
        }
        return list;
    }

    private static object? ScalarOrDefault<T>(DbDataReader reader)
    {
        if (reader.Read())
        {
            var sqlRawValue = reader.GetValue(0);
            return SqlTypeConverter.ConvertRawSqlToClrType(sqlRawValue, reader.GetFieldType(0), reader.GetName(0), toType: typeof(T));
        }
        else
        {
            return default(T);
        }
    }

    private static async Task<T> ScalarAsync<T>(DbDataReader reader, CancellationToken cancellationToken)
    {
        await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        var sqlRawValue = reader.GetValue(0);
        return (T)SqlTypeConverter.ConvertRawSqlToClrType(sqlRawValue, reader.GetFieldType(0), reader.GetName(0), typeof(T))!;
    }

    private static Task<DataTable> TableAsync(DbDataReader reader, CancellationToken cancellationToken)
    {
        var table = new DataTable("Table1");
        try
        {
            var task = table.LoadAsync(reader, cancellationToken);

            if (task.IsCompletedSuccessfully())
            {
                return Task.FromResult(SetNull(ref table));
            }
            
            return Wait(task, SetNull(ref table));
            static async Task<DataTable> Wait(Task task, [DisallowNull] DataTable? table)
            {
                try
                {
                    await task.ConfigureAwait(false);
                    return SetNull(ref table);
                }
                finally
                {
                    table?.Dispose();
                }
            }
        }
        finally
        {
            table?.Dispose();
        }
    }

    private static Task<object?> ScalarAsync(DbDataReader reader, CancellationToken cancellationToken)
    {
        var task = reader.ReadAsync(cancellationToken);

        if (task.IsCompletedSuccessfully)
        {
            var value = Read(reader);
            return Task.FromResult(value);
        }

        return Wait(task, reader);
        static async Task<object?> Wait(Task<bool> task, DbDataReader reader)
        {
            await task.ConfigureAwait(false);
            return Read(reader);
        }

        static object? Read(DbDataReader reader)
        {
            var sqlRawValue = reader.GetValue(0);
            return NullIfDBNull(sqlRawValue);
        }
    }

    private static async Task<List<object?>> ScalarListAsync(DbDataReader reader, CancellationToken cancellationToken)
    {
        var list = new List<object?>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var sqlRawValue = reader.GetValue(0);
            var sqlValue = NullIfDBNull(sqlRawValue);
            list.Add(sqlValue);
        }
        return list;
    }

    private static Task<int> ExecuteAsync(DbDataReader reader)
    {
        return Task.FromResult(reader.RecordsAffected);
    }

    private object Single<T>(DbDataReader reader) // T - сложный тип и не может быть Null.
    {
        reader.Read();
        var toObject = new ObjectMapper<T>(reader, _parent);
        return toObject.ReadObject();
    }

    private T AnonymousSingle<T>(DbDataReader reader) where T : class
    {
        reader.Read();
        var toObject = new ObjectMapper<T>(reader, _parent);
        return toObject.ReadAsAnonymousObject<T>();
    }

    private object? SingleOrDefault<T>(DbDataReader reader)
    {
        if (reader.Read())
        {
            var toObject = new ObjectMapper<T>(reader, _parent);
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
            var toObject = new ObjectMapper<T>(reader, _parent);
            return toObject.ReadAsAnonymousObject<T>();
        }
        else
        {
            return null;
        }
    }

    private List<TResult> FromAnonList<TAnon, TResult>(Func<TAnon, TResult> selector) where TAnon : class
    {
        return Wrapper(selector, (reader, selector) => AnonumouseList(reader, selector));
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

    private List<TAnon> AnonumouseList<TAnon>(DbDataReader reader) where TAnon : class
    {
        var list = new List<TAnon>();
        if (reader.Read())
        {
            var toObject = new ObjectMapper<TAnon>(reader, _parent);
            do
            {
                var rowObj = toObject.ReadAsAnonymousObject<TAnon>();
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
            var toObject = new ObjectMapper<TAnon>(reader, _parent);
            do
            {
                var result = AnonToResult(toObject, selector);
                list.Add(result);
            } while (reader.Read());
        }
        return list;

        static TResult AnonToResult(ObjectMapper<TAnon> toObject, Func<TAnon, TResult> selector)
        {
            var row = toObject.ReadAsAnonymousObject<TAnon>();
            var result = selector(row);
            return result;
        }
    }

    private List<T> List<T>(DbDataReader reader)
    {
        var list = new List<T>();
        if (reader.Read())
        {
            var toObject = new ObjectMapper<T>(reader, _parent);
            do
            {
                var result = (T)toObject.ReadObject();
                list.Add(result);

            } while (reader.Read());
        }
        return list;
    }

    private async Task<TCollection> CollectionAsync<TItem, TCollection>(DbDataReader reader, CancellationToken cancellationToken) where TCollection : ICollection<TItem>, new()
    {
        var list = new TCollection();
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var toObject = new ObjectMapper<TItem>(reader, _parent);
            do
            {
                var item = (TItem)toObject.ReadObject();
                list.Add(item);

            } while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false));
        }
        return list;
    }

    private async Task<List<T>> AnonymousListAsync<T>(DbDataReader reader, CancellationToken cancellationToken) where T : class
    {
        var list = new List<T>();
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var toObject = new ObjectMapper<T>(reader, _parent);
            do
            {
                var result = toObject.ReadAsAnonymousObject<T>();
                list.Add(result);

            } while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false));
        }
        return list;
    }

    private async Task<List<TResult>> AnonumouseListAsync<TAnon, TResult>(DbDataReader reader,
        Func<TAnon, TResult> selector,
        CancellationToken cancellationToken) where TAnon : class
    {
        var list = new List<TResult>();
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var toObject = new ObjectMapper<TAnon>(reader, _parent);
            do
            {
                var result = AnonToResult(toObject, selector);
                list.Add(result);

            } while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false));
        }
        return list;

        static TResult AnonToResult(ObjectMapper<TAnon> toObject, Func<TAnon, TResult> selector)
        {
            var anonObj = toObject.ReadAsAnonymousObject<TAnon>();
            var result = selector(anonObj);
            return result;
        }
    }

    private async Task<List<T>> ListAsync<T>(DbDataReader reader, CancellationToken cancellationToken)
    {
        var list = new List<T>();
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var toObject = new ObjectMapper<T>(reader, _parent);
            do
            {
                var result = (T)toObject.ReadObject();
                list.Add(result);

            } while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false));
        }
        return list;
    }

    private async Task<T?> SingleOrDefaultAsync<T>(DbDataReader reader, CancellationToken cancellationToken)
    {
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var objectMapper = new ObjectMapper<T>(reader, _parent);
            return (T)objectMapper.ReadObject();
        }
        else
        {
            return default;
        }
    }

    private Task<T> SingleAsync<T>(DbDataReader reader, CancellationToken cancellationToken)
    {
        var task = reader.ReadAsync(cancellationToken);
        if (task.IsCompletedSuccessfully())
        {
            var objectMapper = new ObjectMapper<T>(reader, _parent);
            var value = Map(reader);
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
            var objectMapper = new ObjectMapper<T>(reader, _parent);
            return (T)objectMapper.ReadObject();
        }
    }

    private Task<T> AnonymousSingleAsync<T>(DbDataReader reader, CancellationToken cancellationToken) where T : class
    {
        var task = reader.ReadAsync(cancellationToken);

        if (task.IsCompletedSuccessfully)
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
            var toObject = new ObjectMapper<T>(reader, _parent);
            return toObject.ReadAsAnonymousObject<T>();
        }
    }

    private Task<T?> AnonymousSingleOrDefaultAsync<T>(DbDataReader reader, CancellationToken cancellationToken) where T : class
    {
        var task = reader.ReadAsync(cancellationToken);

        if (task.IsCompletedSuccessfully)
        {
            var hasRows = task.Result;
            var value = MapAnonymousObject(hasRows, reader, _parent);
            return Task.FromResult(value);
        }
        else
        {
            return WaitAsync(task, reader);

            async Task<T?> WaitAsync(Task<bool> task, DbDataReader reader)
            {
                var hasRows = await task.ConfigureAwait(false);
                return MapAnonymousObject(hasRows, reader, _parent);
            }
        }

        static T? MapAnonymousObject(bool hasRows, DbDataReader reader, SqlORM sqlORM)
        {
            if (hasRows)
            {
                var toObject = new ObjectMapper<T>(reader, sqlORM);
                return toObject.ReadAsAnonymousObject<T>();
            }
            else
            {
                return null;
            }
        }
    }

    private Task<T> WrapperAsync<T>(Func<DbDataReader, CancellationToken, Task<T>> selector, CancellationToken cancellationToken)
    {
        return WrapperAsync(static (r, selector, canc) => selector(r, canc), state: selector, cancellationToken);
    }

    /// <param name="selector">Содержит перегруженный токен отмены для поддержания аварийной отмены.</param>
    /// <param name="cancellationToken">Пользовательский токен отмены.</param>
    private async Task<T> WrapperAsync<T, TArg>(Func<DbDataReader, TArg, CancellationToken, Task<T>> selector, TArg state, CancellationToken cancellationToken)
    {
        // Смешиваем пользовательский токен и аварийный таймаут.
        using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            // Установить таймаут.
            linked.CancelAfter(millisecondsDelay: QueryTimeoutSec * 1000);

            // The CommandTimeout property will be ignored during asynchronous method calls such as BeginExecuteReader.
            var comReader = await GetCommandReaderAsync(linked.Token).ConfigureAwait(false);

            // Аварийный контроль соединения. При не явном дисконнекте выполняет закрытие с дополнительной форой после QueryTimeoutSec.
            var closeConnection = new CloseConnection(_closeConnectionPenaltySec, comReader.Connection, linked.Token);
            try
            {
                // Отправляет серверу запрос на отмену выполняющегося запроса по таймауту или по запросу пользователя.
                var cancelCommandRequest = new CancelCommandRequest(comReader.Command, linked.Token);
                try
                {
                    // Инициализация запроса и ожидание готовности данных.
                    var reader = await comReader.GetReaderAsync(linked.Token).ConfigureAwait(false);

                    // Получение данных сервера.
                    return await selector(reader, state, linked.Token).ConfigureAwait(false);
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
        using (var commandReader = GetCommandReader())
        {
            var reader = commandReader.GetReader();
            return selector(reader);
        }
    }

    private T Wrapper<T, TState>(TState state, Func<DbDataReader, TState, T> selector)
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
        private readonly SqlReader _parent;

        public Anonimous(SqlReader parent)
        {
            _parent = parent;
        }

        public TResult[] Array<TResult>(Func<TAnon, TResult> selector)
            => _parent.FromAnonArray(selector);

        public List<TResult> List<TResult>(Func<TAnon, TResult> selector)
            => _parent.FromAnonList(selector);

        Task<TResult[]> IAsyncAnonymousReader<TAnon>.Array<TResult>(Func<TAnon, TResult> selector)
            => _parent.FromAnonArrayAsync(selector);

        Task<TResult[]> IAsyncAnonymousReader<TAnon>.Array<TResult>(Func<TAnon, TResult> selector, CancellationToken cancellationToken)
            => _parent.FromAnonArrayAsync(selector, cancellationToken);

        Task<List<TResult>> IAsyncAnonymousReader<TAnon>.List<TResult>(Func<TAnon, TResult> selector)
            => _parent.FromAnonListAsync(selector);

        Task<List<TResult>> IAsyncAnonymousReader<TAnon>.List<TResult>(Func<TAnon, TResult> selector, CancellationToken cancellationToken)
            => _parent.FromAnonListAsync(selector, cancellationToken);
    }
}
