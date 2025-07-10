#region Using
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.DataStructures;
#endregion
namespace FakeProvider
{
    internal sealed class TileReference : Tile
    {
        //
        // 摘要:
        //     Size of the tile data in bytes
        public const int TileSize = 14;

        internal unsafe readonly StructTile* _tile;

        public unsafe override ushort type
        {
            get
            {
                return _tile->type;
            }
            set
            {
                _tile->type = value;
            }
        }

        public unsafe override ushort wall
        {
            get
            {
                return _tile->wall;
            }
            set
            {
                _tile->wall = value;
            }
        }

        public unsafe override byte liquid
        {
            get
            {
                return _tile->liquid;
            }
            set
            {
                _tile->liquid = value;
            }
        }

        public unsafe override ushort sTileHeader
        {
            get
            {
                return _tile->sTileHeader;
            }
            set
            {
                _tile->sTileHeader = value;
            }
        }

        public unsafe override byte bTileHeader
        {
            get
            {
                return _tile->bTileHeader;
            }
            set
            {
                _tile->bTileHeader = value;
            }
        }

        public unsafe override byte bTileHeader2
        {
            get
            {
                return _tile->bTileHeader2;
            }
            set
            {
                _tile->bTileHeader2 = value;
            }
        }

        public unsafe override byte bTileHeader3
        {
            get
            {
                return _tile->bTileHeader3;
            }
            set
            {
                _tile->bTileHeader3 = value;
            }
        }

        public unsafe override short frameX
        {
            get
            {
                return _tile->frameX;
            }
            set
            {
                _tile->frameX = value;
            }
        }

        public unsafe override short frameY
        {
            get
            {
                return _tile->frameY;
            }
            set
            {
                _tile->frameY = value;
            }
        }

        public override void Initialise()
        {
        }

        //
        // 摘要:
        //     Creates a new reference to the packed memory data
        //
        // 参数:
        //   tile:
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe TileReference(ref StructTile tile)
        {
            _tile = (StructTile*)Unsafe.AsPointer(ref tile);
        }

        //
        // 摘要:
        //     Clears the tile data, using a faster method than resetting each variable (which
        //     creates more instructions than required with this setup)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe override void ClearEverything()
        {
            Unsafe.InitBlockUnaligned(_tile, 0, 14u);
        }
    }
    public sealed class TileProvider : ModFramework.ICollection<ITile>, IDisposable
    {
        #region Data

        public TileProviderCollection ProviderCollection { get; internal set; }
        internal StructTile[,] Data;
        private TileReference[,] _tileReferenceCache;
        internal readonly EntityManager _entityManager;
        public EntityManager EntityManager => _entityManager;
        public string Name { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Index { get; internal set; } = -1;
        public int Order { get; internal set; } = -1;
        public int Layer { get; private set; }
        public bool Enabled { get; private set; } = false;
        public HashSet<int> Observers { get; private set; }
        public bool IsPersonal => Observers != null;
        public ReadOnlyCollection<IFake> Entities => _entityManager.Entities;

        public int GetIndex(int x, int y) => x * (Height - 1) + y;

        #endregion

        #region Constructor

        internal TileProvider()
        {
            _entityManager = new(this);
        }

        #endregion
        #region Initialize

        internal void Initialize(string Name, int X, int Y, int Width, int Height, int Layer, HashSet<int> Observers = null)
        {
            Init(Name, X, Y, Width, Height, Layer, Observers);
        }

        internal void Initialize(string Name, int X, int Y, int Width, int Height, int Layer, ModFramework.ICollection<ITile> CopyFrom, HashSet<int> Observers = null)
        {
            Init(Name, X, Y, Width, Height, Layer, Observers);

            for (int i = 0; i < Width; i++)
                for (int j = 0; j < Height; j++)
                {
                    ITile t = CopyFrom[i, j];
                    if (t != null)
                        this[i, j].CopyFrom(t);
                }
        }

        internal void Initialize(string Name, int X, int Y, int Width, int Height, int Layer, ITile[,] CopyFrom, HashSet<int> Observers = null)
        {
            Init(Name, X, Y, Width, Height, Layer, Observers);

            for (int i = 0; i < Width; i++)
                for (int j = 0; j < Height; j++)
                {
                    ITile t = CopyFrom[i, j];
                    if (t != null)
                        this[i, j].CopyFrom(t);
                }
        }
        internal void Init(string Name, int X, int Y, int Width, int Height, int Layer, HashSet<int> Observers = null)
        {
            if (Width <= 0 || Height <= 0) throw new ArgumentException("Invalid width or height.");
            this.Name = Name;
            this.Data = new StructTile[Width, Height];
            this._tileReferenceCache = new TileReference[Width, Height];
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
            this.Layer = Layer;
            this.Observers = Observers;
        }

        #endregion

        #region operator[,]

        unsafe ITile ModFramework.ICollection<ITile>.this[int x, int y]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                //data ??= new TileData[Width * Height];
                return new TileReference(ref Data[x, y]);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (value is TileReference tref) // if it's one of our own, copy memory instead of newing up a new reference.
                    Data[x, y] = *tref._tile;
                else new TileReference(ref Data[x, y]).CopyFrom(value);
            }
        }

