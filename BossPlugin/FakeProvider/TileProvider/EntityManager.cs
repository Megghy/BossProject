namespace FakeProvider;

using System.Collections.ObjectModel;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Tile_Entities;

public class EntityManager
{
    private static readonly HashSet<Type> ScannableEntityTypes = new()
    {
        typeof(TETrainingDummy),
        typeof(TEItemFrame),
        typeof(TELogicSensor),
        typeof(TEDisplayDoll),
        typeof(TEFoodPlatter),
        typeof(TEHatRack),
        typeof(TETeleportationPylon),
        typeof(TEWeaponsRack)
    };

    private readonly TileProvider _provider;
    private readonly List<IFake> _entities = new();
    public ReadOnlyCollection<IFake> Entities => new(_entities.ToList());
    private readonly object _locker = new();

    public EntityManager(TileProvider provider)
    {
        _provider = provider;
    }

    #region AddSign

    public FakeSign AddSign(int X, int Y, string Text)
    {
        FakeSign sign = new FakeSign(_provider, -1, X, Y, Text);
        lock (_locker)
            _entities.Add(sign);
        UpdateEntity(sign);
        return sign;
    }

    #endregion
    #region AddChest

    public FakeChest AddChest(int X, int Y, Item[] Items = null)
    {
        FakeChest chest = new FakeChest(_provider, -1, X, Y, Items);
        lock (_locker)
            _entities.Add(chest);
        UpdateEntity(chest);
        return chest;
    }

    #endregion
    #region AddTrainingDummy

    public FakeTrainingDummy AddTrainingDummy(int X, int Y)
    {
        FakeTrainingDummy dummy = new FakeTrainingDummy(_provider, -1, X, Y);
        lock (_locker)
            _entities.Add(dummy);
        UpdateEntity(dummy);
        return dummy;
    }

    #endregion
    #region AddItemFrame

    public FakeItemFrame AddItemFrame(int X, int Y, Item Item = null)
    {
        FakeItemFrame itemFrame = new FakeItemFrame(_provider, -1, X, Y, Item);
        lock (_locker)
            _entities.Add(itemFrame);
        UpdateEntity(itemFrame);
        return itemFrame;
    }

    #endregion
    #region AddLogicSensor

    public FakeLogicSensor AddLogicSensor(int X, int Y, TELogicSensor.LogicCheckType LogicCheckType)
    {
        FakeLogicSensor sensor = new FakeLogicSensor(_provider, -1, X, Y, LogicCheckType);
        lock (_locker)
            _entities.Add(sensor);
        UpdateEntity(sensor);
        return sensor;
    }

    #endregion
    #region AddDisplayDoll

    public FakeDisplayDoll AddDisplayDoll(int X, int Y, Item[] Items = null, Item[] Dyes = null)
    {
        FakeDisplayDoll doll = new FakeDisplayDoll(_provider, -1, X, Y, Items, Dyes);
        lock (_locker)
            _entities.Add(doll);
        UpdateEntity(doll);
        return doll;
    }

    #endregion
    #region AddFoodPlatter

    public FakeFoodPlatter AddFoodPlatter(int X, int Y, Item Item = null)
    {
        FakeFoodPlatter foodPlatter = new FakeFoodPlatter(_provider, -1, X, Y, Item);
        lock (_locker)
            _entities.Add(foodPlatter);
        UpdateEntity(foodPlatter);
        return foodPlatter;
    }

    #endregion
    #region AddHatRack

    public FakeHatRack AddHatRack(int X, int Y, Item[] Items = null, Item[] Dyes = null)
    {
        FakeHatRack rack = new FakeHatRack(_provider, -1, X, Y, Items, Dyes);
        lock (_locker)
            _entities.Add(rack);
        UpdateEntity(rack);
        return rack;
    }

    #endregion
    #region AddTeleportationPylon

    public FakeTeleportationPylon AddTeleportationPylon(int X, int Y)
    {
        FakeTeleportationPylon pylon = new FakeTeleportationPylon(_provider, -1, X, Y);
        lock (_locker)
            _entities.Add(pylon);
        UpdateEntity(pylon);
        return pylon;
    }

    #endregion
    #region AddWeaponsRack

