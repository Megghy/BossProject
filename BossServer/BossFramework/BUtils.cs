using BossFramework.BModels;
using BossFramework.BNet;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Terraria;
using Terraria.GameContent.Events;
using TrProtocol;
using TrProtocol.Packets;
using TShockAPI;
using Color = Microsoft.Xna.Framework.Color;
using ProtocolBitsByte = TrProtocol.Models.ProtocolBitsByte;

namespace BossFramework
{
    public static class BUtils
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            if (!source.Any())
                return;
            int count = 0;
            foreach (T obj in source)
            {
                action(obj, count);
                count++;
            }
        }
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action) => source.ForEach((obj, _) => action(obj));
        public static void ForEach(this int count, Action<int> action)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            for (int i = 0; i < count; i++)
            {
                action(i);
            }
        }
        public static bool IsSimilarWith(this string text, string othor)
        {
            text = text.ToLower();
            othor = othor.ToLower();
            return text == othor || text.StartsWith(othor);
        }
        public static bool IsPointInCircle(int x, int y, int cx, int cy, double r)
        {
            //到圆心的距离 是否大于半径。半径是R  
            //如O(x,y)点圆心，任意一点P（x1,y1） （x-x1）*(x-x1)+(y-y1)*(y-y1)>R*R 那么在圆外 反之在圆内

            if ((cx - x) * (cx - x) + (cy - y) * (cy - y) <= r * r)
            {
                return true;        //当前点在圆内
            }
            else
            {
                return false;       //当前点在圆外
            }
        }
        public struct Circle//圆类
        {
            public Circle(Point point, double r)//构造函数
            {
                Center = point;
                R = r;
            }
            public int Is(Point point)//判断函数
            {
                double a = Math.Sqrt((Center.X - point.X) + (Center.Y - point.Y));//点到圆心的距离
                if (a > R) return -1;
                else if (a == R) return 0;
                else return 1;
            }
            public Point Center { get; init; }//圆心坐标
            public double R { get; init; }//圆半径
        }

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
        public static string SerializeToJson(this object obj)
        {
            try
            {
                return JsonConvert.SerializeObject(obj);
            }
            catch (Exception ex)
            {
                BLog.Error(ex);
                return default;
            }
        }
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
        public static byte[] CompressBytes(this byte[] data)
        {
            try
            {
                using (MemoryStream memoryStream = new())
                {
                    using (DeflateStream deflateStream = new(memoryStream, CompressionMode.Compress))
                    {
                        deflateStream.Write(data, 0, data.Length);
                    }
                    return memoryStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                BLog.Error(ex);
#pragma warning disable S1168 // Empty arrays and collections should be returned instead of null
                return null;
#pragma warning restore S1168 // Empty arrays and collections should be returned instead of null
            }
        }

        public static byte[] DecompressBytes(this byte[] data)
        {
            try
            {
                using (MemoryStream decompressedStream = new())
                {
                    using (MemoryStream compressStream = new(data))
                    {
                        using (DeflateStream deflateStream = new(compressStream, CompressionMode.Decompress))
                        {
                            deflateStream.CopyTo(decompressedStream);
                        }
                    }
                    return decompressedStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                BLog.Error(ex);
#pragma warning disable S1168 // Empty arrays and collections should be returned instead of null
                return null;
#pragma warning restore S1168 // Empty arrays and collections should be returned instead of null
            }
        }

        public static WorldData GetCurrentWorldData(bool? ssc = null)
        {
            var worldInfo = new WorldData
            {
                Time = (int)Main.time
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
            bb6[1] = Main.fastForwardTime;
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
        public static BPlayer GetBPlayer(this TSPlayer plr) => plr.GetData<BPlayer>("Boss.BPlayer") ?? BPlayer.Default;
        public static byte[] SerializePacket(this Packet p) => PacketHandler.Serializer.Serialize(p);
        public static void Kill(this SyncProjectile proj)
        {
            var plr = TShock.Players[proj.PlayerSlot]?.GetBPlayer();
            plr?.SendPacket(new KillProjectile()
            {
                ProjSlot = proj.ProjSlot,
                PlayerSlot = proj.PlayerSlot
            });
        }
        public static void Inactive(this SyncProjectile proj)
        {
            var plr = TShock.Players[proj.PlayerSlot]?.GetBPlayer();
            var oldType = proj.ProjType;
            proj.ProjType = 0;
            plr?.SendPacket(proj);
            proj.ProjType = oldType;
        }
        public static void SendTo(this Packet packet, BPlayer plr)
            => plr.SendPacket(packet);

        public static void SendPacketToAll(this Packet packet, params BPlayer[] ignore)
        {
            BInfo.OnlinePlayers.Where(p => !(ignore?.Contains(p) == true))
                .ForEach(p => p.SendPacket(packet));
        }
        public static void SendPacketsToAll(this IEnumerable<Packet> packets, params BPlayer[] ignore)
        {
            var data = packets.GetPacketsByteData();
            BInfo.OnlinePlayers.Where(p => !(ignore?.Contains(p) == true)).ForEach(p => p.SendRawData(data));
        }
        public static void SendPacketsTo(this IEnumerable<Packet> packets, BPlayer plr)
            => plr.SendPackets(packets);
        public static byte[] GetPacketsByteData(this IEnumerable<Packet> packets)
        {
            List<byte> packetData = new();
            packets.TForEach(packet => packetData.AddRange(packet.SerializePacket()));
            return packetData.ToArray();
        }
        public static void SendMsg(this TSPlayer plr, object msg, Color color = default)
        {
            color = color == default ? Color.White : color;
            plr?.SendMessage(msg.ToString(), color); //todo 根据玩家状态改变前缀
        }
        public static void SendCombatMessage(string msg, float x, float y, Color color = default, bool randomPosition = true)
        {
            color = color == default ? Color.White : color;
            Random random = new();
            TSPlayer.All.SendData(PacketTypes.CreateCombatTextExtended, msg, (int)color.PackedValue, x + (randomPosition ? random.Next(-75, 75) : 0), y + (randomPosition ? random.Next(-50, 50) : 0));
        }
    }
}
