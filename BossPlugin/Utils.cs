using BossPlugin.BModels;
using BossPlugin.BNet;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.Events;
using TerrariaUI.Base;
using TerrariaUI.Widgets;
using TrProtocol;
using TrProtocol.Packets;
using TShockAPI;
using BitsByte = TrProtocol.Models.BitsByte;
using Color = Microsoft.Xna.Framework.Color;

namespace BossPlugin
{
    public static class Utils
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
            if (source.Count() < 1)
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
            for (int i = count; i < count; i++)
            {
                action(i);
            }
        }

        public static WorldData GetCurrentWorldData(bool? ssc = null)
        {
            var worldInfo = new WorldData
            {
                Time = (int)Main.time
            };
            BitsByte bb3 = 0;
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
            BitsByte bb4 = 0;
            bb4[0] = WorldGen.shadowOrbSmashed;
            bb4[1] = NPC.downedBoss1;
            bb4[2] = NPC.downedBoss2;
            bb4[3] = NPC.downedBoss3;
            bb4[4] = Main.hardMode;
            bb4[5] = NPC.downedClown;
            bb4[6] = ssc ?? Main.ServerSideCharacter;
            bb4[7] = NPC.downedPlantBoss;
            worldInfo.EventInfo1 = bb4;
            BitsByte bb5 = 0;
            bb5[0] = NPC.downedMechBoss1;
            bb5[1] = NPC.downedMechBoss2;
            bb5[2] = NPC.downedMechBoss3;
            bb5[3] = NPC.downedMechBossAny;
            bb5[4] = Main.cloudBGActive >= 1f;
            bb5[5] = WorldGen.crimson;
            bb5[6] = Main.pumpkinMoon;
            bb5[7] = Main.snowMoon;
            worldInfo.EventInfo2 = bb5;
            BitsByte bb6 = 0;
            bb6[0] = Main.expertMode;
            bb6[1] = Main.fastForwardTime;
            bb6[2] = Main.slimeRain;
            bb6[3] = NPC.downedSlimeKing;
            bb6[4] = NPC.downedQueenBee;
            bb6[5] = NPC.downedFishron;
            bb6[6] = NPC.downedMartians;
            bb6[7] = NPC.downedAncientCultist;
            worldInfo.EventInfo3 = bb6;
            BitsByte bb7 = 0;
            bb7[0] = NPC.downedMoonlord;
            bb7[1] = NPC.downedHalloweenKing;
            bb7[2] = NPC.downedHalloweenTree;
            bb7[3] = NPC.downedChristmasIceQueen;
            bb7[4] = NPC.downedChristmasSantank;
            bb7[5] = NPC.downedChristmasTree;
            bb7[6] = NPC.downedGolemBoss;
            bb7[7] = BirthdayParty.PartyIsUp;
            worldInfo.EventInfo4 = bb7;
            BitsByte bb8 = 0;
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
        public static BPlayer GetBPlayer(this TSPlayer plr) => plr.GetData<BPlayer>("BossPlugin.BPlayer");
        public static byte[] Serialize(this Packet p) => PacketHandler.Serializer.Serialize(p);

        public static void SendEX(this TSPlayer plr, object msg, Color color = default)
        {
            color = color == default ? Color.White : color;
            plr.SendMessage(msg.ToString(), color); //todo 根据玩家状态改变前缀
        }
        public static void SendCombatMessage(string msg, float x, float y, Color color = default, bool randomPosition = true)
        {
            color = color == default ? Color.White : color;
            Random random = new();
            TSPlayer.All.SendData(PacketTypes.CreateCombatTextExtended, msg, (int)color.PackedValue, x + (randomPosition ? random.Next(-75, 75) : 0), y + (randomPosition ? random.Next(-50, 50) : 0));
        }

        #region tui拓展方法
        public static TSPlayer Player(this Touch t) => TShock.Players[t.PlayerIndex];
        public static void UpdateText(this Label l, object text)
        {
            l?.SetText(text.ToString());
            l?.UpdateSelf();
        }
        public static void UpdateTextColor(this Label l, byte id)
        {
            l.LabelStyle.TextColor = id;
            l?.UpdateSelf();
        }
        public static void UpdateTileColor(this VisualObject l, byte id)
        {
            l.Style.TileColor = id;
            l?.UpdateSelf();
        }
        public static void UpdateWallColor(this VisualObject l, byte id)
        {
            l.Style.WallColor = id;
            l?.UpdateSelf();
        }
        public static void UpdateSelf(this VisualObject v) => v.Update().Apply().Draw();
        #endregion
    }
}
