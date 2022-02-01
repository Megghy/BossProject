#region Using
using System.Collections.ObjectModel;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Tile_Entities;
#endregion
namespace FakeProvider
{
    public sealed class TileProvider : ModFramework.ICollection<ITile>, IDisposable
    {
        #region Data

        public TileProviderCollection ProviderCollection { get; internal set; }
        internal StructTile[,] Data;
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
        private List<IFake> _Entities = new List<IFake>();
        public ReadOnlyCollection<IFake> Entities => new ReadOnlyCollection<IFake>(_Entities.ToList());
        private object Locker = new object();

        #endregion

        #region Constructor

        internal TileProvider() { }

        #endregion
        #region Initialize

        internal void Initialize(string Name, int X, int Y, int Width, int Height, int Layer, HashSet<int> Observers = null)
        {
            this.Name = Name;
            this.Data = new StructTile[Width, Height];
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
            this.Layer = Layer;
            this.Observers = Observers;
        }

        internal void Initialize(string Name, int X, int Y, int Width, int Height, int Layer, ModFramework.ICollection<ITile> CopyFrom, HashSet<int> Observers = null)
        {
            this.Name = Name;
            this.Data = new StructTile[Width, Height];
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
            this.Layer = Layer;
            this.Observers = Observers;

            for (int i = X; i < X + Width; i++)
                for (int j = Y; j < Y + Height; j++)
                {
                    ITile t = CopyFrom[i, j];
                    if (t != null)
                        this[i, j].CopyFrom(t);
                }
        }

        internal void Initialize(string Name, int X, int Y, int Width, int Height, int Layer, ITile[,] CopyFrom, HashSet<int> Observers = null)
        {
            this.Name = Name;
            this.Data = new StructTile[Width, Height];
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
            this.Layer = Layer;
            this.Observers = Observers;

            for (int i = 0; i < Width; i++)
                for (int j = 0; j < Height; j++)
                {
                    ITile t = CopyFrom[i, j];
                    if (t != null)
                        this[i, j].CopyFrom(t);
                }
        }

        #endregion

        #region operator[,]

        ITile ModFramework.ICollection<ITile>.this[int X, int Y]
        {
            get => new TileReference(Data, X, Y);
            set => new TileReference(Data, X, Y).CopyFrom(value);
        }

        public ITile this[int X, int Y]
        {
            get => new TileReference(Data, X, Y);
            set => new TileReference(Data, X, Y).CopyFrom(value);
        }

        #endregion
        #region GetIncapsulatedTile

        public ITile GetIncapsulatedTile(int X, int Y) =>
            new TileReference(Data, X - this.X, Y - this.Y);

        #endregion
        #region SetIncapsulatedTile

        public void SetIncapsulatedTile(int X, int Y, ITile Tile) =>
            new TileReference(Data, X - this.X, Y - this.Y).CopyFrom(Tile);

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
            foreach (var entity in Entities)
                RemoveEntity(entity);
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

            foreach (var entity in provider.Entities)
                AddEntity(entity);
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

        public FakeSign AddSign(int X, int Y, string Text)
        {
            FakeSign sign = new FakeSign(this, -1, X, Y, Text);
            lock (Locker)
                _Entities.Add(sign);
            UpdateEntity(sign);
            return sign;
        }

        #endregion
        #region AddChest

        public FakeChest AddChest(int X, int Y, Item[] Items = null)
        {
            FakeChest chest = new FakeChest(this, -1, X, Y, Items);
            lock (Locker)
                _Entities.Add(chest);
            UpdateEntity(chest);
            return chest;
        }

        #endregion
        #region AddTrainingDummy

        public FakeTrainingDummy AddTrainingDummy(int X, int Y)
        {
            FakeTrainingDummy dummy = new FakeTrainingDummy(this, -1, X, Y);
            lock (Locker)
                _Entities.Add(dummy);
            UpdateEntity(dummy);
            return dummy;
        }

        #endregion
        #region AddItemFrame

        public FakeItemFrame AddItemFrame(int X, int Y, Item Item = null)
        {
            FakeItemFrame itemFrame = new FakeItemFrame(this, -1, X, Y, Item);
            lock (Locker)
                _Entities.Add(itemFrame);
            UpdateEntity(itemFrame);
            return itemFrame;
        }