    public FakeWeaponsRack AddWeaponsRack(int X, int Y, Item Item = null)
    {
        FakeWeaponsRack itemFrame = new FakeWeaponsRack(_provider, -1, X, Y, Item);
        lock (_locker)
            _entities.Add(itemFrame);
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
        int x = Entity.x - _provider.X;
        int y = Entity.y - _provider.Y;
        FakeSign sign = new FakeSign(_provider, replace ? Array.IndexOf(Main.sign, Entity) : -1, x, y, Entity.text);
        lock (_locker)
            _entities.Add(sign);
        UpdateEntity(sign);
        return sign;
    }

    public FakeChest AddEntity(Chest Entity, bool replace = false)
    {
        int x = Entity.x - _provider.X;
        int y = Entity.y - _provider.Y;
        FakeChest chest = new FakeChest(_provider, replace ? Array.IndexOf(Main.chest, Entity) : -1, x, y, Entity.item);
        lock (_locker)
            _entities.Add(chest);
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
        int x = Entity.Position.X - _provider.X;
        int y = Entity.Position.Y - _provider.Y;
        if (replace)
        {
            TileEntity.ByID.Remove(Entity.ID);
            TileEntity.ByPosition.Remove(Entity.Position);
        }
        FakeTrainingDummy fake = new FakeTrainingDummy(_provider, replace ? Entity.ID : -1, x, y, Entity.npc);
        lock (_locker)
            _entities.Add(fake);
        UpdateEntity(fake);
        return fake;
    }

    public FakeItemFrame AddEntity(TEItemFrame Entity, bool replace = false)
    {
        int x = Entity.Position.X - _provider.X;
        int y = Entity.Position.Y - _provider.Y;
        if (replace)
        {
            TileEntity.ByID.Remove(Entity.ID);
            TileEntity.ByPosition.Remove(Entity.Position);
        }
        FakeItemFrame fake = new FakeItemFrame(_provider, replace ? Entity.ID : -1, x, y, Entity.item);
        lock (_locker)
            _entities.Add(fake);
        UpdateEntity(fake);
        return fake;
    }

    public FakeLogicSensor AddEntity(TELogicSensor Entity, bool replace = false)
    {
        int x = Entity.Position.X - _provider.X;
        int y = Entity.Position.Y - _provider.Y;
        if (replace)
        {
            TileEntity.ByID.Remove(Entity.ID);
            TileEntity.ByPosition.Remove(Entity.Position);
        }
        FakeLogicSensor fake = new FakeLogicSensor(_provider, replace ? Entity.ID : -1, x, y, Entity.logicCheck);
        lock (_locker)
            _entities.Add(fake);
        UpdateEntity(fake);
        return fake;
    }

    public FakeDisplayDoll AddEntity(TEDisplayDoll Entity, bool replace = false)
    {
        int x = Entity.Position.X - _provider.X;
        int y = Entity.Position.Y - _provider.Y;
        if (replace)
        {
            TileEntity.ByID.Remove(Entity.ID);
            TileEntity.ByPosition.Remove(Entity.Position);
        }
        FakeDisplayDoll fake = new FakeDisplayDoll(_provider, replace ? Entity.ID : -1, x, y, Entity._items, Entity._dyes);
        lock (_locker)
            _entities.Add(fake);
        UpdateEntity(fake);
        return fake;
    }

    public FakeFoodPlatter AddEntity(TEFoodPlatter Entity, bool replace = false)
    {
        int x = Entity.Position.X - _provider.X;
        int y = Entity.Position.Y - _provider.Y;
        if (replace)
        {
            TileEntity.ByID.Remove(Entity.ID);
            TileEntity.ByPosition.Remove(Entity.Position);
        }
        FakeFoodPlatter fake = new FakeFoodPlatter(_provider, replace ? Entity.ID : -1, x, y, Entity.item);
        lock (_locker)
            _entities.Add(fake);
        UpdateEntity(fake);
        return fake;
    }

    public FakeHatRack AddEntity(TEHatRack Entity, bool replace = false)
    {
        int x = Entity.Position.X - _provider.X;
        int y = Entity.Position.Y - _provider.Y;
        if (replace)
        {
            TileEntity.ByID.Remove(Entity.ID);
            TileEntity.ByPosition.Remove(Entity.Position);
        }
        FakeHatRack fake = new FakeHatRack(_provider, replace ? Entity.ID : -1, x, y, Entity._items, Entity._dyes);
        lock (_locker)
            _entities.Add(fake);
        UpdateEntity(fake);
        return fake;
    }

