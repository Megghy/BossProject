using System.Collections.Concurrent;
using System.Data;
using System.IO.Compression;
using System.Reflection;
using BossFramework.BCore;
using BossFramework.BModels;
using BossFramework.BNet;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.GameContent.Tile_Entities;
using TrProtocol;
using TrProtocol.Models;
using TrProtocol.Packets;
using TShockAPI;
using Color = Microsoft.Xna.Framework.Color;
using ProtocolBitsByte = TrProtocol.Models.BitsByte;

namespace BossFramework
{
    /// <summary>
    /// Boss框架工具类，提供各种实用方法
    /// </summary>
    public static class BUtils
    {
        #region 内部类

        /// <summary>
        /// 简单的包缓存实现（LRU策略）
        /// </summary>
        private class PacketCache
        {
            private readonly ConcurrentDictionary<int, CacheItem> _cache = new();
            private readonly object _lock = new();
            private const int MaxCacheSize = 100;
            private const int CleanupThreshold = 120;

            public bool TryGetValue(Packet packet, out byte[] data)
            {
                data = null;
                var key = packet.GetHashCode();
                if (_cache.TryGetValue(key, out var item) &&
                    DateTime.UtcNow - item.CreatedTime < TimeSpan.FromSeconds(30))
                {
                    item.LastAccess = DateTime.UtcNow;
                    data = item.Data;
                    return true;
                }
                return false;
            }

            public void Set(Packet packet, byte[] data)
            {
                var key = packet.GetHashCode();
                var item = new CacheItem
                {
                    Data = data,
                    CreatedTime = DateTime.UtcNow,
                    LastAccess = DateTime.UtcNow
                };

                _cache.AddOrUpdate(key, item, (k, v) => item);

                // 定期清理过期缓存
                if (_cache.Count > CleanupThreshold)
                {
                    Task.Run(CleanupExpiredItems);
                }
            }

            private void CleanupExpiredItems()
            {
                lock (_lock)
                {
                    var expiredKeys = _cache
                        .Where(kvp => DateTime.UtcNow - kvp.Value.LastAccess > TimeSpan.FromSeconds(30))
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var key in expiredKeys)
                    {
                        _cache.TryRemove(key, out _);
                    }

                    // 如果仍超出限制，移除最老的项
                    if (_cache.Count > MaxCacheSize)
                    {
                        var oldestKeys = _cache
                            .OrderBy(kvp => kvp.Value.LastAccess)
                            .Take(_cache.Count - MaxCacheSize)
                            .Select(kvp => kvp.Key)
                            .ToList();

                        foreach (var key in oldestKeys)
                        {
                            _cache.TryRemove(key, out _);
                        }
                    }
                }
            }

            private class CacheItem
            {
                public byte[] Data { get; set; }
                public DateTime CreatedTime { get; set; }
                public DateTime LastAccess { get; set; }
            }
        }

        #endregion

        #region 字段和属性

        /// <summary>
        /// 全局随机数生成器
        /// </summary>
        public static readonly Random Rand = new();

        /// <summary>
        /// 包序列化缓存（简单LRU实现）
        /// </summary>
        private static readonly PacketCache _packetCache = new();

        /// <summary>
        /// 解析参数的反射方法
        /// </summary>
        private static readonly MethodInfo _parseParametersMethod = typeof(Commands)
            .GetMethod("ParseParameters", BindingFlags.NonPublic | BindingFlags.Static);

        #endregion

        #region 集合扩展方法

