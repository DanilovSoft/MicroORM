using System.Collections.Generic;
using System.Data;

namespace DanilovSoft.MicroORM
{
    public interface ISqlReader
    {
        IAsyncSqlReader ToAsync();
        int Execute();
        DataTable Table();
        object? Scalar();
        T Scalar<T>();
        object?[] ScalarArray();
        List<object?> ScalarList();
        T[] ScalarArray<T>();
        List<T> ScalarList<T>();
        T? ScalarOrDefault<T>();
        T Single<T>();
        T Single<T>(T anonymousType) where T : class;
        T? SingleOrDefault<T>();
        T? SingleOrDefault<T>(T anonymousType) where T : class;
        List<T> List<T>();
        List<T> List<T>(T anonymousType) where T : class;
        T[] Array<T>();
        T[] Array<T>(T anonymousType) where T : class;
        TCollection Collection<TItem, TCollection>() where TCollection : ICollection<TItem>, new();
    }
}