        #endregion
        #region AddLogicSensor

        public FakeLogicSensor AddLogicSensor(int X, int Y, TELogicSensor.LogicCheckType LogicCheckType)
        {
            FakeLogicSensor sensor = new FakeLogicSensor(this, -1, X, Y, LogicCheckType);
            lock (Locker)
                _Entities.Add(sensor);
            UpdateEntity(sensor);
            return sensor;
        }

        #endregion
        #region AddDisplayDoll

        public FakeDisplayDoll AddDisplayDoll(int X, int Y, Item[] Items = null, Item[] Dyes = null)
        {
            FakeDisplayDoll doll = new FakeDisplayDoll(this, -1, X, Y, Items, Dyes);
            lock (Locker)
                _Entities.Add(doll);
            UpdateEntity(doll);
            return doll;
        }

        #endregion
        #region AddFoodPlatter

        public FakeFoodPlatter AddFoodPlatter(int X, int Y, Item Item = null)
        {
            FakeFoodPlatter foodPlatter = new FakeFoodPlatter(this, -1, X, Y, Item);
            lock (Locker)
                _Entities.Add(foodPlatter);
            UpdateEntity(foodPlatter);
            return foodPlatter;
        }

        #endregion
        #region AddHatRack

        public FakeHatRack AddHatRack(int X, int Y, Item[] Items = null, Item[] Dyes = null)
        {
            FakeHatRack rack = new FakeHatRack(this, -1, X, Y, Items, Dyes);
            lock (Locker)
                _Entities.Add(rack);
            UpdateEntity(rack);
            return rack;
        }

        #endregion
        #region AddTeleportationPylon

        public FakeTeleportationPylon AddTeleportationPylon(int X, int Y)
        {
            FakeTeleportationPylon pylon = new FakeTeleportationPylon(this, -1, X, Y);
            lock (Locker)
                _Entities.Add(pylon);
            UpdateEntity(pylon);
            return pylon;
        }

        #endregion
        #region AddWeaponsRack

        public FakeWeaponsRack AddWeaponsRack(int X, int Y, Item Item = null)
        {
            FakeWeaponsRack itemFrame = new FakeWeaponsRack(this, -1, X, Y, Item);
            lock (Locker)
                _Entities.Add(itemFrame);
            UpdateEntity(itemFrame);
            return itemFrame;
        }

        #endregion
        #region AddEntity

        public IFake AddEntity(IFake Entity) =>
            Entity is Sign sign
                ? AddEntity(sign)
                : Entity is Chest chest
                    ? AddEntity(chest)
                    : Entity is TileEntity tileEntity
                        ? AddEntity(tileEntity)
                        : throw new ArgumentException($"Unknown entity type {Entity.GetType().Name}",
                            nameof(Entity));

        public FakeSign AddEntity(Sign Entity, bool replace = false)
        {
            int x = Entity.x - ProviderCollection.OffsetX - this.X;
            int y = Entity.y - ProviderCollection.OffsetY - this.Y;
            FakeSign sign = new FakeSign(this, replace ? Array.IndexOf(Main.sign, Entity) : -1, x, y, Entity.text);
            lock (Locker)
                _Entities.Add(sign);
            UpdateEntity(sign);
            return sign;
        }

        public FakeChest AddEntity(Chest Entity, bool replace = false)
        {
            int x = Entity.x - ProviderCollection.OffsetX - this.X;
            int y = Entity.y - ProviderCollection.OffsetY - this.Y;
            FakeChest chest = new FakeChest(this, replace ? Array.IndexOf(Main.chest, Entity) : -1, x, y, Entity.item);
            lock (Locker)
                _Entities.Add(chest);
            UpdateEntity(chest);
            return chest;
        }