        /// <summary>
        /// 为集合的每个元素执行操作，并提供索引
        /// </summary>
        /// <typeparam name="T">集合元素类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="action">要执行的操作</param>
        public static void ForEachWithIndex<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            int index = 0;
            foreach (var item in source)
            {
                action(item, index++);
            }
        }

        /// <summary>
        /// 循环执行指定次数的操作
        /// </summary>
        /// <param name="count">循环次数</param>
        /// <param name="action">要执行的操作</param>
        public static void For(this int count, Action<int> action)
        {
            if (count <= 0) return;
            for (int i = 0; i < count; i++)
            {
                action(i);
            }
        }

        /// <summary>
        /// 从集合中随机选择一个元素
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="source">源集合</param>
        /// <returns>随机选择的元素</returns>
        public static T Random<T>(this IEnumerable<T> source)
            => source is null || !source.Any() ? default : source.ToList()[Rand.Next(0, source.Count())];

        #endregion

        #region 字符串处理

        /// <summary>
        /// 检查两个字符串是否相似（忽略大小写，支持前缀匹配）
        /// </summary>
        /// <param name="text">待比较的文本</param>
        /// <param name="other">目标文本</param>
        /// <returns>是否相似</returns>
        public static bool IsSimilarWith(this string text, string other)
        {
            text = text.ToLower();
            other = other.ToLower();
            return text == other || text.StartsWith(other);
        }

        /// <summary>
        /// 检查字符是否为空白字符
        /// </summary>
        /// <param name="c">要检查的字符</param>
        /// <returns>是否为空白字符</returns>
        public static bool IsWhiteSpace(char c)
        {
            return c == ' ' || c == '\t' || c == '\n';
        }

        /// <summary>
        /// 解析参数字符串为参数列表
        /// </summary>
        /// <param name="text">参数文本</param>
        /// <returns>解析后的参数列表</returns>
        public static List<string> ParseParameters(string text)
        {
            return (List<string>)_parseParametersMethod.Invoke(null, new object[] { text });
        }

        #endregion

        #region 几何计算

        /// <summary>
        /// 判断点是否在圆内
        /// </summary>
        /// <param name="x">点的X坐标</param>
        /// <param name="y">点的Y坐标</param>
        /// <param name="cx">圆心X坐标</param>
        /// <param name="cy">圆心Y坐标</param>
        /// <param name="r">圆的半径</param>
        /// <returns>点是否在圆内</returns>
        public static bool IsPointInCircle(int x, int y, int cx, int cy, double r)
        {
            // 使用平方距离比较避免开方运算，提高性能
            return (cx - x) * (cx - x) + (cy - y) * (cy - y) <= r * r;
        }

        /// <summary>
        /// 圆结构体，用于几何计算
        /// </summary>
        public struct Circle
        {
            /// <summary>
            /// 圆心坐标
            /// </summary>
            public Point Center { get; init; }

            /// <summary>
            /// 圆半径
            /// </summary>
            public double R { get; init; }

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="point">圆心坐标</param>
            /// <param name="r">半径</param>
            public Circle(Point point, double r)
            {
                Center = point;
                R = r;
            }

            /// <summary>
            /// 判断点与圆的位置关系
            /// </summary>
            /// <param name="point">待判断的点</param>
            /// <returns>-1:圆外, 0:圆上, 1:圆内</returns>
            public int Is(Point point)
            {
                double distanceSquared = (Center.X - point.X) * (Center.X - point.X) +
                                       (Center.Y - point.Y) * (Center.Y - point.Y);
                double radiusSquared = R * R;

                if (distanceSquared > radiusSquared) return -1;
                else if (Math.Abs(distanceSquared - radiusSquared) < 1e-10) return 0;
                else return 1;
            }
        }

        /// <summary>
        /// 计算两点之间的距离
        /// </summary>
        /// <param name="value1">第一个点</param>
        /// <param name="value2">第二个点</param>
        /// <returns>距离</returns>
        public static float Distance(Point value1, Microsoft.Xna.Framework.Vector2 value2)
        {
            float deltaX = value1.X - value2.X;
            float deltaY = value1.Y - value2.Y;
            return (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        /// <summary>
        /// 计算两点之间的距离（引用版本）
        /// </summary>
        /// <param name="value1">第一个点</param>
        /// <param name="value2">第二个点</param>
        /// <param name="result">输出距离</param>
        public static void Distance(ref Point value1, ref Microsoft.Xna.Framework.Vector2 value2, out float result)
        {
            float deltaX = value1.X - value2.X;
            float deltaY = value1.Y - value2.Y;
            result = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        /// <summary>
        /// 判断两个向量是否在指定距离内
        /// </summary>
        /// <param name="p1">第一个向量</param>
        /// <param name="p2">第二个向量</param>
        /// <param name="distance">最大距离</param>
        /// <returns>是否在范围内</returns>
        public static bool IsNearBy(Microsoft.Xna.Framework.Vector2 p1, Microsoft.Xna.Framework.Vector2 p2, float distance)
        {
            float squaredDistance = Microsoft.Xna.Framework.Vector2.DistanceSquared(p1, p2);
            float squaredMax = distance * distance;
            return squaredDistance <= squaredMax;
        }

        /// <summary>
        /// 判断向量和点是否在指定距离内
        /// </summary>
        /// <param name="p1">向量</param>
        /// <param name="p2">点</param>
        /// <param name="distance">最大距离</param>
        /// <returns>是否在范围内</returns>
        public static bool IsNearBy(Microsoft.Xna.Framework.Vector2 p1, Point p2, float distance)
            => IsNearBy(p1, new Microsoft.Xna.Framework.Vector2(p2.X, p2.Y), distance);

        /// <summary>
        /// 判断玩家是否在指定物块坐标附近
        /// </summary>
        /// <param name="player">玩家</param>
        /// <param name="tilePosition">物块坐标</param>
        /// <param name="distance">最大距离（默认150）</param>
        /// <returns>是否在范围内</returns>
        public static bool IsNearBy(this BPlayer player, Point tilePosition, float distance = 150)
            => IsNearBy(player.TilePositionV, new Microsoft.Xna.Framework.Vector2(tilePosition.X, tilePosition.Y), distance);

        #endregion

        #region 序列化和数据处理

        /// <summary>
        /// 将JSON字符串反序列化为指定类型的对象
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="text">JSON字符串</param>
        /// <returns>反序列化后的对象</returns>
        public static T DeserializeJson<T>(this string text)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(text);
            }
            catch (Exception ex)
            {
                BLog.Error(ex);
                return default;
            }
        }

        /// <summary>
        /// 将对象序列化为JSON字符串
        /// </summary>
        /// <param name="obj">要序列化的对象</param>
        /// <param name="format">是否格式化输出</param>
        /// <returns>JSON字符串</returns>
        public static string SerializeToJson(this object obj, bool format = false)
        {
            try
            {
                return JsonConvert.SerializeObject(obj, format ? Formatting.Indented : Formatting.None);
            }
            catch (Exception ex)
            {
                BLog.Error(ex);
                return default;
            }
        }

        /// <summary>
        /// 将字节数组反序列化为指定类型的对象
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="data">字节数组</param>
        /// <returns>反序列化后的对象</returns>
        public static T DeserializeBytes<T>(this byte[] data)
        {
            try
            {
                return Bssom.Serializer.BssomSerializer.Deserialize<T>(data);
            }
            catch (Exception ex)
            {
                BLog.Error(ex);
                return default;
            }
        }

        /// <summary>
        /// 将对象序列化为字节数组
        /// </summary>
        /// <param name="data">要序列化的对象</param>
        /// <returns>序列化后的字节数组</returns>
        public static byte[] SerializeToBytes(this object data)
        {
            try
            {
                return Bssom.Serializer.BssomSerializer.Serialize(data);
            }
            catch (Exception ex)
            {
                BLog.Error(ex);
                return default;
            }
        }

        /// <summary>
        /// 压缩字节数组
        /// </summary>
        /// <param name="data">要压缩的字节数组</param>
        /// <returns>压缩后的字节数组</returns>
        public static byte[] CompressBytes(this byte[] data)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                using (var deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress))
                {
                    deflateStream.Write(data, 0, data.Length);
                }
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                BLog.Error(ex);
#pragma warning disable S1168 // Empty arrays and collections should be returned instead of null
                return null;
