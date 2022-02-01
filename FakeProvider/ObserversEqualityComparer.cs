namespace FakeProvider
{
    public class ObserversEqualityComparer : IEqualityComparer<IEnumerable<TileProvider>>
    {
        public bool Equals(IEnumerable<TileProvider> b1, IEnumerable<TileProvider> b2) =>
            b1 == b2 || Enumerable.SequenceEqual(b1, b2);
        public int GetHashCode(IEnumerable<TileProvider> bx) => 0;
    }
}
