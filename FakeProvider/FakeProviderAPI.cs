using Terraria;

namespace FakeProvider
{
    public static class FakeProviderAPI
    {
        #region Data

        public const string WorldProviderName = "__world__";
        public static TileProviderCollection Tile { get; internal set; }
        public static TileProvider World { get; internal set; }
        private static ObserversEqualityComparer OEC = new();

        #endregion

        #region CreateTileProvider

        public static TileProvider CreateTileProvider(string Name, int X, int Y, int Width, int Height, int Layer = 0)
        {
            TileProvider result = new();
            result.Initialize(Name, X, Y, Width, Height, Layer);
            Tile.Add(result);
            return result;
        }

        public static TileProvider CreateTileProvider(string Name, int X, int Y, int Width, int Height, int Layer, ModFramework.ICollection<ITile> CopyFrom)
        {
            TileProvider result = new();
            result.Initialize(Name, X, Y, Width, Height, Layer, CopyFrom);
            Tile.Add(result);
            return result;
        }

        public static TileProvider CreateTileProvider(string Name, int X, int Y, int Width, int Height, int Layer, ITile[,] CopyFrom)
        {
            TileProvider result = new TileProvider();
            result.Initialize(Name, X, Y, Width, Height, Layer, CopyFrom);
            Tile.Add(result);
            return result;
        }

        #endregion
        #region CreatePersonalTileProvider

        public static TileProvider CreatePersonalTileProvider(string Name, HashSet<int> Players, int X, int Y, int Width, int Height, int Layer = 0)
        {
            TileProvider result = new TileProvider();
            result.Initialize(Name, X, Y, Width, Height, Layer, Players);
            Tile.Add(result);
            return result;
        }

        public static TileProvider CreatePersonalTileProvider(string Name, HashSet<int> Players, int X, int Y, int Width, int Height, int Layer, ModFramework.ICollection<ITile> CopyFrom)
        {
            TileProvider result = new TileProvider();
            result.Initialize(Name, X, Y, Width, Height, Layer, CopyFrom, Players);
            Tile.Add(result);
            return result;
        }

        public static TileProvider CreatePersonalTileProvider(string Name, HashSet<int> Players, int X, int Y, int Width, int Height, int Layer, ITile[,] CopyFrom)
        {
            TileProvider result = new TileProvider();
            result.Initialize(Name, X, Y, Width, Height, Layer, CopyFrom, Players);
            Tile.Add(result);
            return result;
        }

        #endregion
        #region SelectObservers

        public static HashSet<int> SelectObservers(int player = -1, int except = -1)
        {
            HashSet<int> result = new HashSet<int>();
            if (player >= 0)
                result.Add(player);
            else
                for (int i = 0; i < 255; i++)
                    result.Add(i);
            result.Remove(except);
            return result;
        }

        #endregion
        #region ApplyPersonal

        public static (ModFramework.ICollection<ITile> tiles, bool relative) ApplyPersonal(IEnumerable<TileProvider> Providers, int X, int Y, int Width, int Height)
        {
            if (Providers.Count() == 0)
                return (Tile, false);

            TileCollection result = new(new ITile[Width, Height]);

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    result[x, y] = Tile[X + x, Y + y];

            foreach (TileProvider provider in Providers)
                provider?.Apply(result, X, Y);

            return (result, true);
        }

        #endregion
        #region GroupBy

        // TODO: Optimize
        public static IEnumerable<IGrouping<IEnumerable<TileProvider>, RemoteClient>> GroupByPersonal(
                List<RemoteClient> Clients, int X, int Y, int Width, int Height)
        {
            IEnumerable<TileProvider> personal = Tile.CollidePersonal(X, Y, Width, Height);
            return Clients.GroupBy(client =>
                personal.Where(provider =>
                    provider.Observers.Contains(client.Id))
                , OEC);
        }

        #endregion
        #region GetProviderAt

        public static TileProvider GetProviderAt(int X, int Y) =>
            Tile.Providers[Tile.ProviderIndexes[X, Y]];

        #endregion
    }
}
