#region Using
using Terraria;
#endregion
namespace FakeProvider
{
    public class TileProviderCollection : ModFramework.ICollection<ITile>, IDisposable
    {
        #region Data

        public const string VoidProviderName = "__void__";

        protected TileProvider[] _GlobalProvidersBuffer = new TileProvider[10];
        protected List<TileProvider> _GlobalProvidersOrder = new List<TileProvider>();

        protected List<TileProvider> _PersonalProviders = new List<TileProvider>();
        /// <summary>
        /// List of all non-personal providers
        /// </summary>
        public List<TileProvider> Global
        {
            get
            {
                lock (Locker)
                    return new List<TileProvider>(_GlobalProvidersOrder);
            }
        }
        /// <summary>
        /// List of all personal providers
        /// </summary>
        public List<TileProvider> Personal
        {
            get
            {
                lock (Locker)
                    return new List<TileProvider>(_PersonalProviders);
            }
        }
        /// <summary>
        /// List of all providers
        /// </summary>
        public IEnumerable<TileProvider> Providers => Global.Concat(Personal);
        /// <summary> <see cref="ProviderIndexes"/>[X, Y] is an index of provider at point (X, Y). </summary>
        public ushort[,] ProviderIndexes;
        /// <summary> World width visible by client. </summary>
        public int Width { get; protected set; }
        /// <summary> World height visible by client. </summary>
        public int Height { get; protected set; }
        /// <summary> Tile to be visible outside of all providers. </summary>
        protected object Locker { get; set; } = new object();
        internal protected TileProvider Void { get; set; }
        public ITile VoidTile { get; protected set; }

        #endregion

        #region Constructor

        public TileProviderCollection() : base() { }

        #endregion
        #region Initialize

        public void Initialize(int Width, int Height)
        {
            if (ProviderIndexes != null)
                throw new Exception("Attempt to reinitialize.");

            this.Width = Width;
            this.Height = Height;

            ProviderIndexes = new ushort[this.Width, this.Height];
            Void = FakeProviderAPI.CreateTileProvider(VoidProviderName, 0, 0, 1, 1, Int32.MinValue,
                new ITile[,] { { new Terraria.Tile() } });
            VoidTile = Void[0, 0];
        }

        #endregion

        #region operator[,]

        public ITile this[int X, int Y]
        {
            get
            {
                return _GlobalProvidersBuffer[ProviderIndexes[X, Y]][X, Y];
            }
            set
            {
                _GlobalProvidersBuffer[ProviderIndexes[X, Y]][X, Y] = value;
            }
        }

        #endregion
        #region GetTileSafe

        // Offset????
        public ITile GetTileSafe(int X, int Y) => X >= 0 && Y >= 0 && X < Width && Y < Height
            ? this[X, Y]
            : VoidTile;

        #endregion
        #region TileOnTopIndex

        public ushort TileOnTopIndex(int X, int Y) => ProviderIndexes[X, Y];

        #endregion
        #region GetProviderByIndex
        public TileProvider GetProviderByIndex(int index)
        {
            if (index < 0 || index >= _GlobalProvidersBuffer.Length)
                return null;
            return _GlobalProvidersBuffer[index];
        }
        #endregion

        #region Dispose

        public void Dispose()
        {
            lock (Locker)
            {
                foreach (TileProvider provider in _GlobalProvidersOrder)
                    provider.Dispose();
            }
        }

        #endregion

        #region operator[]

        public TileProvider this[string Name]
        {
            get
            {
                lock (Locker)
                    return _GlobalProvidersOrder.FirstOrDefault(p => (p.Name == Name));
            }
        }

        #endregion

        #region Add
        //TODO: Optimize
        internal void Add(TileProvider Provider)
        {
            lock (FakeProviderPlugin.ProvidersToAdd)
            {
                if (FakeProviderPlugin.ProvidersLoaded)
                {
                    lock (Locker)
                    {
                        if (Provider.Observers != null)
                            AddPersonal(Provider);
                        else
                        {
                            if (Providers.Any(p => (p.Name == Provider.Name)))
                                throw new ArgumentException($"Tile collection '{Provider.Name}' " +
                                    "is already in use. Name must be unique.");
                            PlaceProviderOnTopOfLayer(Provider);
                            int index = GetEmptyIndex();
                            _GlobalProvidersBuffer[index] = Provider;
                            Provider.ProviderCollection = this;
                            Provider.Index = index;
                            Provider.Enable(false);
                        }
                    }
                }
                else
                    FakeProviderPlugin.ProvidersToAdd.Add(Provider);
            }
        }

