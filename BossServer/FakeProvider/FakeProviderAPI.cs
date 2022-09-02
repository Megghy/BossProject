using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TShockAPI;

namespace FakeProvider
{
    public static class FakeProviderAPI
    {
        #region Data

        public const string WorldProviderName = "__world__";
        public static TileProviderCollection Tile { get; internal set; }
        public static TileProvider World { get; internal set; }
        private static ObserversEqualityComparer OEC = new ObserversEqualityComparer();

        #endregion

        #region CreateTileProvider

        public static TileProvider CreateTileProvider(string Name, int X, int Y, int Width, int Height, int Layer = 0)
        {
            TileProvider result = new TileProvider();
            result.Initialize(Name, X, Y, Width, Height, Layer);
            Tile.Add(result);
            return result;
        }

        public static TileProvider CreateTileProvider(string Name, int X, int Y, int Width, int Height, int Layer, ModFramework.ICollection<ITile> CopyFrom)
        {
            TileProvider result = new TileProvider();
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

            TileCollection result = new TileCollection(new ITile[Width, Height]);

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    result[x, y] = Tile[X + x, Y + y];

            foreach (TileProvider provider in Providers)
                provider?.Apply(result, X, Y);

            return (result, true);
        }

        #endregion
        #region GroupByPersonal

        // TODO: Optimize
        public static IEnumerable<IGrouping<IEnumerable<TileProvider>, RemoteClient>> GroupByPersonal(
                List<RemoteClient> Clients, int X, int Y, int Width, int Height)
        {
            IEnumerable<TileProvider> personal = Tile.CollidePersonal(X, Y, Width, Height);
            return Clients.GroupBy(client =>
                personal.Where(provider => provider.Observers.Contains(client.Id)),
                OEC);
        }

        #endregion
        #region Remove

        public static bool Remove(TileProvider provider, bool Draw = true) =>
            Tile.Remove(provider, Draw);

        #endregion
        #region FindProvider

        public static IEnumerable<TileProvider> FindProvider(string name, bool includeGlobal = true, bool includePersonal = false)
        {
            if (!includeGlobal && !includePersonal)
                throw new ArgumentException("Choose which provider you want.", "includeGlobal & includePersonal");
            var providers = Tile.Providers;

            IEnumerable<TileProvider> _providers = providers.Where(p => p.Name == name);
            if (_providers.Count() > 0)
                return _providers;
            return providers.Where(p => p.Name.ToLower().StartsWith(name.ToLower()));
        }

        #endregion
    }
}