        public IFake AddEntity(TileEntity Entity, bool replace = false) =>
            Entity is TETrainingDummy trainingDummy
                ? (IFake)AddEntity(trainingDummy, replace)
                : Entity is TEItemFrame itemFrame
                    ? (IFake)AddEntity(itemFrame, replace)
                    : Entity is TELogicSensor logicSensor
                        ? (IFake)AddEntity(logicSensor, replace)
                        : Entity is TEDisplayDoll displayDoll
                            ? (IFake)AddEntity(displayDoll, replace)
                            : Entity is TEFoodPlatter foodPlatter
                                ? (IFake)AddEntity(foodPlatter, replace)
                                : Entity is TEHatRack hatRack
                                    ? (IFake)AddEntity(hatRack, replace)
                                    : Entity is TETeleportationPylon teleportationPylon
                                        ? (IFake)AddEntity(teleportationPylon, replace)
                                        : Entity is TEWeaponsRack weaponsRack
                                            ? (IFake)AddEntity(weaponsRack, replace)
                                            : throw new ArgumentException($"Unknown entity type {Entity.GetType().Name}", nameof(Entity));

        public FakeTrainingDummy AddEntity(TETrainingDummy Entity, bool replace = false)
        {
            int x = Entity.Position.X - ProviderCollection.OffsetX - this.X;
            int y = Entity.Position.Y - ProviderCollection.OffsetY - this.Y;
            if (replace)
            {
                TileEntity.ByID.Remove(Entity.ID);
                TileEntity.ByPosition.Remove(Entity.Position);
            }
            FakeTrainingDummy fake = new FakeTrainingDummy(this, replace ? Entity.ID : -1, x, y, Entity.npc);
            lock (Locker)
                _Entities.Add(fake);
            UpdateEntity(fake);
            return fake;
        }

        public FakeItemFrame AddEntity(TEItemFrame Entity, bool replace = false)
        {
            int x = Entity.Position.X - ProviderCollection.OffsetX - this.X;
            int y = Entity.Position.Y - ProviderCollection.OffsetY - this.Y;
            if (replace)
            {
                TileEntity.ByID.Remove(Entity.ID);
                TileEntity.ByPosition.Remove(Entity.Position);
            }
            FakeItemFrame fake = new FakeItemFrame(this, replace ? Entity.ID : -1, x, y, Entity.item);
            lock (Locker)
                _Entities.Add(fake);
            UpdateEntity(fake);
            return fake;
        }

        public FakeLogicSensor AddEntity(TELogicSensor Entity, bool replace = false)
        {
            int x = Entity.Position.X - ProviderCollection.OffsetX - this.X;
            int y = Entity.Position.Y - ProviderCollection.OffsetY - this.Y;
            if (replace)
            {
                TileEntity.ByID.Remove(Entity.ID);
                TileEntity.ByPosition.Remove(Entity.Position);
            }
            FakeLogicSensor fake = new FakeLogicSensor(this, replace ? Entity.ID : -1, x, y, Entity.logicCheck);
            lock (Locker)
                _Entities.Add(fake);
            UpdateEntity(fake);
            return fake;
        }

        public FakeDisplayDoll AddEntity(TEDisplayDoll Entity, bool replace = false)
        {
            int x = Entity.Position.X - ProviderCollection.OffsetX - this.X;
            int y = Entity.Position.Y - ProviderCollection.OffsetY - this.Y;
            if (replace)
            {
                TileEntity.ByID.Remove(Entity.ID);
                TileEntity.ByPosition.Remove(Entity.Position);
            }
            FakeDisplayDoll fake = new FakeDisplayDoll(this, replace ? Entity.ID : -1, x, y, Entity._items, Entity._dyes);
            lock (Locker)
                _Entities.Add(fake);
            UpdateEntity(fake);
            return fake;
        }

        public FakeFoodPlatter AddEntity(TEFoodPlatter Entity, bool replace = false)
        {
            int x = Entity.Position.X - ProviderCollection.OffsetX - this.X;
            int y = Entity.Position.Y - ProviderCollection.OffsetY - this.Y;
            if (replace)
            {
                TileEntity.ByID.Remove(Entity.ID);
                TileEntity.ByPosition.Remove(Entity.Position);
            }
            FakeFoodPlatter fake = new FakeFoodPlatter(this, replace ? Entity.ID : -1, x, y, Entity.item);
            lock (Locker)
                _Entities.Add(fake);
            UpdateEntity(fake);
            return fake;
        }

