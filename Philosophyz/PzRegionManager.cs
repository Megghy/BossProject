using System.Data;
using MySql.Data.MySqlClient;
using Terraria;
using TShockAPI;
using TShockAPI.DB;
using TShockAPI.DB.Queries;

namespace Philosophyz
{
    public class PzRegion
    {
        public int Id { get; set; }

        /// <summary>
        /// 用于单一存档时的默认存档
        /// </summary>
        public string Default { get; set; }

        /// <summary>
        /// 存档集合
        /// </summary>
        public Dictionary<string, PlayerData> PlayerDatas { get; set; }

        /// <summary>
        /// 是否为单一存档
        /// </summary>
        public bool HasDefault => !string.IsNullOrWhiteSpace(Default);

        /// <summary>
        /// 获取单一存档 (如果有的话)
        /// </summary>
        /// <returns>单一存档信息</returns>
        public PlayerData GetDefaultData()
        {
            if (!HasDefault)
                return null;

            return !PlayerDatas.TryGetValue(Default, out PlayerData r) ? null : r;
        }
    }

    internal class PzRegionManager
    {
        public List<PzRegion> PzRegions = new List<PzRegion>();

        private readonly IDbConnection _database;

        public PzRegionManager(IDbConnection db)
        {
            _database = db;

            var table = new SqlTable("PzRegions",
                                     new SqlColumn("Region", MySqlDbType.Int32) { Primary = true },
                                     new SqlColumn("DefaultChar", MySqlDbType.VarChar, 10)
            );

            var charsTable = new SqlTable("PzChars",
                                          new SqlColumn("Name", MySqlDbType.VarChar, 10) { Unique = true },
                                          new SqlColumn("RegionId", MySqlDbType.Int32) { Unique = true },
                                          new SqlColumn("Health", MySqlDbType.Int32),
                                          new SqlColumn("MaxHealth", MySqlDbType.Int32),
                                          new SqlColumn("Mana", MySqlDbType.Int32),
                                          new SqlColumn("MaxMana", MySqlDbType.Int32),
                                          new SqlColumn("Inventory", MySqlDbType.Text),
                                          new SqlColumn("extraSlot", MySqlDbType.Int32),
                                          new SqlColumn("spawnX", MySqlDbType.Int32),
                                          new SqlColumn("spawnY", MySqlDbType.Int32),
                                          new SqlColumn("skinVariant", MySqlDbType.Int32),
                                          new SqlColumn("hair", MySqlDbType.Int32),
                                          new SqlColumn("hairDye", MySqlDbType.Int32),
                                          new SqlColumn("hairColor", MySqlDbType.Int32),
                                          new SqlColumn("pantsColor", MySqlDbType.Int32),
                                          new SqlColumn("shirtColor", MySqlDbType.Int32),
                                          new SqlColumn("underShirtColor", MySqlDbType.Int32),
                                          new SqlColumn("shoeColor", MySqlDbType.Int32),
                                          new SqlColumn("hideVisuals", MySqlDbType.Int32),
                                          new SqlColumn("skinColor", MySqlDbType.Int32),
                                          new SqlColumn("eyeColor", MySqlDbType.Int32),
                                          new SqlColumn("questsCompleted", MySqlDbType.Int32)
            );
            var creator = new SqlTableCreator(db,
                                              db.GetSqlType() == SqlType.Sqlite
                                                  ? (IQueryBuilder)new SqliteQueryBuilder()
                                                  : new MysqlQueryBuilder());
            creator.EnsureTableStructure(table);
            creator.EnsureTableStructure(charsTable);
        }

        /// <summary>
        /// 重新加载所有数据库中的pz区域
        /// </summary>
        public void ReloadRegions()
        {
            PzRegions.Clear();

            using (var reader = _database.QueryReader("SELECT PzRegions.* FROM PzRegions, Regions WHERE PzRegions.Region = Regions.Id AND Regions.WorldID = @0", Main.worldID))
            {
                while (reader != null && reader.Read())
                {
                    var id = reader.Get<int>("Region");
                    var region = new PzRegion
                    {
                        Id = id,
                        Default = reader.Get<string>("DefaultChar"),
                        PlayerDatas = ReadDatas(id)
                    };

                    PzRegions.Add(region);

                    if (!string.IsNullOrWhiteSpace(region.Default) && !region.PlayerDatas.ContainsKey(region.Default))
                    {
                        TShock.Log.Warn("[PzRegion] 已经删除无效的默认值 ({0}-{1}).", region.Default, region.Id);
                        region.Default = null;
                    }
                }
            }
        }

