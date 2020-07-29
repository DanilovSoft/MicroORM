using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM
{
    public interface IAsyncSqlReader
    {
        Task<int> Execute();
        /// <exception cref="OperationCanceledException"/>
        Task<int> Execute(CancellationToken cancellationToken);
        Task<DataTable> Table();
        /// <exception cref="OperationCanceledException"/>
        Task<DataTable> Table(CancellationToken cancellationToken);
        Task<object> Scalar();
        /// <exception cref="OperationCanceledException"/>
        Task<object> Scalar(CancellationToken cancellationToken);
        Task<object[]> ScalarArray();
        /// <exception cref="OperationCanceledException"/>
        Task<object[]> ScalarArray(CancellationToken cancellationToken);
        Task<List<object>> ScalarList();
        /// <exception cref="OperationCanceledException"/>
        Task<List<object>> ScalarList(CancellationToken cancellationToken);
        Task<T[]> ScalarArray<T>();
        /// <exception cref="OperationCanceledException"/>
        Task<T[]> ScalarArray<T>(CancellationToken cancellationToken);
        Task<List<T>> ScalarList<T>();
        /// <exception cref="OperationCanceledException"/>
        Task<List<T>> ScalarList<T>(CancellationToken cancellationToken);
        Task<T> Scalar<T>();
        /// <exception cref="OperationCanceledException"/>
        Task<T> Scalar<T>(CancellationToken cancellationToken);
        Task<T> ScalarOrDefault<T>();
        /// <exception cref="OperationCanceledException"/>
        Task<T> ScalarOrDefault<T>(CancellationToken cancellationToken);
        Task<T> Single<T>();
        /// <exception cref="OperationCanceledException"/>
        Task<T> Single<T>(CancellationToken cancellationToken);
        Task<T> Single<T>(T anonymousType) where T : class;
        /// <exception cref="OperationCanceledException"/>
        Task<T> Single<T>(T anonymousType, CancellationToken cancellationToken) where T : class;
        Task<T> Single<T>(Action<T, DbDataReader> selector) where T : class;
        /// <exception cref="OperationCanceledException"/>
        Task<T> Single<T>(Action<T, DbDataReader> selector, CancellationToken cancellationToken) where T : class;
        Task<T> Single<T>(Func<DbDataReader, T> selector);
        /// <exception cref="OperationCanceledException"/>
        Task<T> Single<T>(Func<DbDataReader, T> selector, CancellationToken cancellationToken);
        Task<T> SingleOrDefault<T>();
        /// <exception cref="OperationCanceledException"/>
        Task<T> SingleOrDefault<T>(CancellationToken cancellationToken);
        Task<T> SingleOrDefault<T>(T anonymousType) where T : class;
        /// <exception cref="OperationCanceledException"/>
        Task<T> SingleOrDefault<T>(T anonymousType, CancellationToken cancellationToken) where T : class;
        Task<T> SingleOrDefault<T>(Action<T, DbDataReader> selector) where T : class;
        /// <exception cref="OperationCanceledException"/>
        Task<T> SingleOrDefault<T>(Action<T, DbDataReader> selector, CancellationToken cancellationToken) where T : class;
        Task<T> SingleOrDefault<T>(Func<DbDataReader, T> selector);
        /// <exception cref="OperationCanceledException"/>
        Task<T> SingleOrDefault<T>(Func<DbDataReader, T> selector, CancellationToken cancellationToken);
        Task<List<T>> List<T>();
        /// <exception cref="OperationCanceledException"/>
        Task<List<T>> List<T>(CancellationToken cancellationToken);
        Task<List<T>> List<T>(T anonymousType) where T : class;
        /// <exception cref="OperationCanceledException"/>
        Task<List<T>> List<T>(T anonymousType, CancellationToken cancellationToken) where T : class;
        Task<List<T>> List<T>(Func<DbDataReader, T> selector);
        /// <exception cref="OperationCanceledException"/>
        Task<List<T>> List<T>(Func<DbDataReader, T> selector, CancellationToken cancellationToken);
        Task<List<T>> List<T>(Action<T, DbDataReader> selector) where T : class;
        /// <exception cref="OperationCanceledException"/>
        Task<List<T>> List<T>(Action<T, DbDataReader> selector, CancellationToken cancellationToken) where T : class;
        Task<T[]> Array<T>();
        /// <exception cref="OperationCanceledException"/>
        Task<T[]> Array<T>(CancellationToken cancellationToken);
        Task<T[]> Array<T>(T anonymousType) where T : class;
        /// <exception cref="OperationCanceledException"/>
        Task<T[]> Array<T>(T anonymousType, CancellationToken cancellationToken) where T : class;
        Task<T[]> Array<T>(Func<DbDataReader, T> selector);
        /// <exception cref="OperationCanceledException"/>
        Task<T[]> Array<T>(Func<DbDataReader, T> selector, CancellationToken cancellationToken);
        Task<T[]> Array<T>(Action<T, DbDataReader> selector) where T : class;
        /// <exception cref="OperationCanceledException"/>
        Task<T[]> Array<T>(Action<T, DbDataReader> selector, CancellationToken cancellationToken) where T : class;
        Task<TCollection> Collection<TItem, TCollection>() where TCollection : ICollection<TItem>, new();
        /// <exception cref="OperationCanceledException"/>
        Task<TCollection> Collection<TItem, TCollection>(CancellationToken cancellationToken) where TCollection : ICollection<TItem>, new();
        Task<TCollection> Collection<TItem, TCollection>(Action<TItem, DbDataReader> selector) where TCollection : ICollection<TItem>, new() where TItem : class;
        /// <exception cref="OperationCanceledException"/>
        Task<TCollection> Collection<TItem, TCollection>(Action<TItem, DbDataReader> selector, CancellationToken cancellationToken) where TCollection : ICollection<TItem>, new() where TItem : class;

        IAsyncAnonymousReader<T> AsAnonymous<T>(T anonymousType) where T : class;
    }
}
