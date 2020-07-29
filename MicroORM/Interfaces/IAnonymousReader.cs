﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM
{
    public interface IAnonymousReader<T> where T : class
    {
        List<TResult> List<TResult>(Func<T, TResult> selector);
        TResult[] Array<TResult>(Func<T, TResult> selector);
    }

    public interface IAsyncAnonymousReader<T> where T : class
    {
        Task<List<TResult>> List<TResult>(Func<T, TResult> selector);
        Task<List<TResult>> List<TResult>(Func<T, TResult> selector, CancellationToken cancellationToken);

        Task<TResult[]> Array<TResult>(Func<T, TResult> selector);
        Task<TResult[]> Array<TResult>(Func<T, TResult> selector, CancellationToken cancellationToken);
    }
}
