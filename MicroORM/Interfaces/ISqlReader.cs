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
        IAsyncAnonymousReader<T> AsAnonymousAsync<T>(T anonymousType) where T : class;


        /// <exception cref="MicroOrmException"/>
        int Execute();

        /// <exception cref="MicroOrmException"/>
        DataTable Table();
        
        /// <exception cref="MicroOrmException"/>
        object? Scalar();
        
        /// <exception cref="MicroOrmException"/>
        T Scalar<T>();
        
        /// <exception cref="MicroOrmException"/>
        object?[] ScalarArray();
        
        /// <exception cref="MicroOrmException"/>
        List<object?> ScalarList();
        
        /// <exception cref="MicroOrmException"/>
        T[] ScalarArray<T>();
        
        /// <exception cref="MicroOrmException"/>
        List<T> ScalarList<T>();
        
        /// <exception cref="MicroOrmException"/>
        T? ScalarOrDefault<T>();
        
        /// <exception cref="MicroOrmException"/>
        T Single<T>();
        
        /// <exception cref="MicroOrmException"/>
        T Single<T>(T anonymousType) where T : class;
        
        /// <exception cref="MicroOrmException"/>
        T? SingleOrDefault<T>();
        
        /// <exception cref="MicroOrmException"/>
        T? SingleOrDefault<T>(T anonymousType) where T : class;
        
        /// <exception cref="MicroOrmException"/>
        List<T> ToList<T>();
        
        /// <exception cref="MicroOrmException"/>
        List<T> ToList<T>(T anonymousType) where T : class;
        
        /// <exception cref="MicroOrmException"/>
        T[] ToArray<T>();
        
        /// <exception cref="MicroOrmException"/>
        T[] ToArray<T>(T anonymousType) where T : class;
        
        /// <exception cref="MicroOrmException"/>
        TCollection ToCollection<TItem, TCollection>() where TCollection : ICollection<TItem>, new();



        /// <exception cref="MicroOrmException"/>
        Task<int> ExecuteAsync();

        /// <exception cref="MicroOrmException"/>
        /// <exception cref="OperationCanceledException"/>
        Task<int> ExecuteAsync(CancellationToken cancellationToken);

        /// <exception cref="MicroOrmException"/>
        Task<DataTable> TableAsync();

        /// <exception cref="MicroOrmException"/>
        /// <exception cref="OperationCanceledException"/>
        Task<DataTable> TableAsync(CancellationToken cancellationToken);

        /// <exception cref="MicroOrmException"/>
        Task<object?> ScalarAsync();

        /// <exception cref="MicroOrmException"/>
        /// <exception cref="OperationCanceledException"/>
        Task<object?> ScalarAsync(CancellationToken cancellationToken);

        /// <exception cref="MicroOrmException"/>
        Task<object?[]> ScalarArrayAsync();

        /// <exception cref="MicroOrmException"/>
        /// <exception cref="OperationCanceledException"/>
        Task<object?[]> ScalarArrayAsync(CancellationToken cancellationToken);

        /// <exception cref="MicroOrmException"/>
        Task<List<object?>> ScalarListAsync();

        /// <exception cref="MicroOrmException"/>
        /// <exception cref="OperationCanceledException"/>
        Task<List<object?>> ScalarListAsync(CancellationToken cancellationToken);

        /// <exception cref="MicroOrmException"/>
        Task<T[]> ScalarArrayAsync<T>();

        /// <exception cref="MicroOrmException"/>
        /// <exception cref="OperationCanceledException"/>
        Task<T[]> ScalarArrayAsync<T>(CancellationToken cancellationToken);

        /// <exception cref="MicroOrmException"/>
        Task<List<T>> ScalarListAsync<T>();

        /// <exception cref="MicroOrmException"/>
        /// <exception cref="OperationCanceledException"/>
        Task<List<T>> ScalarListAsync<T>(CancellationToken cancellationToken);

        /// <exception cref="MicroOrmException"/>
        Task<T> ScalarAsync<T>();

        /// <exception cref="MicroOrmException"/>
        /// <exception cref="OperationCanceledException"/>
        Task<T> ScalarAsync<T>(CancellationToken cancellationToken);

        /// <exception cref="MicroOrmException"/>
        Task<T?> ScalarOrDefaultAsync<T>();

        /// <exception cref="MicroOrmException"/>
        /// <exception cref="OperationCanceledException"/>
        Task<T?> ScalarOrDefaultAsync<T>(CancellationToken cancellationToken);

        /// <exception cref="MicroOrmException"/>
        Task<T> SingleAsync<T>();

        /// <exception cref="MicroOrmException"/>
        /// <exception cref="OperationCanceledException"/>
        Task<T> SingleAsync<T>(CancellationToken cancellationToken);

        /// <exception cref="MicroOrmException"/>
        Task<T> SingleAsync<T>(T anonymousType) where T : class;

        /// <exception cref="MicroOrmException"/>
        /// <exception cref="OperationCanceledException"/>
        Task<T> SingleAsync<T>(T anonymousType, CancellationToken cancellationToken) where T : class;

        /// <exception cref="MicroOrmException"/>
        Task<T?> SingleOrDefaultAsync<T>();

        /// <exception cref="MicroOrmException"/>
        /// <exception cref="OperationCanceledException"/>
        Task<T?> SingleOrDefaultAsync<T>(CancellationToken cancellationToken);

        /// <exception cref="MicroOrmException"/>
        Task<T?> SingleOrDefaultAsync<T>(T anonymousType) where T : class;

        /// <exception cref="MicroOrmException"/>
        /// <exception cref="OperationCanceledException"/>
        Task<T?> SingleOrDefaultAsync<T>(T anonymousType, CancellationToken cancellationToken) where T : class;

        /// <exception cref="MicroOrmException"/>
        Task<List<T>> ToListAsync<T>();

        /// <exception cref="MicroOrmException"/>
        /// <exception cref="OperationCanceledException"/>
        Task<List<T>> ToListAsync<T>(CancellationToken cancellationToken);

        /// <exception cref="MicroOrmException"/>
        Task<List<T>> ToListAsync<T>(T anonymousType) where T : class;

        /// <exception cref="MicroOrmException"/>
        /// <exception cref="OperationCanceledException"/>
        Task<List<T>> ToListAsync<T>(T anonymousType, CancellationToken cancellationToken) where T : class;

        /// <exception cref="MicroOrmException"/>
        Task<T[]> ToArrayAsync<T>();

        /// <exception cref="MicroOrmException"/>
        /// <exception cref="OperationCanceledException"/>
        Task<T[]> ToArrayAsync<T>(CancellationToken cancellationToken);

        /// <exception cref="MicroOrmException"/>
        Task<T[]> ToArrayAsync<T>(T anonymousType) where T : class;

        /// <exception cref="MicroOrmException"/>
        /// <exception cref="OperationCanceledException"/>
        Task<T[]> ToArrayAsync<T>(T anonymousType, CancellationToken cancellationToken) where T : class;

        /// <exception cref="MicroOrmException"/>
        Task<TCollection> ToCollectionAsync<TItem, TCollection>() where TCollection : ICollection<TItem>, new();

        /// <exception cref="MicroOrmException"/>
        /// <exception cref="OperationCanceledException"/>
        Task<TCollection> ToCollectionAsync<TItem, TCollection>(CancellationToken cancellationToken) where TCollection : ICollection<TItem>, new();
    }
}