        public FakeHatRack AddEntity(TEHatRack Entity, bool replace = false)
        {
            int x = Entity.Position.X - ProviderCollection.OffsetX - this.X;
            int y = Entity.Position.Y - ProviderCollection.OffsetY - this.Y;
            if (replace)
            {
                TileEntity.ByID.Remove(Entity.ID);
                TileEntity.ByPosition.Remove(Entity.Position);
            }
            FakeHatRack fake = new FakeHatRack(this, replace ? Entity.ID : -1, x, y, Entity._items, Entity._dyes);
            lock (Locker)
                _Entities.Add(fake);
            UpdateEntity(fake);
            return fake;
        }

        public FakeTeleportationPylon AddEntity(TETeleportationPylon Entity, bool replace = false)
        {
            int x = Entity.Position.X - ProviderCollection.OffsetX - this.X;
            int y = Entity.Position.Y - ProviderCollection.OffsetY - this.Y;
            if (replace)
            {
                TileEntity.ByID.Remove(Entity.ID);
                TileEntity.ByPosition.Remove(Entity.Position);
            }
            FakeTeleportationPylon fake = new FakeTeleportationPylon(this, replace ? Entity.ID : -1, x, y);
            lock (Locker)
                _Entities.Add(fake);
            UpdateEntity(fake);
            return fake;
        }

        public FakeWeaponsRack AddEntity(TEWeaponsRack Entity, bool replace = false)
        {
            int x = Entity.Position.X - ProviderCollection.OffsetX - this.X;
            int y = Entity.Position.Y - ProviderCollection.OffsetY - this.Y;
            if (replace)
            {
                TileEntity.ByID.Remove(Entity.ID);
                TileEntity.ByPosition.Remove(Entity.Position);
            }
            FakeWeaponsRack fake = new FakeWeaponsRack(this, replace ? Entity.ID : -1, x, y, Entity.item);
            lock (Locker)
                _Entities.Add(fake);
            UpdateEntity(fake);
            return fake;
        }

        #endregion
        #region RemoveEntity

        public void RemoveEntity(IFake Entity)
        {
            lock (Locker)
            {
                HideEntity(Entity);
                if (!_Entities.Remove(Entity))
                    throw new Exception("No such entity in this tile provider.");
            }
        }

        #endregion
        #region UpdateEntities

        public void UpdateEntities()
        {
            lock (Locker)
                foreach (IFake entity in _Entities.ToArray())
                    UpdateEntity(entity);
        }

        #endregion
        #region HideEntities

        public void HideEntities()
        {
            lock (Locker)
                foreach (IFake entity in _Entities.ToArray())
                    HideEntity(entity);
        }

        #endregion
        #region UpdateEntity

        private bool UpdateEntity(IFake Entity)
        {
            if (IsEntityTile(Entity.RelativeX, Entity.RelativeY, Entity.TileTypes)
                    && TileOnTop(Entity.RelativeX, Entity.RelativeY))
                return ApplyEntity(Entity);
            else
                HideEntity(Entity);
            return true;
        }

        #endregion
        #region ApplyEntity

