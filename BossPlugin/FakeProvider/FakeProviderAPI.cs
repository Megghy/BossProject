using Terraria;

namespace FakeProvider
{
    /// <summary>
    /// FakeProvider 的公共 API, 用于创建和管理物块提供者。
    /// </summary>
    public static class FakeProviderAPI
    {
        #region Data

        /// <summary>
        /// 世界物块提供者的保留名称。
        /// </summary>
        public const string WorldProviderName = "__world__";
        /// <summary>
        /// 所有物块提供者的集合。
        /// </summary>
        public static TileProviderCollection Tile { get; internal set; }
        /// <summary>
        /// 代表整个游戏世界的物块提供者。
        /// </summary>
        public static TileProvider World { get; internal set; }
        private static ObserversEqualityComparer OEC = new ObserversEqualityComparer();

        #endregion

        #region CreateTileProvider

        /// <summary>
        /// 创建一个对所有玩家可见的物块提供者。
        /// </summary>
        /// <param name="Name">提供者名称</param>
        /// <param name="X">区域起始X坐标</param>
        /// <param name="Y">区域起始Y坐标</param>
        /// <param name="Width">区域宽度</param>
        /// <param name="Height">区域高度</param>
        /// <param name="Layer">图层</param>
        /// <returns>创建的物块提供者实例</returns>
        public static TileProvider CreateTileProvider(string Name, int X, int Y, int Width, int Height, int Layer = 0)
        {
            TileProvider result = new TileProvider();
            result.Initialize(Name, X, Y, Width, Height, Layer);
            Tile.Add(result);
            return result;
        }

        /// <summary>
        /// 从现有集合复制数据，创建一个对所有玩家可见的物块提供者。
        /// </summary>
        /// <param name="Name">提供者名称</param>
        /// <param name="X">区域起始X坐标</param>
        /// <param name="Y">区域起始Y坐标</param>
        /// <param name="Width">区域宽度</param>
        /// <param name="Height">区域高度</param>
        /// <param name="Layer">图层</param>
        /// <param name="CopyFrom">要复制的物块集合</param>
        /// <returns>创建的物块提供者实例</returns>
        public static TileProvider CreateTileProvider(string Name, int X, int Y, int Width, int Height, int Layer, ModFramework.ICollection<ITile> CopyFrom)
        {
            TileProvider result = new TileProvider();
            result.Initialize(Name, X, Y, Width, Height, Layer, CopyFrom);
            Tile.Add(result);
            return result;
        }

        /// <summary>
        /// 从现有二维数组复制数据，创建一个对所有玩家可见的物块提供者。
        /// </summary>
        /// <param name="Name">提供者名称</param>
        /// <param name="X">区域起始X坐标</param>
        /// <param name="Y">区域起始Y坐标</param>
        /// <param name="Width">区域宽度</param>
        /// <param name="Height">区域高度</param>
        /// <param name="Layer">图层</param>
        /// <param name="CopyFrom">要复制的物块二维数组</param>
        /// <returns>创建的物块提供者实例</returns>
        public static TileProvider CreateTileProvider(string Name, int X, int Y, int Width, int Height, int Layer, ITile[,] CopyFrom)
        {
            TileProvider result = new TileProvider();
            result.Initialize(Name, X, Y, Width, Height, Layer, CopyFrom);
            Tile.Add(result);
            return result;
        }

        #endregion
        #region CreatePersonalTileProvider

        /// <summary>
        /// 创建一个仅对指定玩家可见的物块提供者。
        /// </summary>
        /// <param name="Name">提供者名称</param>
        /// <param name="Players">可见的玩家ID集合</param>
        /// <param name="X">区域起始X坐标</param>
        /// <param name="Y">区域起始Y坐标</param>
        /// <param name="Width">区域宽度</param>
        /// <param name="Height">区域高度</param>
        /// <param name="Layer">图层</param>
        /// <returns>创建的物块提供者实例</returns>
        public static TileProvider CreatePersonalTileProvider(string Name, HashSet<int> Players, int X, int Y, int Width, int Height, int Layer = 0)
        {
            TileProvider result = new TileProvider();
            result.Initialize(Name, X, Y, Width, Height, Layer, Players);
            Tile.Add(result);
            return result;
        }