        #endregion
        #region AddPersonal

        internal void AddPersonal(TileProvider Provider)
        {
            lock (Locker)
            {
                if (Providers.Any(p => (p.Name == Provider.Name)))
                    throw new ArgumentException($"Tile collection '{Provider.Name}' " +
                        "is already in use. Name must be unique.");
                _PersonalProviders.Add(Provider);
                //int index = GetEmptyIndex();
                //_Providers[index] = Provider;
                Provider.ProviderCollection = this;
                //Provider.Index = index;
                Provider.Enable(false);
            }
        }

        #endregion
        #region Remove

        public bool Remove(TileProvider provider, bool Draw = true, bool Cleanup = false)
        {
            if (provider.Name == VoidProviderName)
                throw new InvalidOperationException("You cannot remove void provider.");

            bool contains = false;

            lock (Locker)
            {
                provider.Disable(Draw);
                if (provider.IsPersonal && (contains = _PersonalProviders.Contains(provider)))
                    _PersonalProviders.Remove(provider);
                else if (contains = _GlobalProvidersOrder.Contains(provider))
                {
                    _GlobalProvidersBuffer[provider.Index] = null;
                    _GlobalProvidersOrder.Remove(provider);
                }
            }

            if (Cleanup)
                GC.Collect();

            return contains;
        }

        #endregion
        #region SetTop

        public bool SetTop(string Name, bool Draw = true)
        {
            lock (Locker)
            {
                TileProvider provider = _GlobalProvidersOrder.FirstOrDefault(p => (p.Name == Name));
                if (provider == null)
                    return false;
                provider.SetTop(Draw);
                return true;
            }
        }

        #endregion
        #region CollidePersonal

        public IEnumerable<TileProvider> CollidePersonal(int X, int Y, int Width, int Height) =>
            _PersonalProviders.Where(provider => provider.Enabled && provider.HasCollision(X, Y, Width, Height));

        #endregion

        #region GetEmptyIndex

        private int GetEmptyIndex()
        {
            int index = 0;
            lock (Locker)
            {
                while (index < _GlobalProvidersBuffer.Length && _GlobalProvidersBuffer[index] != null)
                    index++;
                if (index >= _GlobalProvidersBuffer.Length)
                    Array.Resize(ref _GlobalProvidersBuffer, _GlobalProvidersBuffer.Length * 2);
            }
            return index;
        }

        #endregion
        #region PlaceProviderOnTopOfLayer

        internal void PlaceProviderOnTopOfLayer(TileProvider Provider)
        {
            lock (Locker)
            {
                _GlobalProvidersOrder.Remove(Provider);
                int order = _GlobalProvidersOrder.FindIndex(p => (p.Layer > Provider.Layer));
                if (order == -1)
                    order = _GlobalProvidersOrder.Count;
                _GlobalProvidersOrder.Insert(order, Provider);
                for (int i = order; i < _GlobalProvidersOrder.Count; i++)
                    _GlobalProvidersOrder[i].Order = i;
            }
        }

        #endregion
        #region Intersect

        internal static void Intersect(TileProvider Provider, int X, int Y, int Width, int Height,
            out int RX, out int RY, out int RWidth, out int RHeight)
        {
            int ex1 = Provider.X + Provider.Width;
            int ex2 = X + Width;
            int ey1 = Provider.Y + Provider.Height;
            int ey2 = Y + Height;
            int maxSX = (Provider.X > X) ? Provider.X : X;
            int maxSY = (Provider.Y > Y) ? Provider.Y : Y;
            int minEX = (ex1 < ex2) ? ex1 : ex2;
            int minEY = (ey1 < ey2) ? ey1 : ey2;
            RX = maxSX;
            RY = maxSY;
            RWidth = minEX - maxSX;
            RHeight = minEY - maxSY;
        }

        #endregion
        #region UpdateRectangleReferences

