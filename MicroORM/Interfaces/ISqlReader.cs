using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM
{
    public interface ISqlReader
    {
        IAsyncSqlReader ToAsync();
        int Execute();
        DataTable Table();
        object Scalar();
        T Scalar<T>();
        object[] ScalarArray();
        List<object> ScalarList();
        T[] ScalarArray<T>();
        List<T> ScalarList<T>();
        T ScalarOrDefault<T>();
        T Single<T>();
        T Single<T>(T @object);
        T Single<T>(Action<T, DbDataReader> selector) where T : class;
        T Single<T>(Func<DbDataReader, T> selector);
        T SingleOrDefault<T>();
        T SingleOrDefault<T>(T @object);
        T SingleOrDefault<T>(Action<T, DbDataReader> selector) where T : class;
        T SingleOrDefault<T>(Func<DbDataReader, T> selector);
        List<T> List<T>();
        List<T> List<T>(T @object);
        List<T> List<T>(Func<DbDataReader, T> selector);
        List<T> List<T>(Action<T, DbDataReader> selector) where T : class;
        T[] Array<T>();
        T[] Array<T>(T @object);
        T[] Array<T>(Func<DbDataReader, T> selector);
        T[] Array<T>(Action<T, DbDataReader> selector) where T : class;
        TCollection Collection<TItem, TCollection>() where TCollection : ICollection<TItem>, new();
        TCollection Collection<TItem, TCollection>(Action<TItem, DbDataReader> selector) where TCollection : ICollection<TItem>, new() where TItem : class;
    }
}