        private bool ApplyEntity(IFake Entity)
        {
            if (Entity is FakeSign)
            {
                Entity.X = ProviderCollection.OffsetX + this.X + Entity.RelativeX;
                Entity.Y = ProviderCollection.OffsetY + this.Y + Entity.RelativeY;
                if (Entity.Index >= 0 && Main.sign[Entity.Index] == Entity)
                    return true;

                bool applied = false;
                for (int i = 0; i < 1000; i++)
                {
                    if (Main.sign[i] != null && Main.sign[i].x == Entity.X && Main.sign[i].y == Entity.Y)
                        Main.sign[i] = null;
                    if (!applied && Main.sign[i] == null)
                    {
                        applied = true;
                        Main.sign[i] = (FakeSign)Entity;
                        Entity.Index = i;
                    }
                }
                return applied;
            }
            else if (Entity is FakeChest)
            {
                Entity.X = ProviderCollection.OffsetX + this.X + Entity.RelativeX;
                Entity.Y = ProviderCollection.OffsetY + this.Y + Entity.RelativeY;
                if (Entity.Index >= 0 && Main.chest[Entity.Index] == Entity)
                    return true;

                bool applied = false;
                for (int i = 0; i < 1000; i++)
                {
                    if (Main.chest[i] != null && Main.chest[i].x == Entity.X && Main.chest[i].y == Entity.Y)
                        Main.chest[i] = null;
                    if (!applied && Main.chest[i] == null)
                    {
                        applied = true;
                        Main.chest[i] = (FakeChest)Entity;
                        Entity.Index = i;
                    }
                }
                return applied;
            }
            else if (Entity is TileEntity)
            {
                Point16 position = new Point16(Entity.X, Entity.Y);
                if (TileEntity.ByPosition.TryGetValue(position, out TileEntity entity)
                        && entity == Entity)
                    TileEntity.ByPosition.Remove(position);
                Entity.X = ProviderCollection.OffsetX + this.X + Entity.RelativeX;
                Entity.Y = ProviderCollection.OffsetY + this.Y + Entity.RelativeY;
                TileEntity.ByPosition[new Point16(Entity.X, Entity.Y)] = (TileEntity)Entity;
                if (Entity.Index < 0)
                    Entity.Index = TileEntity.AssignNewID();
                TileEntity.ByID[Entity.Index] = (TileEntity)Entity;
                return true;
            }
            else
                throw new ArgumentException($"Unknown entity type {Entity.GetType().Name}", nameof(Entity));
        }

        #endregion
        #region HideEntity

        private void HideEntity(IFake Entity)
        {
            if (Entity is Sign)
            {
                if (Entity.Index >= 0 && Main.sign[Entity.Index] == Entity)
                    Main.sign[Entity.Index] = null;
            }
            else if (Entity is Chest)
            {
                if (Entity.Index >= 0 && Main.chest[Entity.Index] == Entity)
                    Main.chest[Entity.Index] = null;
            }
            else if (Entity is TileEntity entity)
            {
                TileEntity.ByID.Remove(Entity.Index);
                if (Entity.Index >= 0
                        && TileEntity.ByPosition.TryGetValue(entity.Position, out TileEntity entity2)
                        && entity == entity2)
                    TileEntity.ByPosition.Remove(entity.Position);

                if (Entity is TETrainingDummy trainingDummy && trainingDummy.npc >= 0)
                {
                    NPC npc = Main.npc[trainingDummy.npc];
                    npc.type = 0;
                    npc.active = false;
                    NetMessage.SendData((int)PacketTypes.NpcUpdate, -1, -1, null, trainingDummy.npc);
                    trainingDummy.npc = -1;
                }
            }
            else
                throw new ArgumentException($"Unknown entity type {Entity.GetType().Name}", nameof(Entity));
        }

        #endregion
        #region GetEntityTileTypes

        private ushort[] GetEntityTileTypes(TileEntity Entity) =>
            Entity is TETrainingDummy
                ? FakeTrainingDummy._TileTypes
                : Entity is TEItemFrame
                    ? FakeItemFrame._TileTypes
                    : Entity is TELogicSensor
                        ? FakeLogicSensor._TileTypes
                        : Entity is TEDisplayDoll
                            ? FakeDisplayDoll._TileTypes
                            : Entity is TEFoodPlatter
                                ? FakeFoodPlatter._TileTypes
                                : Entity is TEHatRack
                                    ? FakeHatRack._TileTypes
                                    : Entity is TETeleportationPylon
                                        ? FakeTeleportationPylon._TileTypes
                                        : Entity is TEWeaponsRack
                                            ? FakeWeaponsRack._TileTypes
                                            : throw new ArgumentException($"Unknown entity type {Entity.GetType().Name}", nameof(Entity));

        #endregion
        #region ScanEntities