#pragma warning restore S1168 // Empty arrays and collections should be returned instead of null
            }
        }

        /// <summary>
        /// 解压缩字节数组
        /// </summary>
        /// <param name="data">要解压缩的字节数组</param>
        /// <returns>解压缩后的字节数组</returns>
        public static byte[] DecompressBytes(this byte[] data)
        {
            try
            {
                using var decompressedStream = new MemoryStream();
                using var compressStream = new MemoryStream(data);
                using var deflateStream = new DeflateStream(compressStream, CompressionMode.Decompress);
                deflateStream.CopyTo(decompressedStream);
                return decompressedStream.ToArray();
            }
            catch (Exception ex)
            {
                BLog.Error(ex);
#pragma warning disable S1168 // Empty arrays and collections should be returned instead of null
                return null;
#pragma warning restore S1168 // Empty arrays and collections should be returned instead of null
            }
        }

        #endregion

        #region 游戏世界数据

        /// <summary>
        /// 获取当前世界数据
        /// </summary>
        /// <param name="ssc">服务器端角色（可选）</param>
        /// <returns>世界数据对象</returns>
        public static WorldData GetCurrentWorldData(bool? ssc = null)
        {
            var worldInfo = new WorldData
            {
                Time = (int)Main.time,
                GameMode = (byte)Main.GameMode
            };
            ProtocolBitsByte bb3 = 0;
            bb3[0] = Main.dayTime;
            bb3[1] = Main.bloodMoon;
            bb3[2] = Main.eclipse;
            worldInfo.DayAndMoonInfo = bb3;
            worldInfo.MoonPhase = (byte)Main.moonPhase;
            worldInfo.MaxTileX = (short)Main.maxTilesX;
            worldInfo.MaxTileY = (short)Main.maxTilesY;
            worldInfo.SpawnX = (short)Main.spawnTileX;
            worldInfo.SpawnY = (short)Main.spawnTileY;
            worldInfo.WorldSurface = (short)Main.worldSurface;
            worldInfo.RockLayer = (short)Main.rockLayer;
            worldInfo.WorldID = Main.worldID;
            worldInfo.WorldName = Main.worldName;
            worldInfo.WorldUniqueID = Main.ActiveWorldFileData.UniqueId;
            worldInfo.WorldGeneratorVersion = Main.ActiveWorldFileData.WorldGeneratorVersion;
            worldInfo.MoonType = (byte)Main.moonType;
            worldInfo.TreeBackground = (byte)WorldGen.treeBG1;
            worldInfo.CorruptionBackground = (byte)WorldGen.corruptBG;
            worldInfo.JungleBackground = (byte)WorldGen.jungleBG;
            worldInfo.SnowBackground = (byte)WorldGen.snowBG;
            worldInfo.HallowBackground = (byte)WorldGen.hallowBG;
            worldInfo.CrimsonBackground = (byte)WorldGen.crimsonBG;
            worldInfo.DesertBackground = (byte)WorldGen.desertBG;
            worldInfo.OceanBackground = (byte)WorldGen.oceanBG;
            worldInfo.IceBackStyle = (byte)Main.iceBackStyle;
            worldInfo.JungleBackStyle = (byte)Main.jungleBackStyle;
            worldInfo.HellBackStyle = (byte)Main.hellBackStyle;
            worldInfo.WindSpeedSet = Main.windSpeedCurrent;
            worldInfo.CloudNumber = (byte)Main.numClouds;
            worldInfo.Tree1 = Main.treeX[0];
            worldInfo.Tree2 = Main.treeX[1];
            worldInfo.Tree3 = Main.treeX[2];
            worldInfo.TreeStyle1 = (byte)Main.treeStyle[0];
            worldInfo.TreeStyle2 = (byte)Main.treeStyle[1];
            worldInfo.TreeStyle3 = (byte)Main.treeStyle[2];
            worldInfo.TreeStyle4 = (byte)Main.treeStyle[3];
            worldInfo.CaveBack1 = (byte)Main.caveBackX[0];
            worldInfo.CaveBack2 = (byte)Main.caveBackX[1];
            worldInfo.CaveBack3 = (byte)Main.caveBackX[2];
            worldInfo.CaveBackStyle1 = (byte)Main.caveBackStyle[0];
            worldInfo.CaveBackStyle2 = (byte)Main.caveBackStyle[1];
            worldInfo.CaveBackStyle3 = (byte)Main.caveBackStyle[2];
            worldInfo.CaveBackStyle4 = (byte)Main.caveBackStyle[3];
            if (!Main.raining)
            {
                worldInfo.Rain = 0;
            }
            else
                worldInfo.Rain = Main.maxRaining;
            ProtocolBitsByte bb4 = 0;
            bb4[0] = WorldGen.shadowOrbSmashed;
            bb4[1] = NPC.downedBoss1;
            bb4[2] = NPC.downedBoss2;
            bb4[3] = NPC.downedBoss3;
            bb4[4] = Main.hardMode;
            bb4[5] = NPC.downedClown;
            bb4[6] = ssc ?? Main.ServerSideCharacter;
            bb4[7] = NPC.downedPlantBoss;
            worldInfo.EventInfo1 = bb4;
            ProtocolBitsByte bb5 = 0;
            bb5[0] = NPC.downedMechBoss1;
            bb5[1] = NPC.downedMechBoss2;
            bb5[2] = NPC.downedMechBoss3;
            bb5[3] = NPC.downedMechBossAny;
            bb5[4] = Main.cloudBGActive >= 1f;
            bb5[5] = WorldGen.crimson;
            bb5[6] = Main.pumpkinMoon;
            bb5[7] = Main.snowMoon;
            worldInfo.EventInfo2 = bb5;
            ProtocolBitsByte bb6 = 0;
            bb6[0] = Main.expertMode;
            bb6[1] = Main.IsFastForwardingTime();//Main.fastForwardTime;
            bb6[2] = Main.slimeRain;
            bb6[3] = NPC.downedSlimeKing;
            bb6[4] = NPC.downedQueenBee;
            bb6[5] = NPC.downedFishron;
            bb6[6] = NPC.downedMartians;
            bb6[7] = NPC.downedAncientCultist;
            worldInfo.EventInfo3 = bb6;
            ProtocolBitsByte bb7 = 0;
            bb7[0] = NPC.downedMoonlord;
            bb7[1] = NPC.downedHalloweenKing;
            bb7[2] = NPC.downedHalloweenTree;
            bb7[3] = NPC.downedChristmasIceQueen;
            bb7[4] = NPC.downedChristmasSantank;
            bb7[5] = NPC.downedChristmasTree;
            bb7[6] = NPC.downedGolemBoss;
            bb7[7] = BirthdayParty.PartyIsUp;
            worldInfo.EventInfo4 = bb7;
            ProtocolBitsByte bb8 = 0;
            bb8[0] = NPC.downedPirates;
            bb8[1] = NPC.downedFrost;
            bb8[2] = NPC.downedGoblins;
            bb8[3] = Sandstorm.Happening;
            bb8[4] = DD2Event.Ongoing;
            bb8[5] = DD2Event.DownedInvasionT1;
            bb8[6] = DD2Event.DownedInvasionT2;
            bb8[7] = DD2Event.DownedInvasionT3;
            worldInfo.EventInfo5 = bb8;
            worldInfo.InvasionType = (sbyte)Main.invasionType;
            return worldInfo;
        }
        public static TileSquare GetSquareData(int x, int y, int size, TileChangeType type = TileChangeType.None)
            => GetSquareData(x, y, size, size, type);
        public static TileSquare GetSquareData(int x, int y, int width, int height, TileChangeType type = TileChangeType.None)
        {
            var data = new TileSquare()
            {
                Data = new()
                {
                    ChangeType = type,
                    Height = (byte)height,
                    Width = (byte)width,
                    TilePosX = (short)x,
                    TilePosY = (short)y,
                    Tiles = new SimpleTileData[width, height]
                }
            };

            for (int tempX = x; tempX < x + width; tempX++)
            {
                for (int tempY = y; tempY < y + height; tempY++)
                {
                    var tile = Main.tile[tempX, tempY];
                    ProtocolBitsByte bb1 = 0;
                    ProtocolBitsByte bb2 = 0;

                    bb1[0] = tile.active();
                    bb1[2] = (tile.wall > 0);
                    bb1[3] = (tile.liquid > 0);
                    bb1[4] = tile.wire();
                    bb1[5] = tile.halfBrick();
                    bb1[6] = tile.actuator();
                    bb1[7] = tile.inActive();

                    bb2[0] = tile.wire2();
                    bb2[1] = tile.wire3();
                    bb2[2] = tile.active();
                    bb2[3] = tile.wall > 0;
                    bb2 += (byte)(tile.slope() << 4);
                    bb2[7] = tile.wire4();
                    data.Data.Tiles[tempX - x, tempY - y] = (new()
                    {
                        Flags1 = bb1,
                        Flags2 = bb2,
                        FrameX = tile.frameX,
                        FrameY = tile.frameY,
                        Liquid = tile.liquid,
                        LiquidType = tile.liquid,
                        TileColor = tile.color(),
                        TileType = tile.type,
                        WallColor = tile.wallColor(),
                        WallType = tile.wall,
                    });
                }
            }
            return data;
        }

        #endregion

        #region 玩家和包处理

        /// <summary>
        /// 获取Boss玩家对象
        /// </summary>
        /// <param name="plr">TSPlayer对象</param>
        /// <returns>BPlayer对象</returns>
        public static BPlayer GetBPlayer(this TSPlayer plr)
            => plr.GetData<BPlayer>("Boss.BPlayer") ?? new(plr);

        /// <summary>
        /// 序列化数据包（带缓存优化）
        /// </summary>
        /// <param name="packet">要序列化的数据包</param>
        /// <returns>序列化后的字节数组</returns>
        public static byte[] SerializePacket(this Packet packet)
        {
            // 尝试从缓存获取
            if (_packetCache.TryGetValue(packet, out byte[] cachedData))
                return cachedData;

            // 序列化并缓存
            var data = PacketHandler.Serializer.Serialize(packet);
            _packetCache.Set(packet, data);
            return data;
        }

        /// <summary>
        /// 直接序列化数据包（不使用缓存，适用于一次性数据包）
        /// </summary>
        /// <param name="packet">要序列化的数据包</param>
        /// <returns>序列化后的字节数组</returns>
        public static byte[] SerializePacketDirect(this Packet packet)
        {
            return PacketHandler.Serializer.Serialize(packet);
        }
        /// <summary>
        /// 杀死指定的同步弹幕
        /// </summary>
        /// <param name="proj">要杀死的弹幕</param>
        public static void Kill(this SyncProjectile proj)
        {
            var plr = TShock.Players[proj.PlayerSlot]?.GetBPlayer();
            plr?.SendPacket(new KillProjectile()
            {
                ProjSlot = proj.ProjSlot,
                PlayerSlot = proj.PlayerSlot
            });
        }

        /// <summary>
        /// 使指定的同步弹幕变为非活跃状态
        /// </summary>
        /// <param name="proj">要设为非活跃的弹幕</param>
        public static void Inactive(this SyncProjectile proj)
        {
            var plr = TShock.Players[proj.PlayerSlot]?.GetBPlayer();
            var oldType = proj.ProjType;
            proj.ProjType = 0;
            plr?.SendPacket(proj);
            proj.ProjType = oldType;
        }

        /// <summary>
        /// 发送数据包给指定玩家
        /// </summary>
        /// <param name="packet">要发送的数据包</param>
        /// <param name="plr">目标玩家</param>
        public static void SendTo(this Packet packet, BPlayer plr)
            => plr.SendPacket(packet);

        /// <summary>
        /// 发送数据包给所有在线玩家（可排除指定玩家）
        /// </summary>
        /// <param name="packet">要发送的数据包</param>
        /// <param name="ignore">要排除的玩家数组</param>
        public static void SendPacketToAll(this Packet packet, params BPlayer[] ignore)
            => BInfo.OnlinePlayers.Where(p => !(ignore?.Contains(p) == true))
                .ForEach(p => p.SendPacket(packet));

        /// <summary>
        /// 发送数据包给所有在线玩家（可排除指定索引的玩家）
        /// </summary>
        /// <param name="packet">要发送的数据包</param>
        /// <param name="ignoreIndex">要排除的玩家索引</param>
        public static void SendPacketToAll(this Packet packet, int ignoreIndex)
            => BInfo.OnlinePlayers.Where(p => p.Index != ignoreIndex)
                .ForEach(p => p.SendPacket(packet));

        /// <summary>
        /// 发送多个数据包给所有在线玩家（可排除指定玩家）
        /// </summary>
        /// <param name="packets">要发送的数据包集合</param>
        /// <param name="ignore">要排除的玩家数组</param>
        public static void SendPacketsToAll(this IEnumerable<Packet> packets, params BPlayer[] ignore)
            => BInfo.OnlinePlayers.Where(p => !(ignore?.Contains(p) == true))
                .ForEach(p => p.SendPackets(packets));

        /// <summary>
        /// 发送多个数据包给指定玩家
        /// </summary>
        /// <typeparam name="T">数据包类型</typeparam>
        /// <param name="packets">要发送的数据包集合</param>
        /// <param name="plr">目标玩家</param>
        public static void SendPacketsTo<T>(this IEnumerable<T> packets, BPlayer plr) where T : Packet
            => plr.SendPackets(packets);
        public static void SendRawDataDirect(this TSPlayer plr, ReadOnlyMemory<byte> data)
        {
            if (plr.Client.Socket is BossSocket socket)
            {
                socket.AsyncSend(data, plr.Client.ServerWriteCallBack, BossSocket._directSendFlag);
            }
            else
            {
                plr.Client.Socket.AsyncSend(data.ToArray(), 0, data.Length, plr.Client.ServerWriteCallBack, BossSocket._directSendFlag);
            }
        }
        public static void SendRawDataDirect(this TSPlayer plr, byte[] data)
        {
            if (plr.Client.Socket is BossSocket socket)
            {
                socket.AsyncSend(data, plr.Client.ServerWriteCallBack, BossSocket._directSendFlag);
            }
            else
            {
                plr.Client.Socket.AsyncSend(data, 0, data.Length, plr.Client.ServerWriteCallBack, BossSocket._directSendFlag);
            }
        }

        /// <summary>
        /// 获取数据包集合的字节数据（优化版本）
        /// </summary>
        /// <param name="packets">数据包集合</param>
        /// <returns>合并后的字节数组</returns>
        public static byte[] GetPacketsByteData(this IEnumerable<Packet> packets)
        {
            var packetList = packets.ToList();
            if (packetList.Count == 0)
                return [];

            if (packetList.Count == 1)
                return packetList[0].SerializePacket();

            return GetPacketsByteDataBatch(packetList);
        }

        /// <summary>
        /// 批量处理数据包字节数据（内存优化）
        /// </summary>
        /// <param name="packets">数据包列表</param>
        /// <returns>合并后的字节数组</returns>
        private static byte[] GetPacketsByteDataBatch(List<Packet> packets)
        {
            // 智能预估总大小
            var estimatedSize = GetEstimatedPacketSize(packets);

            // 方案1：预先序列化所有包来计算准确大小（适用于小批量）
            if (packets.Count <= 10)
            {
                return GetPacketsByteDataPrecise(packets);
            }

            // 方案2：使用动态扩容的MemoryStream（适用于大批量）
            return GetPacketsByteDataDynamic(packets, estimatedSize);
        }

        /// <summary>
        /// 精确计算大小的批量处理（适用于小批量）
        /// </summary>
        /// <param name="packets">数据包列表</param>
        /// <returns>合并后的字节数组</returns>
        private static byte[] GetPacketsByteDataPrecise(List<Packet> packets)
        {
            // 先序列化所有包获取准确大小
            var serializedPackets = new List<byte[]>(packets.Count);
            var totalSize = 0;

            foreach (var packet in packets)
            {
                var data = packet.SerializePacket();
                serializedPackets.Add(data);
                totalSize += data.Length;
            }

            // 使用准确大小创建最终数组
            var result = new byte[totalSize];
            var offset = 0;

            foreach (var data in serializedPackets)
            {
                Buffer.BlockCopy(data, 0, result, offset, data.Length);
                offset += data.Length;
            }

            return result;
        }

        /// <summary>
        /// 动态扩容的批量处理（适用于大批量）
        /// </summary>
        /// <param name="packets">数据包列表</param>
        /// <param name="estimatedSize">预估大小</param>
        /// <returns>合并后的字节数组</returns>
        private static byte[] GetPacketsByteDataDynamic(List<Packet> packets, int estimatedSize)
        {
            using var stream = new MemoryStream(estimatedSize);
            var expansionCount = 0;
            var originalCapacity = stream.Capacity;

            foreach (var packet in packets)
            {
                var data = packet.SerializePacket();
                var beforeCapacity = stream.Capacity;
                stream.Write(data, 0, data.Length);

                // 监控扩容情况用于优化预估算法
                if (stream.Capacity > beforeCapacity)
                {
                    expansionCount++;
                }
            }

            // 记录扩容统计用于后续优化
            UpdateSizeEstimationStats(packets.Count, (int)stream.Length, originalCapacity, expansionCount);

            return stream.ToArray();
        }

        /// <summary>
        /// 简单预估数据包大小
        /// </summary>
        /// <param name="packets">数据包列表</param>
        /// <returns>预估的总大小</returns>
        private static int GetEstimatedPacketSize(List<Packet> packets)
        {
            if (packets.Count == 0) return 0;

            // 使用保守的固定估计 + 较大的缓冲区
            // 每个包平均估计128字节，添加50%缓冲区
            var baseEstimate = packets.Count * 128;
            return (int)(baseEstimate * 1.5);
        }

        /// <summary>
        /// 更新大小预估统计（用于优化预估算法）
        /// </summary>
        /// <param name="packetCount">包数量</param>
        /// <param name="actualSize">实际大小</param>
        /// <param name="estimatedSize">预估大小</param>
        /// <param name="expansionCount">扩容次数</param>
        private static void UpdateSizeEstimationStats(int packetCount, int actualSize, int estimatedSize, int expansionCount)
        {
            // 这里可以实现统计逻辑来优化预估算法
            // 例如记录不同包数量下的平均大小，用于改进GetEstimatedPacketSize方法
            if (expansionCount > 2)
            {
                // 如果扩容次数过多，可以记录日志用于后续优化
                BLog.DEBUG($"数据包批量处理发生多次扩容: 包数量={packetCount}, 实际大小={actualSize}, 预估大小={estimatedSize}, 扩容次数={expansionCount}");
            }
        }

        /// <summary>
        /// 清理包缓存（供手动调用）
        /// </summary>
        public static void ClearPacketCache()
        {
            // 触发缓存清理
            _packetCache.GetType()
                .GetMethod("CleanupExpiredItems", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.Invoke(_packetCache, null);
        }

        #endregion

        #region 消息发送

        /// <summary>
        /// 向玩家发送消息
        /// </summary>
        /// <param name="plr">目标玩家</param>
        /// <param name="msg">消息内容</param>
        /// <param name="color">消息颜色（默认白色）</param>
        public static void SendMsg(this TSPlayer plr, object msg, Color color = default)
        {
            color = color == default ? Color.White : color;
            plr?.SendMessage(msg.ToString(), color); // TODO: 根据玩家状态改变前缀
        }

        /// <summary>
        /// 发送战斗文本消息
        /// </summary>
        /// <param name="msg">消息内容</param>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="color">文本颜色（默认白色）</param>
        /// <param name="randomPosition">是否随机位置偏移</param>
        public static void SendCombatMessage(string msg, float x, float y, Color color = default, bool randomPosition = true)
        {
            color = color == default ? Color.White : color;
            Random random = new();
            TSPlayer.All.SendData(PacketTypes.CreateCombatTextExtended, msg, (int)color.PackedValue,
                x + (randomPosition ? random.Next(-75, 75) : 0),
                y + (randomPosition ? random.Next(-50, 50) : 0));
        }

        #endregion

        #region 命令处理
        /// <summary>
        /// 处理玩家命令
        /// </summary>
        /// <param name="player">执行命令的玩家</param>
        /// <param name="text">命令文本</param>
        /// <param name="ignorePerm">是否忽略权限检查</param>
        /// <returns>命令是否成功处理</returns>
        public static bool HandleCommand(TSPlayer player, string text, bool ignorePerm)
        {
            if (Internal_ParseCmd(text, out var cmdText, out var cmdName, out var args, out var silent))
            {
                HandleCommandDirect(player, cmdText, cmdName, args, silent, ignorePerm);
                return true;
            }
            else
                player.SendErrorMessage("键入的指令无效；使用 {0}help 查看有效指令。", Commands.Specifier);
            return false;
        }

        /// <summary>
        /// 内部命令解析方法
        /// </summary>
        /// <param name="text">原始命令文本</param>
        /// <param name="cmdText">解析后的命令文本</param>
        /// <param name="cmdName">命令名称</param>
        /// <param name="args">命令参数</param>
        /// <param name="silent">是否为静默命令</param>
        /// <returns>解析是否成功</returns>
        private static bool Internal_ParseCmd(string text, out string cmdText, out string cmdName, out List<string> args, out bool silent)
        {
            cmdText = text[1..];
            var cmdPrefix = text[0].ToString();
            silent = cmdPrefix == Commands.SilentSpecifier;

            var index = -1;
            for (var i = 0; i < cmdText.Length; i++)
            {
                if (IsWhiteSpace(cmdText[i]))
                {
                    index = i;
                    break;
                }
            }
            if (index == 0) // Space after the command specifier should not be supported
            {
                args = null;
                cmdName = null;
                return false;
            }
            cmdName = index < 0 ? cmdText.ToLower() : cmdText[..index].ToLower();

            args = index < 0 ?
                [] :
               ParseParameters(cmdText[index..]);
            return true;
        }

        /// <summary>
        /// 直接处理命令（跳过解析步骤）
        /// </summary>
        /// <param name="player">执行命令的玩家</param>
        /// <param name="cmdText">命令文本</param>
        /// <param name="cmdName">命令名称</param>
        /// <param name="args">命令参数</param>
        /// <param name="silent">是否为静默命令</param>
        /// <param name="ignorePerm">是否忽略权限检查</param>
        /// <returns>命令是否成功处理</returns>
        public static bool HandleCommandDirect(TSPlayer player, string cmdText, string cmdName, List<string> args, bool silent, bool ignorePerm)
        {
            CommandPlaceholder.Placeholders.Where(p => p.Match(cmdText))
                .ForEach(p =>
                {
                    cmdText = p.Replace(new(player.GetBPlayer()), cmdText);
                    if (args.Count != 0)
                        for (int i = 0; i < args.Count; i++)
                        {
                            if (p.Match(args[i]))
                                args[i] = p.Replace(new(player.GetBPlayer()), args[i]);
                        }
                });
            var cmds = Commands.ChatCommands.FindAll(x => x.HasAlias(cmdName));

            lock (player)
            {
                try
                {
                    if (ignorePerm)
                        player.SetData("BossFramework.IgnorePerm", true);

                    if (cmds.Count == 0)
                    {
                        if (player.AwaitingResponse.ContainsKey(cmdName))
                        {
                            Action<CommandArgs> call = player.AwaitingResponse[cmdName];
                            player.AwaitingResponse.Remove(cmdName);
                            call(new CommandArgs(cmdText, player, args));
                            return true;
                        }
                        player.SendErrorMessage("键入的指令无效；使用 {0}help 查看有效指令。", Commands.Specifier);
                        return true;
                    }
                    foreach (var cmd in cmds)
                    {
                        cmd.CommandDelegate?.Invoke(new CommandArgs(cmdText, silent, player, args));
                    }
                    BLog.Info($"{player.Name} 使用命令 {TShock.Config.Settings.CommandSpecifier}{cmdText}");
                }
                catch (Exception ex)
                {
                    BLog.Warn(ex);
                    player.SendErrorMessage($"指令执行失败");
                }
                finally
                {
                    player.RemoveData("BossFramework.IgnorePerm");
                }
                return true;
            }
        }

        #endregion

        #region 类型转换扩展

        /// <summary>
        /// 将ShortPosition转换为Point
        /// </summary>
        /// <param name="position">源位置</param>
        /// <returns>转换后的Point</returns>
        public static Point ToPoint(this ShortPosition position)
            => new(position.X, position.Y);

        /// <summary>
        /// 将ShortPosition转换为Point16
        /// </summary>
        /// <param name="position">源位置</param>
        /// <returns>转换后的Point16</returns>
        public static Terraria.DataStructures.Point16 ToPoint16(this ShortPosition position)
            => new(position.X, position.Y);

        /// <summary>
        /// 将Point转换为ShortPosition
        /// </summary>
        /// <param name="position">源位置</param>
        /// <returns>转换后的ShortPosition</returns>
        public static ShortPosition ToShortPosition(this Point position)
            => new((short)position.X, (short)position.Y);

        /// <summary>
        /// 将Point16转换为ShortPosition
        /// </summary>
        /// <param name="position">源位置</param>
        /// <returns>转换后的ShortPosition</returns>
        public static ShortPosition ToShortPosition(this Terraria.DataStructures.Point16 position)
            => new((short)position.X, (short)position.Y);

        /// <summary>
        /// 将ItemData转换为Terraria Item
        /// </summary>
        /// <param name="item">物品数据</param>
        /// <returns>Terraria物品对象</returns>
        public static Item Get(this ItemData item)
        {
            var terrariaItem = new Item();
            terrariaItem.SetDefaults(item.ItemID);
            terrariaItem.stack = item.Stack;
            terrariaItem.prefix = item.Prefix;
            return terrariaItem;
        }

        /// <summary>
        /// 将Terraria Item转换为ItemData
        /// </summary>
        /// <param name="item">Terraria物品对象</param>
        /// <returns>物品数据</returns>
        public static ItemData Get(this Item item)
            => new()
            {
                ItemID = (short)item.type,
                Prefix = item.prefix,
                Stack = (short)item.stack
            };

        /// <summary>
        /// 将Microsoft.Xna.Framework.Vector2转换为TrProtocol.Models.Vector2
        /// </summary>
        /// <param name="vector">源向量</param>
        /// <returns>转换后的向量</returns>
        public static TrProtocol.Models.Vector2 Get(this Microsoft.Xna.Framework.Vector2 vector)
            => new(vector.X, vector.Y);

        /// <summary>
        /// 将TrProtocol.Models.Vector2转换为Microsoft.Xna.Framework.Vector2
        /// </summary>
        /// <param name="vector">源向量</param>
        /// <returns>转换后的向量</returns>
        public static Microsoft.Xna.Framework.Vector2 Get(this TrProtocol.Models.Vector2 vector)
            => new(vector.X, vector.Y);
        #endregion

        #region 实体转换

        /// <summary>
        /// 将TrProtocol TileEntity转换为Terraria TileEntity
        /// </summary>
        /// <param name="entity">协议实体</param>
        /// <returns>Terraria实体</returns>
        public static Terraria.DataStructures.TileEntity ToTrTileEntity(this TileEntity entity)
        {
            var result = entity switch
            {
                TrProtocol.Models.TileEntities.TEDisplayDoll e => new TEDisplayDoll
                {
                    Position = e.Position.ToPoint16(),
                    _dyes = e.Dyes.Select(i => i.Get()).ToArray(),
                    _items = e.Items.Select(i => i.Get()).ToArray(),
                },
                TrProtocol.Models.TileEntities.TEFoodPlatter e => new TEFoodPlatter()
                {
                    item = e.Item.Get(),
                    Position = e.Position.ToPoint16()
                },
                TrProtocol.Models.TileEntities.TEHatRack e => new TEHatRack()
                {
                    ID = e.ID,
                    Position = e.Position.ToPoint16(),
                    _dyes = e.Dyes.Select(i => i.Get()).ToArray(),
                    _items = e.Items.Select(i => i.Get()).ToArray(),
                },
                TrProtocol.Models.TileEntities.TEItemFrame e => new TEItemFrame()
                {
                    ID = e.ID,
                    Position = e.Position.ToPoint16(),
                    item = e.Item.Get()
                },
                TrProtocol.Models.TileEntities.TELogicSensor e => new TELogicSensor()
                {
                    ID = e.ID,
                    Position = e.Position.ToPoint16(),
                    logicCheck = (TELogicSensor.LogicCheckType)e.LogicCheck,
                    On = e.On
                },
                TrProtocol.Models.TileEntities.TETeleportationPylon e => new TETeleportationPylon()
                {
                    ID = e.ID,
                    Position = e.Position.ToPoint16()
                },
                TrProtocol.Models.TileEntities.TETrainingDummy e => new TETrainingDummy()
                {
                    ID = e.ID,
                    Position = e.Position.ToPoint16(),
                    npc = e.NPC
                },
                TrProtocol.Models.TileEntities.TEWeaponsRack e => (Terraria.DataStructures.TileEntity)new TEWeaponsRack()
                {
                    ID = e.ID,
                    Position = e.Position.ToPoint16(),
                    item = e.Item.Get()
                },
                _ => null,
            };
            result.type = (byte)entity.EntityType;
            result.Position = entity.Position.ToPoint16();
            result.ID = entity.ID;
            return result;
        }
        /// <summary>
        /// 将Terraria TileEntity转换为TrProtocol TileEntity
        /// </summary>
        /// <param name="entity">Terraria实体</param>
        /// <returns>协议实体</returns>
        public static TileEntity ToProtocalTileEntity(this Terraria.DataStructures.TileEntity entity)
        {
            var result = entity switch
            {
                TEDisplayDoll e => new TrProtocol.Models.TileEntities.TEDisplayDoll
                {
                    Dyes = e._dyes.Select(i => i.Get()).ToArray(),
                    Items = e._items.Select(i => i.Get()).ToArray(),
                },
                TEFoodPlatter e => new TrProtocol.Models.TileEntities.TEFoodPlatter()
                {
                    Item = e.item.Get(),
                    Position = e.Position.ToShortPosition()
                },
                TEHatRack e => new TrProtocol.Models.TileEntities.TEHatRack()
                {
                    Dyes = e._dyes.Select(i => i.Get()).ToArray(),
                    Items = e._items.Select(i => i.Get()).ToArray(),
                },
                TEItemFrame e => new TrProtocol.Models.TileEntities.TEItemFrame()
                {
                    Item = e.item.Get()
                },
                TELogicSensor e => new TrProtocol.Models.TileEntities.TELogicSensor()
                {
                    LogicCheck = (LogicCheckType)e.logicCheck,
                    On = e.On
                },
                TETeleportationPylon e => new TrProtocol.Models.TileEntities.TETeleportationPylon()
                {
                },
                TETrainingDummy e => new TrProtocol.Models.TileEntities.TETrainingDummy()
                {
                    NPC = e.npc
                },
                TEWeaponsRack e => (TrProtocol.Models.TileEntity)new TrProtocol.Models.TileEntities.TEWeaponsRack()
                {
                    Item = e.item.Get()
                },
                _ => null,
            };
            result.Position = entity.Position.ToShortPosition();
            result.ID = entity.ID;
            return result;
        }

        #endregion

    }
}