        public void UpdateRectangleReferences(int X, int Y, int Width, int Height, int RemoveIndex)
        {
            if (!FakeProviderPlugin.ProvidersLoaded)
                return;
            lock (Locker)
            {
                (X, Y, Width, Height) = Clamp(X, Y, Width, Height);

                Queue<TileProvider> providers = new Queue<TileProvider>();
                foreach (TileProvider provider in _GlobalProvidersOrder)
                    if (provider.Enabled)
                    {
                        ushort providerIndex = (ushort)provider.Index;
                        // Update tiles
                        Intersect(provider, X, Y, Width, Height, out int x, out int y,
                            out int width, out int height);
                        int dx = x - provider.X;
                        int dy = y - provider.Y;
                        for (int i = 0; i < width; i++)
                            for (int j = 0; j < height; j++)
                                ProviderIndexes[x + i, y + j] = providerIndex;

                        if (width > 0 && height > 0)
                            providers.Enqueue(provider);
                    }

                if (RemoveIndex >= 0)
                    for (int i = X; i < X + Width; i++)
                        for (int j = Y; j < Y + Height; j++)
                            if (ProviderIndexes[i, j] == RemoveIndex)
                                ProviderIndexes[i, j] = 0;

                // We are updating all the stuff only after tiles update since signs,
                // chests and entities apply only in case the tile on top is from this provider.
                while (providers.Count > 0)
                {
                    TileProvider provider = providers.Dequeue();
                    provider.UpdateEntities();
                }
            }
        }

        #endregion
        #region UpdateProviderReferences

        public void UpdateProviderReferences(TileProvider Provider)
        {
            if (!Provider.Enabled || !FakeProviderPlugin.ProvidersLoaded || Provider.Observers != null)
                return;
            lock (Locker)
            {
                // Scanning rectangle where this provider is/will appear.
                ScanRectangle(Provider.X, Provider.Y, Provider.Width, Provider.Height, Provider);

                // Update tiles
                ushort providerIndex = (ushort)Provider.Index;
                int order = Provider.Order;
                (int x, int y, int width, int height) = Provider.ClampXYWH();

                for (int i = 0; i < width; i++)
                    for (int j = 0; j < height; j++)
                    {
                        TileProvider provider = _GlobalProvidersBuffer[ProviderIndexes[x + i, y + j]];
                        if (provider[x + i, y + j] == null || provider.Order <= order || !provider.Enabled)
                            ProviderIndexes[x + i, y + j] = providerIndex;
                    }

                foreach (TileProvider provider in _GlobalProvidersOrder)
                {
                    Intersect(provider, x, y, width, height,
                        out int x2, out int y2, out int width2, out int height2);
                    if (width2 > 0 && height2 > 0)
                        provider.UpdateEntities();
                }
            }
        }

        #endregion
        #region HideEntities

        public void HideEntities()
        {
            lock (Locker)
                foreach (TileProvider provider in _GlobalProvidersOrder.Concat(_PersonalProviders))
                    if (provider.Name != FakeProviderAPI.WorldProviderName)
                        provider.HideEntities();
        }

        #endregion
        #region UpdateEntities

        public void UpdateEntities()
        {
            lock (Locker)
                foreach (TileProvider provider in _GlobalProvidersOrder.Concat(_PersonalProviders))
                    if (provider.Name != FakeProviderAPI.WorldProviderName)
                        provider.UpdateEntities();
        }

        #endregion
        #region ScanRectangle

        public void ScanRectangle(int X, int Y, int Width, int Height, TileProvider IgnoreProvider = null)
        {
            lock (Locker)
                foreach (TileProvider provider in _GlobalProvidersOrder.Concat(_PersonalProviders))
                    if (provider != IgnoreProvider)
                    {
                        Intersect(provider, X, Y, Width, Height, out int x, out int y, out int width, out int height);
                        if (width > 0 && height > 0)
                            provider.ScanEntities();
                    }
        }

        #endregion

        #region Clamp

        public (int x, int y, int width, int height) Clamp(int X, int Y, int Width, int Height) =>
            (Helper.Clamp(X, 0, this.Width),
            Helper.Clamp(Y, 0, this.Height),
            Helper.Clamp(Width, 0, this.Width - X),
            Helper.Clamp(Height, 0, this.Height - Y));

        #endregion
    }
}