    public FakeTeleportationPylon AddEntity(TETeleportationPylon Entity, bool replace = false)
    {
        int x = Entity.Position.X - _provider.X;
        int y = Entity.Position.Y - _provider.Y;
        if (replace)
        {
            TileEntity.ByID.Remove(Entity.ID);
            TileEntity.ByPosition.Remove(Entity.Position);
        }
        FakeTeleportationPylon fake = new FakeTeleportationPylon(_provider, replace ? Entity.ID : -1, x, y);
        lock (_locker)
            _entities.Add(fake);
        UpdateEntity(fake);
        return fake;
    }

    public FakeWeaponsRack AddEntity(TEWeaponsRack Entity, bool replace = false)
    {
        int x = Entity.Position.X - _provider.X;
        int y = Entity.Position.Y - _provider.Y;
        if (replace)
        {
            TileEntity.ByID.Remove(Entity.ID);
            TileEntity.ByPosition.Remove(Entity.Position);
        }
        FakeWeaponsRack fake = new FakeWeaponsRack(_provider, replace ? Entity.ID : -1, x, y, Entity.item);
        lock (_locker)
            _entities.Add(fake);
        UpdateEntity(fake);
        return fake;
    }

    #endregion
    #region RemoveEntity

    public void RemoveEntity(IFake Entity)
    {
        lock (_locker)
        {
            HideEntity(Entity);
            if (!_entities.Remove(Entity))
                throw new Exception("No such entity in this tile provider.");
        }
    }

    #endregion
    #region UpdateEntities

    public void UpdateEntities()
    {
        lock (_locker)
            foreach (IFake entity in _entities.ToArray())
                UpdateEntity(entity);
    }

    #endregion
    #region HideEntities

    public void HideEntities()
    {
        lock (_locker)
            foreach (IFake entity in _entities.ToArray())
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
            Entity.X = _provider.X + Entity.RelativeX;
            Entity.Y = _provider.Y + Entity.RelativeY;
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
            Entity.X = _provider.X + Entity.RelativeX;
            Entity.Y = _provider.Y + Entity.RelativeY;
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
            Entity.X = _provider.X + Entity.RelativeX;
            Entity.Y = _provider.Y + Entity.RelativeY;
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
        lock (_locker)
            foreach (IFake entity in _entities.ToArray())
                if (!IsEntityTile(entity.RelativeX, entity.RelativeY, entity.TileTypes))
                    RemoveEntity(entity);

        (int x, int y, int width, int height) = _provider.XYWH();
        for (int i = 0; i < Main.sign.Length; i++)
        {
            Sign sign = Main.sign[i];
            if (sign == null)
                continue;

            if (sign.GetType().Name == nameof(Sign) // <=> not FakeSign or some other inherited type
                && Helper.Inside(sign.x, sign.y, x, y, width, height)
                && TileOnTop(sign.x - _provider.X, sign.y - _provider.Y))
            {
                if (IsEntityTile(sign.x - _provider.X, sign.y - _provider.Y, FakeSign._TileTypes))
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
                && TileOnTop(chest.x - _provider.X, chest.y - _provider.Y))
            {
                if (IsEntityTile(chest.x - _provider.X, chest.y - _provider.Y, FakeChest._TileTypes))
                    AddEntity(chest, true);
                else
                    Main.chest[i] = null;
            }
        }

        foreach (TileEntity entity in TileEntity.ByID.Values.ToArray())
        {
            int entityX = entity.Position.X;
            int entityY = entity.Position.Y;

            if (ScannableEntityTypes.Contains(entity.GetType())
                && Helper.Inside(entityX, entityY, x, y, width, height)
                && TileOnTop(entityX - _provider.X, entityY - _provider.Y))
            {
                if (IsEntityTile(entityX - _provider.X, entityY - _provider.Y, GetEntityTileTypes(entity)))
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
        ITile providerTile = _provider.GetTileSafe(X, Y);
        return providerTile.active() && TileTypes.Contains(providerTile.type);
    }

    #endregion
    #region TileOnTop

    private bool TileOnTop(int X, int Y) =>
        _provider.ProviderCollection?.TileOnTopIndex(_provider.X + X, _provider.Y + Y) == _provider.Index;

    #endregion

    public void Clear()
    {
        foreach (var entity in Entities)
            RemoveEntity(entity);
    }

    internal void CopyFrom(EntityManager entityManager)
    {
        Clear();
        foreach (var entity in entityManager.Entities)
            AddEntity(entity);
    }
}