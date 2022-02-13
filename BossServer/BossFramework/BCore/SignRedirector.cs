using BossFramework.BAttributes;
using BossFramework.BModels;
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
        private static List<BSign> Signs { get; set; }
        private static List<BSign> OverrideSign { get; set; } = new();

        [AutoPostInit]
        private static void InitSign()
        {
            BLog.DEBUG("初始化标牌重定向");

            Signs = DB.DBTools.GetAll<BSign>().Where(r => r.WorldId == Terraria.Main.worldID).ToList();

            Terraria.Main.sign.Where(s => s != null).BForEach(s =>
            {
                if (!Signs.Exists(sign => sign.X == s.x && sign.Y == s.y))
                {
                    OnSyncSign(null, new()
                    {
                        Text = s.text,
                        Position = new((short)s.x, (short)s.y)
                    }); //不存在则新建
                }
            });

            BLog.Success($"共加载 {Signs.Count} 个标牌");
        }
        [SimpleTimer(Time = 5)]
        private static void UpdateClientSign()
        {
            Task.Run(() =>
            {
                BInfo.OnlinePlayers.BForEach(plr =>
                {
                    if (plr.WatchingSign?.sign is { } sign
                    && !new Rectangle(sign.X, sign.Y, 2, 2).Intersects(new Rectangle(plr.TileX - 5, plr.TileY - 5, 14, 12)))
                        plr.WatchingSign = null; //超出范围则未在编辑标牌.

                    plr.LastWatchingSignIndex = (short)(plr.WatchingSign == null ? -1 : 0); //从第一个开始, 第零个一般是当前正在看的
                    var packets = new List<Packet>();
                    Signs.Where(s => BUtils.IsPointInCircle(s.X, s.Y, plr.TileX, plr.TileY, BConfig.Instance.SignRefreshRadius))
                    .BForEach(s => packets.Add(new ReadSign()
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
        public static void OnSyncSign(BPlayer plr, ReadSign sign)
        {
            if (FindBSignFromPos(sign.Position.X, sign.Position.Y) is { } s)
            {
                if (sign.Text != s.Text)
                {
                    s.UpdateSingle(s => s.LastUpdateUser, plr.Index);
                    s.UpdateSingle(s => s.Text, sign.Text);

                    BInfo.OnlinePlayers.Where(p => p.WatchingSign?.sign == s)
                        .BForEach(p =>
                        {
                            sign.SignSlot = plr.WatchingSign.Value.slot;
                            p.SendPacket(sign); //同步给其他正在看这个牌子的玩家
                        });

                    plr?.SendSuccessMsg($"已更新标牌");
                }
            }
            else
            {
                CreateSign(sign.Position.X, sign.Position.Y, sign.Text, plr);
                plr?.SendSuccessMsg($"已创建标牌");
            }
        }
        public static void OnOpenSign(BPlayer plr, RequestReadSign readSign)
        {
            if (FindBSignFromPos(readSign.Position.X, readSign.Position.Y) is { } s)
                s.SendTo(plr, true);
            else //不确定要不要生成, 要是有人一直代码发包就能一直创建了
            {
                CreateSign(readSign.Position.X, readSign.Position.Y, "", plr)
                    .SendTo(plr, true);
            }
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
            DB.DBTools.Insert(sign);
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
            => OverrideSign.LastOrDefault(s => s.Contains(tileX, tileY)) ?? Signs.LastOrDefault(s => s.Contains(tileX, tileY));
        public static void SendTo(this BSign sign, BPlayer target, bool watch = false)
        {
            if (watch)
                target.WatchingSign = new(target.LastWatchingSignIndex, sign);

            var slot = target.GetNextSignSlot();

            target.SendPacket(new ReadSign()
            {
                PlayerSlot = target.Index,
                Position = new((short)sign.X, (short)sign.Y),
                Text = sign.Text,
                SignSlot = slot,
                Bit1 = new Terraria.BitsByte(!watch)
            });
        }

        public static void RegisterOverrideSign(int tileX, int tileY, string text)
            => OverrideSign.Add(new()
            {
                X = tileX,
                Y = tileY,
                Text = text,
                Owner = -1,
                LastUpdateUser = -1
            });
        public static void DeregisterOverrideSign(int tileX, int tileY, string text)
            => OverrideSign.RemoveAll(s => s.Contains(tileX, tileY));
    }
}