        public unsafe ITile this[int x, int y]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (x < 0 || y < 0 || x >= Width || y >= Height) return ProviderCollection.VoidTile;

                var tileRef = _tileReferenceCache[x, y];
                if (tileRef == null)
                {
                    tileRef = new TileReference(ref Data[x, y]);
                    _tileReferenceCache[x, y] = tileRef;
                }
                return tileRef;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (value is TileReference tref) // if it's one of our own, copy memory instead of newing up a new reference.
                    Data[x, y] = *tref._tile;
                else new TileReference(ref Data[x, y]).CopyFrom(value);
            }
        }

        #endregion
        #region GetTileSafe

        public ITile GetTileSafe(int X, int Y) => X >= 0 && Y >= 0 && X < Width && Y < Height
            ? this[X, Y]
            : ProviderCollection.VoidTile;

        #endregion

        #region XYWH

        public (int X, int Y, int Width, int Height) XYWH(int DeltaX = 0, int DeltaY = 0) =>
            (X + DeltaX, Y + DeltaY, Width, Height);

        #endregion
        #region ClampXYWH

        public (int X, int Y, int Width, int Height) ClampXYWH() =>
            (ProviderCollection.Clamp(X, Y, Width, Height));

        #endregion
        #region SetXYWH

        public void SetXYWH(int X, int Y, int Width, int Height, bool Draw = true)
        {
            bool wasEnabled = Enabled;
            if (wasEnabled)
                Disable(Draw);

            this.X = X;
            this.Y = Y;
            if ((this.Width != Width) || (this.Height != Height))
            {
                if (Width <= 0 || Height <= 0)
                    throw new ArgumentException("Invalid new width or height.");

                StructTile[,] newData = new StructTile[Width, Height];
                for (int i = 0; i < Width; i++)
                    for (int j = 0; j < Height; j++)
                        if ((i < this.Width) && (j < this.Height))
                            newData[i, j] = Data[i, j];
                this.Data = newData;
                this._tileReferenceCache = new TileReference[Width, Height];
                this.Width = Width;
                this.Height = Height;
            }

            if (wasEnabled)
                Enable(Draw);
        }

        #endregion
        #region Move

        public void Move(int DeltaX, int DeltaY, bool Draw = true) =>
            SetXYWH(this.X + DeltaX, this.Y + DeltaY, this.Width, this.Height, Draw);

        #endregion
        #region Resize

        public void Resize(int Width, int Height, bool Draw = true) =>
            SetXYWH(this.X, this.Y, Width, Height, Draw);

        #endregion
        #region Enable

        public void Enable(bool Draw = true)
        {
            if (!Enabled)
            {
                Enabled = true;
                ProviderCollection?.UpdateProviderReferences(this);
                if (Draw)
                    this.Draw(true);
            }
        }

        #endregion
        #region Disable

        public void Disable(bool Draw = true)
        {
            if (Enabled)
            {
                Enabled = false;
                if (ProviderCollection != null)
                {
                    // Adding/removing manually added/removed signs, chests and entities
                    ScanEntities();
                    // Remove signs, chests, entities
                    HideEntities();
                    // Showing tiles, signs, chests and entities under the provider
                    if (Observers == null)
                        ProviderCollection.UpdateRectangleReferences(X, Y, Width, Height, Index);
                }
                if (Draw)
                    this.Draw(true);
            }
        }

        #endregion
        #region SetTop

        public void SetTop(bool Draw = true)
        {
            ProviderCollection.PlaceProviderOnTopOfLayer(this);
            ProviderCollection.UpdateProviderReferences(this);
            if (Draw)
                this.Draw();
        }

        #endregion
        #region SetLayer

        public void SetLayer(int Layer, bool Draw = true)
        {
            if (Observers != null)
                return;
            int oldLayer = this.Layer;
            if (Layer != oldLayer)
            {
                this.Layer = Layer;
                ProviderCollection.PlaceProviderOnTopOfLayer(this);
                if (Layer > oldLayer)
                    ProviderCollection.UpdateProviderReferences(this);
                else
                    ProviderCollection.UpdateRectangleReferences(X, Y, Width, Height, -1);
                if (Draw)
                    this.Draw();
            }
        }

        #endregion
        #region Update

        public void Update()
        {
        }

        #endregion
        #region Clear

        public void Clear()
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    this[x, y].ClearEverything();

            ClearEntities();
        }

        #endregion
        #region ClearEntities

        public void ClearEntities()
        {
            _entityManager.Clear();
        }

        #endregion
        #region CopyFrom

        public void CopyFrom(TileProvider provider)
        {
            Clear();
            SetXYWH(provider.X, provider.Y, provider.Width, provider.Height);
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    this[x, y] = provider[x, y];

            _entityManager.CopyFrom(provider._entityManager);
        }

        #endregion
        #region HasCollision

        public bool HasCollision(int X, int Y, int Width, int Height) =>
            (X < (this.X + this.Width)) && (this.X < (X + Width))
                && (Y < (this.Y + this.Height)) && (this.Y < (Y + Height));

        #endregion
        #region Intersect

        private void Intersect(int X, int Y, int Width, int Height,
            out int RX, out int RY, out int RWidth, out int RHeight)
        {
            int ex1 = this.X + this.Width;
            int ex2 = X + Width;
            int ey1 = this.Y + this.Height;
            int ey2 = Y + Height;
            int maxSX = (this.X > X) ? this.X : X;
            int maxSY = (this.Y > Y) ? this.Y : Y;
            int minEX = (ex1 < ex2) ? ex1 : ex2;
            int minEY = (ey1 < ey2) ? ey1 : ey2;
            RX = maxSX;
            RY = maxSY;
            RWidth = minEX - maxSX;
            RHeight = minEY - maxSY;
        }

        #endregion
        #region Apply

        public void Apply(ModFramework.ICollection<ITile> Tiles, int X, int Y)
        {
            Intersect(X, Y, Tiles.Width, Tiles.Height,
                out int x1, out int y1, out int w, out int h);
            int x2 = x1 + w;
            int y2 = y1 + h;

            for (int i = x1; i < x2; i++)
                for (int j = y1; j < y2; j++)
                {
                    ITile tile = this[i - this.X, j - this.Y];
                    if (tile != null)
                        Tiles[i - X, j - Y] = tile;
                }
        }

        #endregion

        #region AddSign

        public FakeSign AddSign(int X, int Y, string Text) => _entityManager.AddSign(X, Y, Text);

        #endregion
        #region AddChest

        public FakeChest AddChest(int X, int Y, Item[] Items = null) => _entityManager.AddChest(X, Y, Items);

        #endregion
        #region AddTrainingDummy

        public FakeTrainingDummy AddTrainingDummy(int X, int Y) => _entityManager.AddTrainingDummy(X, Y);

        #endregion
        #region AddItemFrame

        public FakeItemFrame AddItemFrame(int X, int Y, Item Item = null) => _entityManager.AddItemFrame(X, Y, Item);

        #endregion
        #region AddLogicSensor

        public FakeLogicSensor AddLogicSensor(int X, int Y, Terraria.GameContent.Tile_Entities.TELogicSensor.LogicCheckType LogicCheckType) => _entityManager.AddLogicSensor(X, Y, LogicCheckType);

        #endregion
        #region AddDisplayDoll

        public FakeDisplayDoll AddDisplayDoll(int X, int Y, Item[] Items = null, Item[] Dyes = null) => _entityManager.AddDisplayDoll(X, Y, Items, Dyes);

        #endregion
        #region AddFoodPlatter

        public FakeFoodPlatter AddFoodPlatter(int X, int Y, Item Item = null) => _entityManager.AddFoodPlatter(X, Y, Item);

        #endregion
        #region AddHatRack

        public FakeHatRack AddHatRack(int X, int Y, Item[] Items = null, Item[] Dyes = null) => _entityManager.AddHatRack(X, Y, Items, Dyes);

        #endregion
        #region AddTeleportationPylon

        public FakeTeleportationPylon AddTeleportationPylon(int X, int Y) => _entityManager.AddTeleportationPylon(X, Y);

        #endregion
        #region AddWeaponsRack

        public FakeWeaponsRack AddWeaponsRack(int X, int Y, Item Item = null) => _entityManager.AddWeaponsRack(X, Y, Item);

        #endregion
        #region AddEntity

        public IFake AddEntity(IFake Entity) => _entityManager.AddEntity(Entity);

        public FakeSign AddEntity(Sign Entity, bool replace = false) => _entityManager.AddEntity(Entity, replace);

        public FakeChest AddEntity(Chest Entity, bool replace = false) => _entityManager.AddEntity(Entity, replace);

        public IFake AddEntity(TileEntity Entity, bool replace = false) => _entityManager.AddEntity(Entity, replace);

        public FakeTrainingDummy AddEntity(Terraria.GameContent.Tile_Entities.TETrainingDummy Entity, bool replace = false) => _entityManager.AddEntity(Entity, replace);

        public FakeItemFrame AddEntity(Terraria.GameContent.Tile_Entities.TEItemFrame Entity, bool replace = false) => _entityManager.AddEntity(Entity, replace);

        public FakeLogicSensor AddEntity(Terraria.GameContent.Tile_Entities.TELogicSensor Entity, bool replace = false) => _entityManager.AddEntity(Entity, replace);

        public FakeDisplayDoll AddEntity(Terraria.GameContent.Tile_Entities.TEDisplayDoll Entity, bool replace = false) => _entityManager.AddEntity(Entity, replace);

        public FakeFoodPlatter AddEntity(Terraria.GameContent.Tile_Entities.TEFoodPlatter Entity, bool replace = false) => _entityManager.AddEntity(Entity, replace);

        public FakeHatRack AddEntity(Terraria.GameContent.Tile_Entities.TEHatRack Entity, bool replace = false) => _entityManager.AddEntity(Entity, replace);

        public FakeTeleportationPylon AddEntity(Terraria.GameContent.Tile_Entities.TETeleportationPylon Entity, bool replace = false) => _entityManager.AddEntity(Entity, replace);

        public FakeWeaponsRack AddEntity(Terraria.GameContent.Tile_Entities.TEWeaponsRack Entity, bool replace = false) => _entityManager.AddEntity(Entity, replace);

        #endregion
        #region RemoveEntity

        public void RemoveEntity(IFake Entity) => _entityManager.RemoveEntity(Entity);

        #endregion
        #region UpdateEntities

        public void UpdateEntities() => _entityManager.UpdateEntities();

        #endregion
        #region HideEntities

        public void HideEntities() => _entityManager.HideEntities();

        #endregion
        #region ScanEntities

        public void ScanEntities() => _entityManager.ScanEntities();

        #endregion

        #region Draw

        public void Draw(bool Section = true)
        {
            Terraria.Localization.NetworkText playerList = Observers != null
                ? Terraria.Localization.NetworkText.FromLiteral(string.Concat(Observers.Select(p => (char)p)))
                : null;
            if (Section)
            {
                NetMessage.SendData((int)PacketTypes.TileSendSection, -1, -1, playerList, X, Y, Width, Height);
                int sx1 = Netplay.GetSectionX(X), sy1 = Netplay.GetSectionY(Y);
                int sx2 = Netplay.GetSectionX(X + Width - 1), sy2 = Netplay.GetSectionY(Y + Height - 1);
                NetMessage.SendData((int)PacketTypes.TileFrameSection, -1, -1, playerList, sx1, sy1, sx2, sy2);
            }
            else
                NetMessage.SendData((int)PacketTypes.TileSendSquare, -1, -1, playerList, Math.Max(Width, Height), X, Y);
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            if (Data == null)
                return;
            Disable();
            Data = null;
        }

        #endregion
        #region ToString

        public override string ToString() => Name;

        #endregion
    }
}
