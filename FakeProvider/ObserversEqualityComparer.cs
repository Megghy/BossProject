using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace FakeProvider
{
    public class ObserversEqualityComparer : IEqualityComparer<IEnumerable<TileProvider>>
    {
        public bool Equals(IEnumerable<TileProvider> b1, IEnumerable<TileProvider> b2) =>
            b1 == b2 || Enumerable.SequenceEqual(b1, b2);
        public int GetHashCode(IEnumerable<TileProvider> bx) => 0;
    }
}
