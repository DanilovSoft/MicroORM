using System;
using System.Collections.Generic;

namespace DanilovSoft.MicroORM;

public interface IAnonymousReader<T> where T : class
{
    List<TResult> List<TResult>(Func<T, TResult> selector);
    TResult[] Array<TResult>(Func<T, TResult> selector);
}
