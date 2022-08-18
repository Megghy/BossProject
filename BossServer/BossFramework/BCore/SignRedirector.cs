using BossFramework.BAttributes;
using BossFramework.BModels;
using BossFramework.DB;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TrProtocol;
using TrProtocol.Packets;

namespace BossFramework.BCore
{
    public static class SignRedirector
    {
        public static List<BSign> Signs { get; set; }
        private static List<BSign> _overrideSign { get; set; } = new();

        [AutoPostInit]
        private static void InitSign()
        {
            BLog.DEBUG("初始化标牌重定向");

            Signs = DBTools.GetAll<BSign>().Where(r => r.WorldId == Terraria.Main.worldID).ToList();

            Terraria.Main.sign.Where(s => s != null).ForEach(s =>
            {
                if (!Signs.Exists(sign => sign.X == s.x && sign.Y == s.y))
                {
                    CreateSign(s.x, s.y, s.text); //不存在则新建
                }
            });

            BLog.Success($"共加载 {Signs.Count} 个标牌");

            var invalidCount = 0;
            Signs.Where(s => !Terraria.Main.tileSign[Terraria.Main.tile[s.X, s.Y].type])
                .ToArray()
                .ForEach(s =>
                {
                    RemoveSign(s);
                    invalidCount++;
                });
            if (invalidCount > 0)
                BLog.Success($"移除 {invalidCount} 个无效标牌");
        }
        [SimpleTimer(Time = 5)]
        private static void UpdateClientSign()
        {
            Task.Run(() =>
            {
                BInfo.OnlinePlayers.ForEach(plr =>
                {
                    if (plr.WatchingSign?.sign is { } sign
                    && !new Rectangle(sign.X, sign.Y, 2, 2).Intersects(new Rectangle(plr.TileX - 5, plr.TileY - 5, 14, 12)))
                        plr.WatchingSign = null; //超出范围则未在编辑标牌.

                    plr.LastWatchingSignIndex = (short)(plr.WatchingSign == null ? -1 : 0); //从第一个开始, 第零个一般是当前正在看的
                    var packets = new List<Packet>();

                    AllSign().Where(s => BUtils.IsPointInCircle(s.X, s.Y, plr.TileX, plr.TileY, BConfig.Instance.SignRefreshRadius))
                        .ForEach(s => packets.Add(new ReadSign()
                        {
                            PlayerSlot = plr.Index,
                            Position = new((short)s.X, (short)s.Y),
                            Text = s.Text,
                            SignSlot = plr.GetNextSignSlot(),
                            Bit1 = new Terraria.BitsByte(true)
                        }));

                    plr.SendPackets(packets);
                });
            });
        }

        #region 事件
        public delegate void OnSignRead(BEventArgs.SignReadEventArgs args);
        public static event OnSignRead SignRead;
        public delegate void OnSignCreate(BEventArgs.SignCreateEventArgs args);
        public static event OnSignCreate SignCreate;
        public delegate void OnSignUpdate(BEventArgs.SignUpdateEventArgs args);
        public static event OnSignUpdate SignUpdate;
        public delegate void OnSignRemove(BEventArgs.SignRmoveEventArgs args);
        public static event OnSignRemove SignRemove;
        internal static void OnSyncSign(BPlayer plr, ReadSign sign)
        {
            if (FindBSignFromPos(sign.Position.X, sign.Position.Y) is { } s)
            {
                if (sign.Text != s.Text)
                {
                    var args = new BEventArgs.SignUpdateEventArgs(plr, sign);
                    SignUpdate?.Invoke(args);
                    if (!args.Handled)
                    {
                        if (plr.TsPlayer.HasPermission("boss.player.sign.update"))
                        {
                            UpdateSignDirect(plr, s, sign.Text);
                            plr?.SendSuccessMsg($"已更新标牌");
                        }
                        else
                        {
                            plr.SendInfoMsg($"你没有权限修改这个标牌");
                        }
                    }
                }
            }
            else
            {
                var args = new BEventArgs.SignCreateEventArgs(plr, sign.Position.ToPoint());
                SignCreate?.Invoke(args);
                if (!args.Handled)
                {
                    CreateSign(sign.Position.X, sign.Position.Y, sign.Text, plr);
                    plr?.SendSuccessMsg($"已创建标牌");
                }
            }
        }
        internal static void OnOpenSign(BPlayer plr, RequestReadSign readSign)
        {
            var args = new BEventArgs.SignReadEventArgs(plr, readSign.Position.ToPoint());
            SignRead?.Invoke(args);
            if (!args.Handled)
            {
                if (FindBSignFromPos(readSign.Position.X, readSign.Position.Y) is { } s)
                    s.SendSign(plr, true);
                else //不确定要不要生成, 要是有人一直代码发包就能一直创建了
                {
                    CreateSign(readSign.Position.X, readSign.Position.Y, "", plr)
                        .SendSign(plr, true);
                }
            }
        }
        #endregion