        /// <summary>
        /// 添加区域作为pz区域
        /// </summary>
        /// <param name="id">区域</param>
        public void AddRegion(int id)
        {
            if (PzRegions.Exists(p => p.Id == id))
                return;

            try
            {
                _database.Query(
                    "INSERT INTO PzRegions(Region) VALUES(@0); ", id);
                PzRegions.Add(new PzRegion
                {
                    Id = id,
                    PlayerDatas = new Dictionary<string, PlayerData>()
                });
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
            }
        }

        /// <summary>
        /// 删去区域及所有人物存档
        /// </summary>
        /// <param name="id"></param>
        public void RemoveRegion(int id)
        {
            PzRegions.RemoveAll(p => p.Id == id);
            try
            {
                _database.Query("DELETE FROM PzRegions WHERE Region=@0;", id);
                _database.Query("DELETE FROM PzChars WHERE RegionId=@0;", id);
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
            }
        }

        /// <summary>
        /// 增加人物存档
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="playerData"></param>
        public void AddCharacter(int id, string name, PlayerData playerData)
        {
            if (!PzRegions.Exists(p => p.Id == id))
                return;

            var datas = GetRegionById(id).PlayerDatas;

            try
            {
                if (datas.ContainsKey(name))
                {
                    datas[name] = playerData;
                    _database.Query(
                        "UPDATE PzChars SET Health = @0, MaxHealth = @1, Mana = @2, MaxMana = @3, Inventory = @4, spawnX = @6, spawnY = @7, hair = @8, hairDye = @9, hairColor = @10, pantsColor = @11, shirtColor = @12, underShirtColor = @13, shoeColor = @14, hideVisuals = @15, skinColor = @16, eyeColor = @17, questsCompleted = @18, skinVariant = @19, extraSlot = @20 WHERE RegionId = @5 AND Name = @21;",
                        playerData.health,
                        playerData.maxHealth,
                        playerData.mana,
                        playerData.maxMana,
                        string.Join("~", playerData.inventory),
                        id,
                        playerData.spawnX,
                        playerData.spawnY,
                        playerData.hair,
                        playerData.hairDye,
                        TShock.Utils.EncodeColor(playerData.hairColor),
                        TShock.Utils.EncodeColor(playerData.pantsColor),
                        TShock.Utils.EncodeColor(playerData.shirtColor),
                        TShock.Utils.EncodeColor(playerData.underShirtColor),
                        TShock.Utils.EncodeColor(playerData.shoeColor),
                        TShock.Utils.EncodeBoolArray(playerData.hideVisuals),
                        TShock.Utils.EncodeColor(playerData.skinColor),
                        TShock.Utils.EncodeColor(playerData.eyeColor),
                        playerData.questsCompleted,
                        playerData.skinVariant,
                        playerData.extraSlot,
                        name
                    );
                }
                else
                {
                    datas.Add(name, playerData);
                    _database.Query(
                    "INSERT INTO PzChars(RegionId, Name, Health, MaxHealth, Mana, MaxMana, Inventory, extraSlot, spawnX, spawnY, skinVariant, hair, hairDye, hairColor, pantsColor, shirtColor, underShirtColor, shoeColor, hideVisuals, skinColor, eyeColor, questsCompleted) VALUES(@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10, @11, @12, @13, @14, @15, @16, @17, @18, @19, @20, @21); ",
                        id,
                        name,
                        playerData.health,
                        playerData.maxHealth,
                        playerData.mana,
                        playerData.maxMana,
                        string.Join("~", playerData.inventory),
                        playerData.extraSlot,
                        playerData.spawnX,
                        playerData.spawnY,
                        playerData.skinVariant,
                        playerData.hair,
                        playerData.hairDye,
                        TShock.Utils.EncodeColor(playerData.hairColor),
                        TShock.Utils.EncodeColor(playerData.pantsColor),
                        TShock.Utils.EncodeColor(playerData.shirtColor),
                        TShock.Utils.EncodeColor(playerData.underShirtColor),
                        TShock.Utils.EncodeColor(playerData.shoeColor),
                        TShock.Utils.EncodeBoolArray(playerData.hideVisuals),
                        TShock.Utils.EncodeColor(playerData.skinColor),
                        TShock.Utils.EncodeColor(playerData.eyeColor),
                        playerData.questsCompleted
                    );
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
            }
        }

        /// <summary>
        /// 删去区域内的人物存档
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        public void RemoveCharacter(int id, string name)
        {
            if (!PzRegions.Exists(p => p.Id == id))
                return;

            var region = GetRegionById(id);

            region.PlayerDatas.Remove(name);

            if (string.Equals(name, region.Default, StringComparison.Ordinal))
                SetDefaultCharacter(id, null);

            try
            {
                _database.Query("DELETE FROM PzChars WHERE Name = @0 AND RegionId = @1", name, id);
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
            }
        }

        /// <summary>
        /// 设定区域内的单一存档
        /// </summary>
        /// <param name="id">区域</param>
        /// <param name="name">存档名</param>
        public void SetDefaultCharacter(int id, string name)
        {
            if (!PzRegions.Exists(p => p.Id == id))
                return;

            GetRegionById(id).Default = name;

            try
            {
                _database.Query("UPDATE PzRegions SET DefaultChar = @0 WHERE Region = @1", name, id);
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
            }
        }

        /// <summary>
        /// 使用id寻找pz区域
        /// </summary>
        /// <param name="id">区域id</param>
        /// <returns>区域(若无则为null)</returns>
        public PzRegion GetRegionById(int id)
        {
            return PzRegions.SingleOrDefault(p => p.Id == id);
        }

        public bool TryGetCharater(int id, string name, out PlayerData data)
        {
            return GetRegionById(id).PlayerDatas.TryGetValue(name, out data);
        }

        /// <summary>
        /// 从数据库数据中读取存档信息
        /// </summary>
        /// <param name="reader">数据源</param>
        /// <returns>item1: 存档标识 item2: 存档数据</returns>
        private static Tuple<string, PlayerData> Read(QueryResult reader)
        {
            var items = reader.Get<string>("Inventory").Split('~');
            //Console.WriteLine(string.Join("\r\n", items));
            var inventory = items.Select(NetItem.Parse).ToList();
            if (inventory.Count < NetItem.MaxInventory)
            {
                //TODO: unhardcode this - stop using magic numbers and use NetItem numbers
                //Set new armour slots empty
                //inventory.InsertRange(67, new NetItem[2]);
                //Set new vanity slots empty
                //inventory.InsertRange(77, new NetItem[2]);
                //Set new dye slots empty
                //inventory.InsertRange(87, new NetItem[2]);
                //Set the rest of the new slots empty
                inventory.AddRange(new NetItem[NetItem.MaxInventory - inventory.Count]);
            }

            var playerData = new PlayerData(null)
            {
                exists = true,
                health = reader.Get<int>("Health"),
                maxHealth = reader.Get<int>("MaxHealth"),
                mana = reader.Get<int>("Mana"),
                maxMana = reader.Get<int>("MaxMana"),
                inventory = inventory.ToArray(),
                extraSlot = reader.Get<int>("extraSlot"),
                spawnX = reader.Get<int>("spawnX"),
                spawnY = reader.Get<int>("spawnY"),
                skinVariant = reader.Get<int?>("skinVariant"),
                hair = reader.Get<int?>("hair"),
                hairDye = (byte)reader.Get<int>("hairDye"),
                hairColor = TShock.Utils.DecodeColor(reader.Get<int?>("hairColor")),
                pantsColor = TShock.Utils.DecodeColor(reader.Get<int?>("pantsColor")),
                shirtColor = TShock.Utils.DecodeColor(reader.Get<int?>("shirtColor")),
                underShirtColor = TShock.Utils.DecodeColor(reader.Get<int?>("underShirtColor")),
                shoeColor = TShock.Utils.DecodeColor(reader.Get<int?>("shoeColor")),
                hideVisuals = TShock.Utils.DecodeBoolArray(reader.Get<int?>("hideVisuals")),
                skinColor = TShock.Utils.DecodeColor(reader.Get<int?>("skinColor")),
                eyeColor = TShock.Utils.DecodeColor(reader.Get<int?>("eyeColor")),
                questsCompleted = reader.Get<int>("questsCompleted")
            };

            return new Tuple<string, PlayerData>(reader.Get<string>("Name"), playerData);
        }

        /// <summary>
        /// 读取区域对应的所有存档数据
        /// </summary>
        /// <param name="id">区域</param>
        /// <returns>存档数据集合</returns>
        private Dictionary<string, PlayerData> ReadDatas(int id)
        {
            var datas = new Dictionary<string, PlayerData>();
            using (var reader = _database.QueryReader("SELECT * FROM PzChars WHERE RegionId = @0", id))
            {
                while (reader != null && reader.Read())
                {
                    var current = Read(reader);
                    datas.Add(current.Item1, current.Item2);
                }
            }
            return datas;
        }
    }
}
