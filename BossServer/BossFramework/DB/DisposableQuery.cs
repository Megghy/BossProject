using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace BossFramework.DB
{
    public sealed class DisposableQuery<T> : IQueryable<T>, IDisposable
    {
        private readonly IQueryable<T> _query;
        private readonly IDisposable _disposable;

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        public DisposableQuery(IQueryable<T> query, IDisposable disposable)
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        {
            _query = query;
            _disposable = disposable;
        }
        public Expression Expression => _query.Expression;

        public Type ElementType => _query.ElementType;

        public IQueryProvider Provider => _query.Provider;

        Type IQueryable.ElementType { get; }

        public void Dispose()
        {
            _disposable.Dispose();
        }

        public IEnumerator<T> GetEnumerator() => _query.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _query.GetEnumerator();
    }
}