        public void ScanEntities()
        {
            lock (Locker)
                foreach (IFake entity in _Entities.ToArray())
                    if (!IsEntityTile(entity.RelativeX, entity.RelativeY, entity.TileTypes))
                        RemoveEntity(entity);

            (int x, int y, int width, int height) = XYWH(ProviderCollection.OffsetX, ProviderCollection.OffsetY);
            for (int i = 0; i < Main.sign.Length; i++)
            {
                Sign sign = Main.sign[i];
                if (sign == null)
                    continue;

                if (sign.GetType().Name == nameof(Sign) // <=> not FakeSign or some other inherited type
                    && Helper.Inside(sign.x, sign.y, x, y, width, height)
                    && TileOnTop(sign.x - this.X, sign.y - this.Y))
                {
                    if (IsEntityTile(sign.x - this.X, sign.y - this.Y, FakeSign._TileTypes))
                        AddEntity(sign, true);
                    else
                        Main.sign[i] = null;
                }
            }

            for (int i = 0; i < Main.chest.Length; i++)
            {
                Chest chest = Main.chest[i];
                if (chest == null)
                    continue;

                if (chest.GetType().Name == nameof(Chest) // <=> not FakeChest or some other inherited type
                    && Helper.Inside(chest.x, chest.y, x, y, width, height)
                    && TileOnTop(chest.x - this.X, chest.y - this.Y))
                {
                    if (IsEntityTile(chest.x - this.X, chest.y - this.Y, FakeChest._TileTypes))
                        AddEntity(chest, true);
                    else
                        Main.chest[i] = null;
                }
            }

            foreach (TileEntity entity in TileEntity.ByID.Values.ToArray())
            {
                int entityX = entity.Position.X;
                int entityY = entity.Position.Y;

                if ((entity.GetType().Name == nameof(TETrainingDummy)            // <=> not FakeTrainingDummy or some other inherited type
                        || entity.GetType().Name == nameof(TEItemFrame)          // <=> not FakeItemFrame or some other inherited type
                        || entity.GetType().Name == nameof(TELogicSensor)        // <=> not FakeLogicSensor or some other inherited type
                        || entity.GetType().Name == nameof(TEDisplayDoll)        // <=> not FakeDisplayDoll or some other inherited type
                        || entity.GetType().Name == nameof(TEFoodPlatter)        // <=> not FakeFoodPlatter or some other inherited type
                        || entity.GetType().Name == nameof(TEHatRack)            // <=> not FakeHatRack or some other inherited type
                        || entity.GetType().Name == nameof(TETeleportationPylon) // <=> not FakeTeleportationPylon or some other inherited type
                        || entity.GetType().Name == nameof(TEWeaponsRack))       // <=> not FakeWeaponsRack or some other inherited type
                    && Helper.Inside(entityX, entityY, x, y, width, height)
                    && TileOnTop(entityX - this.X, entityY - this.Y))
                {
                    if (IsEntityTile(entityX - this.X, entityY - this.Y, GetEntityTileTypes(entity)))
                        AddEntity(entity, true);
                    else
                    {
                        TileEntity.ByID.Remove(entity.ID);
                        TileEntity.ByPosition.Remove(entity.Position);
                    }
                }
            }
        }

        #endregion
        #region IsEntityTile

        private bool IsEntityTile(int X, int Y, ushort[] TileTypes)
        {
            ITile providerTile = GetTileSafe(X, Y);
            return providerTile.active() && TileTypes.Contains(providerTile.type);
        }

        #endregion
        #region TileOnTop

        private bool TileOnTop(int X, int Y) =>
            ProviderCollection.ProviderIndexes[this.X + X, this.Y + Y] == Index;

        #endregion

        #region Draw

        public void Draw(bool Section = true)
        {
            if (Section)
            {
                NetMessage.SendData((int)PacketTypes.TileSendSection, -1, -1, null, X, Y, Width, Height);
                int sx1 = Netplay.GetSectionX(X), sy1 = Netplay.GetSectionY(Y);
                int sx2 = Netplay.GetSectionX(X + Width - 1), sy2 = Netplay.GetSectionY(Y + Height - 1);
                NetMessage.SendData((int)PacketTypes.TileFrameSection, -1, -1, null, sx1, sy1, sx2, sy2);
            }
            else
                NetMessage.SendData((int)PacketTypes.TileSendSquare, -1, -1, null, Math.Max(Width, Height), X, Y);
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
