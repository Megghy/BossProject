using BossPlugin.BModels;
using BossPlugin.BNet;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TerrariaUI.Base;
using TerrariaUI.Widgets;
using TrProtocol;
using TShockAPI;

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