        /// <summary>
        /// 从现有集合复制数据，创建一个仅对指定玩家可见的物块提供者。
        /// </summary>
        /// <param name="Name">提供者名称</param>
        /// <param name="Players">可见的玩家ID集合</param>
        /// <param name="X">区域起始X坐标</param>
        /// <param name="Y">区域起始Y坐标</param>
        /// <param name="Width">区域宽度</param>
        /// <param name="Height">区域高度</param>
        /// <param name="Layer">图层</param>
        /// <param name="CopyFrom">要复制的物块集合</param>
        /// <returns>创建的物块提供者实例</returns>
        public static TileProvider CreatePersonalTileProvider(string Name, HashSet<int> Players, int X, int Y, int Width, int Height, int Layer, ModFramework.ICollection<ITile> CopyFrom)
        {
            TileProvider result = new TileProvider();
            result.Initialize(Name, X, Y, Width, Height, Layer, CopyFrom, Players);
            Tile.Add(result);
            return result;
        }

        /// <summary>
        /// 从现有二维数组复制数据，创建一个仅对指定玩家可见的物块提供者。
        /// </summary>
        /// <param name="Name">提供者名称</param>
        /// <param name="Players">可见的玩家ID集合</param>
        /// <param name="X">区域起始X坐标</param>
        /// <param name="Y">区域起始Y坐标</param>
        /// <param name="Width">区域宽度</param>
        /// <param name="Height">区域高度</param>
        /// <param name="Layer">图层</param>
        /// <param name="CopyFrom">要复制的物块二维数组</param>
        /// <returns>创建的物块提供者实例</returns>
        public static TileProvider CreatePersonalTileProvider(string Name, HashSet<int> Players, int X, int Y, int Width, int Height, int Layer, ITile[,] CopyFrom)
        {
            TileProvider result = new TileProvider();
            result.Initialize(Name, X, Y, Width, Height, Layer, CopyFrom, Players);
            Tile.Add(result);
            return result;
        }

        #endregion
        #region SelectObservers

        /// <summary>
        /// 一个工具方法，用于快速选择观察者(玩家)。
        /// </summary>
        /// <param name="player">要选择的单个玩家ID，-1代表所有玩家。</param>
        /// <param name="except">要排除的玩家ID。</param>
        /// <returns>玩家ID集合。</returns>
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

        /// <summary>
        /// 将指定的个人物块提供者应用到物块数据上，生成一个临时的物块视图。
        /// </summary>
        /// <param name="Providers">要应用的提供者集合。</param>
        /// <param name="X">目标区域X坐标。</param>
        /// <param name="Y">目标区域Y坐标。</param>
        /// <param name="Width">目标区域宽度。</param>
        /// <param name="Height">目标区域高度。</param>
        /// <returns>一个元组，包含应用了提供者效果的物块集合以及一个布尔值，指示是否为相对坐标。</returns>
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

        // TODO: 优化
        /// <summary>
        /// 根据客户端可见的个人物块提供者对客户端进行分组。
        /// </summary>
        /// <param name="Clients">要分组的客户端列表。</param>
        /// <param name="X">目标区域X坐标。</param>
        /// <param name="Y">目标区域Y坐标。</param>
        /// <param name="Width">目标区域宽度。</param>
        /// <param name="Height">目标区域高度。</param>
        /// <returns>按可见提供者分组的客户端枚举。</returns>
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

        /// <summary>
        /// 移除一个物块提供者。
        /// </summary>
        /// <param name="provider">要移除的提供者。</param>
        /// <param name="Draw">是否在移除后重绘相关区域。</param>
        /// <returns>如果成功移除，返回 true；否则返回 false。</returns>
        public static bool Remove(TileProvider provider, bool Draw = true) =>
            Tile.Remove(provider, Draw);

        #endregion
        #region FindProvider

        /// <summary>
        /// 根据名称查找物块提供者。
        /// </summary>
        /// <param name="name">要查找的名称。</param>
        /// <param name="includeGlobal">是否包含全局提供者。</param>
        /// <param name="includePersonal">是否包含个人提供者。</param>
        /// <returns>找到的物块提供者枚举。</returns>
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