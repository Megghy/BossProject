#region Using
using Terraria;
#endregion
namespace FakeProvider
{
    public class TileProviderCollection : ModFramework.ICollection<ITile>, IDisposable
    {
        #region Data

        public const string VoidProviderName = "__void__";
        private TileProvider[] _Providers = new TileProvider[10];
        private List<TileProvider> Order = new List<TileProvider>();
        // TODO: Personal order
        private List<TileProvider> Personal = new List<TileProvider>();

        /// <summary> List of all registered providers. </summary>
        public TileProvider[] Providers
        {
            get
            {
                lock (Locker)
                    return _Providers.ToArray();
            }
        }
        /// <summary> <see cref="ProviderIndexes"/>[X, Y] is an index of provider at point (X, Y). </summary>
        internal ushort[,] ProviderIndexes;
        /// <summary> World width visible by client. </summary>
        public int Width { get; protected set; }
        /// <summary> World height visible by client. </summary>
        public int Height { get; protected set; }
        /// <summary> Horizontal offset of the loaded world. </summary>

        // TODO: I completely messed up offset.
        public int OffsetX { get; internal protected set; }
        /// <summary> Vertical offset of the loaded world. </summary>
        public int OffsetY { get; internal protected set; }
        /// <summary> Tile to be visible outside of all providers. </summary>
        protected object Locker { get; set; } = new object();
        internal protected TileProvider Void { get; set; }
        public ITile VoidTile { get; protected set; }

        #endregion
        #region Constructor

        public TileProviderCollection() : base() { }

        #endregion

        #region Initialize

        public void Initialize(int Width, int Height, int OffsetX, int OffsetY)
        {
            if (ProviderIndexes != null)
                throw new Exception("Attempt to reinitialize.");

            this.Width = Width;
            this.Height = Height;
            this.OffsetX = OffsetX;
            this.OffsetY = OffsetY;

            ProviderIndexes = new ushort[this.Width, this.Height];
            Void = FakeProviderAPI.CreateTileProvider(VoidProviderName, 0, 0, 1, 1, Int32.MinValue,
                new ITile[,] { { new Terraria.Tile() } });
            VoidTile = Void[0, 0];
        }

        #endregion

        #region operator[,]

        public ITile this[int X, int Y]
        {
            get => _Providers[ProviderIndexes[X - OffsetX, Y - OffsetY]].GetIncapsulatedTile(X - OffsetX, Y - OffsetY);
            set => _Providers[ProviderIndexes[X - OffsetX, Y - OffsetY]].SetIncapsulatedTile(X - OffsetX, Y - OffsetY, value);
        }

        #endregion
        #region GetTileSafe

        // Offset????
        public ITile GetTileSafe(int X, int Y) => X >= 0 && Y >= 0 && X < Width && Y < Height
            ? this[X, Y]
            : VoidTile;

        #endregion

        #region Dispose

        public void Dispose()
        {
            lock (Locker)
            {
                foreach (TileProvider provider in Order)
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
                    return Order.FirstOrDefault(p => (p.Name == Name));
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
                            if (Order.Any(p => (p.Name == Provider.Name)))
                                throw new ArgumentException($"Tile collection '{Provider.Name}' " +
                                    "is already in use. Name must be unique.");
                            PlaceProviderOnTopOfLayer(Provider);
                            int index = GetEmptyIndex();
                            _Providers[index] = Provider;
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
                if (Order.Any(p => (p.Name == Provider.Name)))
                    throw new ArgumentException($"Tile collection '{Provider.Name}' " +
                        "is already in use. Name must be unique.");
                Personal.Add(Provider);
                //int index = GetEmptyIndex();
                //_Providers[index] = Provider;
                Provider.ProviderCollection = this;
                //Provider.Index = index;
                Provider.Enable(false);
            }
        }

        #endregion
        #region Remove

        public bool Remove(string Name, bool Draw = true, bool Cleanup = false)
        {
            if (Name == VoidProviderName)
                throw new InvalidOperationException("You cannot remove void provider.");

            lock (Locker)
            {
                if (Order.FirstOrDefault(p => (p.Name == Name)) is TileProvider provider)
                {
                    provider.Disable(Draw);
                    Order.Remove(provider);
                    _Providers[provider.Index] = null;
                }
                else if (Personal.FirstOrDefault(p => (p.Name == Name)) is TileProvider provider2)
                {
                    provider2.Disable(Draw);
                    Personal.Remove(provider2);
                }
            }
            if (Cleanup)
                GC.Collect();
            return true;
        }

        #endregion
        #region Clear

        public void Clear(TileProvider except = null)
        {
            lock (Locker)
                foreach (TileProvider provider in Order.ToArray())
                    if (provider != except)
                        Remove(provider.Name, true, false);
            GC.Collect();
        }

        #endregion
        #region SetTop

        public bool SetTop(string Name, bool Draw = true)
        {
            lock (Locker)
            {
                TileProvider provider = Order.FirstOrDefault(p => (p.Name == Name));
                if (provider == null)
                    return false;
                provider.SetTop(Draw);
                return true;
            }
        }

        #endregion
        #region CollidePersonal

        public IEnumerable<TileProvider> CollidePersonal(int X, int Y, int Width, int Height) =>
            Personal.Where(provider => provider.Enabled && provider.HasCollision(X, Y, Width, Height));

        #endregion

        #region GetEmptyIndex

        private int GetEmptyIndex()
        {
            int index = 0;
            while (index < _Providers.Length && _Providers[index] != null)
                index++;
            if (index >= _Providers.Length)
                Array.Resize(ref _Providers, _Providers.Length * 2);
            return index;
        }

        #endregion
        #region PlaceProviderOnTopOfLayer

        internal void PlaceProviderOnTopOfLayer(TileProvider Provider)
        {
            lock (Locker)
            {
                Order.Remove(Provider);
                int order = Order.FindIndex(p => (p.Layer > Provider.Layer));
                if (order == -1)
                    order = Order.Count;
                Order.Insert(order, Provider);
                for (int i = order; i < Order.Count; i++)
                    Order[i].Order = i;
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
                foreach (TileProvider provider in Order)
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
                        TileProvider provider = _Providers[ProviderIndexes[x + i, y + j]];
                        if (provider.GetTileSafe(x + i, y + j) == VoidTile || provider.Order <= order || !provider.Enabled)
                            ProviderIndexes[x + i, y + j] = providerIndex;
                    }

                foreach (TileProvider provider in Order)
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
                foreach (TileProvider provider in Order)
                    if (provider.Name != FakeProviderAPI.WorldProviderName)
                        provider.HideEntities();
        }

        #endregion
        #region UpdateEntities

        public void UpdateEntities()
        {
            lock (Locker)
                foreach (TileProvider provider in Order)
                    if (provider.Name != FakeProviderAPI.WorldProviderName)
                        provider.UpdateEntities();
        }

        #endregion
        #region ScanRectangle

        public void ScanRectangle(int X, int Y, int Width, int Height, TileProvider IgnoreProvider = null)
        {
            lock (Locker)
                foreach (TileProvider provider in Order)
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
