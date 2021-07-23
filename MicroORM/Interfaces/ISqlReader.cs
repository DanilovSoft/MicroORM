using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Threading;

namespace DanilovSoft.MicroORM
{
    public interface ISqlReader
    {
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



        Task<int> ExecuteAsync();

        /// <exception cref="OperationCanceledException"/>
        Task<int> ExecuteAsync(CancellationToken cancellationToken);

        Task<DataTable> TableAsync();

        /// <exception cref="OperationCanceledException"/>
        Task<DataTable> TableAsync(CancellationToken cancellationToken);

        Task<object?> ScalarAsync();

        /// <exception cref="OperationCanceledException"/>
        Task<object?> ScalarAsync(CancellationToken cancellationToken);

        Task<object?[]> ScalarArrayAsync();

        /// <exception cref="OperationCanceledException"/>
        Task<object?[]> ScalarArrayAsync(CancellationToken cancellationToken);

        Task<List<object?>> ScalarListAsync();

        /// <exception cref="OperationCanceledException"/>
        Task<List<object?>> ScalarListAsync(CancellationToken cancellationToken);

        Task<T[]> ScalarArrayAsync<T>();

        /// <exception cref="OperationCanceledException"/>
        Task<T[]> ScalarArrayAsync<T>(CancellationToken cancellationToken);

        Task<List<T>> ScalarListAsync<T>();

        /// <exception cref="OperationCanceledException"/>
        Task<List<T>> ScalarListAsync<T>(CancellationToken cancellationToken);

        Task<T> ScalarAsync<T>();

        /// <exception cref="OperationCanceledException"/>
        Task<T> ScalarAsync<T>(CancellationToken cancellationToken);

        Task<T?> ScalarOrDefaultAsync<T>();

        /// <exception cref="OperationCanceledException"/>
        Task<T?> ScalarOrDefaultAsync<T>(CancellationToken cancellationToken);

        Task<T> SingleAsync<T>();

        /// <exception cref="OperationCanceledException"/>
        Task<T> SingleAsync<T>(CancellationToken cancellationToken);

        Task<T> SingleAsync<T>(T anonymousType) where T : class;

        /// <exception cref="OperationCanceledException"/>
        Task<T> SingleAsync<T>(T anonymousType, CancellationToken cancellationToken) where T : class;

        Task<T?> SingleOrDefaultAsync<T>();

        /// <exception cref="OperationCanceledException"/>
        Task<T?> SingleOrDefaultAsync<T>(CancellationToken cancellationToken);

        Task<T?> SingleOrDefaultAsync<T>(T anonymousType) where T : class;

        /// <exception cref="OperationCanceledException"/>
        Task<T?> SingleOrDefaultAsync<T>(T anonymousType, CancellationToken cancellationToken) where T : class;

        Task<List<T>> ListAsync<T>();

        /// <exception cref="OperationCanceledException"/>
        Task<List<T>> ListAsync<T>(CancellationToken cancellationToken);

        Task<List<T>> ListAsync<T>(T anonymousType) where T : class;

        /// <exception cref="OperationCanceledException"/>
        Task<List<T>> ListAsync<T>(T anonymousType, CancellationToken cancellationToken) where T : class;

        Task<T[]> ArrayAsync<T>();

        /// <exception cref="OperationCanceledException"/>
        Task<T[]> ArrayAsync<T>(CancellationToken cancellationToken);

        Task<T[]> ArrayAsync<T>(T anonymousType) where T : class;

        /// <exception cref="OperationCanceledException"/>
        Task<T[]> ArrayAsync<T>(T anonymousType, CancellationToken cancellationToken) where T : class;

        Task<TCollection> CollectionAsync<TItem, TCollection>() where TCollection : ICollection<TItem>, new();

        /// <exception cref="OperationCanceledException"/>
        Task<TCollection> CollectionAsync<TItem, TCollection>(CancellationToken cancellationToken) where TCollection : ICollection<TItem>, new();

        IAsyncAnonymousReader<T> AsAnonymousAsync<T>(T anonymousType) where T : class;
    }
}