        #region 标牌操作

        public static void UpdateSignDirect(BPlayer plr, BSign sign, string text)
        {
            sign.Text = text;
            sign.UpdateSingle(s => s.LastUpdateUser, plr.Index);
            sign.UpdateSingle(s => s.Text, sign.Text);

            BInfo.OnlinePlayers.Where(p => p.WatchingSign?.sign == sign)
                .ForEach(p =>
                {
                    sign.SendSign(p, true); //同步给其他正在看这个牌子的玩家
                });
        }

        public static BSign CreateSign(int tileX, int tileY, string text, BPlayer plr = null)
        {

            var sign = new BSign()
            {
                X = tileX,
                Y = tileY,
                Text = text,
                Owner = plr?.Index ?? -1,
                LastUpdateUser = plr?.Index ?? -1,
                WorldId = Terraria.Main.worldID
            };
            DBTools.Insert(sign);
            Signs.Add(sign);
            BLog.DEBUG($"创建标牌数据于 {sign.X} - {sign.Y} {(plr is null ? "" : $"来自 {plr}")}");
            return sign;
        }
        public static short GetNextSignSlot(this BPlayer plr)
        {
            if (plr.LastWatchingSignIndex > 998)
                plr.LastWatchingSignIndex = -1;
            plr.LastWatchingSignIndex++;
            if (plr.LastWatchingSignIndex == plr.WatchingChest?.slot)
                plr.LastWatchingSignIndex++;
            return plr.LastWatchingSignIndex;
        }
        public static BSign FindBSignFromPos(int tileX, int tileY)
            => _overrideSign.LastOrDefault(s => s.Contains(tileX, tileY)) ?? Signs.LastOrDefault(s => s.Contains(tileX, tileY));
        public static void SendSign(this BSign sign, BPlayer target, bool watch = false)
        {
            if (watch)
                target.WatchingSign = new(target.LastWatchingSignIndex, sign);
            SendSign(target, (short)sign.X, (short)sign.Y, sign.Text, watch);
        }
        public static void SendSign(BPlayer plr, short x, short y, string text, bool watch = false)
        {
            plr.SendPacket(new ReadSign()
            {
                PlayerSlot = plr.Index,
                Position = new(x, y),
                Text = text,
                SignSlot = plr.GetNextSignSlot(),
                Bit1 = new Terraria.BitsByte(!watch)
            });
        }
        public static bool RemoveSign(int x, int y)
        {
            if (FindBSignFromPos(x, y) is { } sign)
            {
                RemoveSign(sign);
                return true;
            }
            return false;
        }
        public static void RemoveSign(BSign sign)
        {
            BInfo.OnlinePlayers.Where(p => p.WatchingSign?.sign == sign)
                    .ForEach(p => p.WatchingSign = null);
            if (!DeregisterOverrideSign(sign))
                if (Signs.Remove(sign))
                    DBTools.Delete(sign);
        }
        public static BSign[] GetSignsInArea(int startX, int startY, int width, int height)
        {
            var result = new List<BSign>();
            var rec = new Rectangle(startX, startY, width, height);
            AllSign().ForEach((s, i) =>
            {
                if (!result.Exists(r => r.Contains(s.X, s.Y)) && rec.Contains(s.X, s.Y))
                    result.Add(s);
            });
            return result.ToArray();
        }
        #endregion

        public static void RegisterOverrideSign(int tileX, int tileY, string text)
            => _overrideSign.Add(new()
            {
                X = tileX,
                Y = tileY,
                Text = text,
                Owner = -1,
                LastUpdateUser = -1
            });
        public static bool DeregisterOverrideSign(int tileX, int tileY)
            => _overrideSign.RemoveAll(s => s.Contains(tileX, tileY)) != 0;
        public static bool DeregisterOverrideSign(BSign sign)
            => _overrideSign.Remove(sign);
        /// <summary>
        /// 包含注册的牌子在内的所有牌子
        /// </summary>
        /// <returns></returns>
        public static List<BSign> AllSign()
        {
            var result = new List<BSign>();
            result.AddRange(Signs);
            result.AddRange(_overrideSign);
            return result;
        }
        public static List<BSign> RegistedSigns
            => _overrideSign;
    }
}
