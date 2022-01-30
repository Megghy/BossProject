using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace BossPlugin.DB
{
    public sealed class DisposableQuery<T> : IQueryable<T>, IDisposable
    {
        private readonly IQueryable<T> _query;
        private readonly IDisposable _disposable;

        public DisposableQuery(IQueryable<T> query, IDisposable disposable)
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
