using BossFramework.BAttributes;
using BossFramework.BModels;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TrProtocol.Packets;
using static MonoMod.InlineRT.MonoModRule;

namespace BossFramework.BCore
{
    public static class SignRedirector
    {
        public static List<BSign> Signs { get; private set; }
        private static List<BSign> OverrideSign { get; set; } = new();

        [AutoPostInit]
        private static void InitSign()
        {
            BLog.DEBUG("初始化标牌重定向");

            Signs = DB.DBTools.GetAll<BSign>().Where(r => r.WorldId == Terraria.Main.worldID).ToList();

            Terraria.Main.sign.Where(s => s != null).ForEach(s =>
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
        }
        [SimpleTimer(Time = 5)]
        private static void UpdateClientSign()
        {
            Task.Run(() =>
            {
                BInfo.OnlinePlayers.ForEach(plr =>
                {
                    if (plr.WatchingSign != null
                    && !new Rectangle(plr.WatchingSign.X, plr.WatchingSign.Y, 2, 2).Intersects(new Rectangle(plr.TileX - 5, plr.TileY - 5, 14, 12)))
                        plr.WatchingSign = null; //超出范围则未在编辑标牌.

                    plr.LastWatchingSignIndex = plr.WatchingSign == null ? -1 : 0; //从第一个开始, 第零个一般是当前正在看的
                    lock (Signs)
                    {
                        Signs.Where(s => BUtils.IsPointInCircle(s.X, s.Y, plr.TileX, plr.TileY, BConfig.Instance.SignRefreshRadius) && plr.WatchingSign != s)
                        .ForEach(s => s.SendTo(plr));
                    }
                });
            });
        }
        public static void OnSyncSign(BPlayer plr, ReadSign sign)
        {
            if (FindBSignFromPos(sign.Position.X, sign.Position.Y) is { } s)
            {
                s.UpdateSingle(s => s.Text, sign.Text);
                plr?.SendSuccessMsg($"已更新标牌");
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
                LastUpdateUser = plr?.Index ?? -1
            };
            DB.DBTools.Insert(sign);
            Signs.Add(sign);
            BLog.Info($"创建标牌数据于 {sign.X} - {sign.Y}");
            return sign;
        }

        public static BSign FindBSignFromPos(int tileX, int tileY)
            => OverrideSign.LastOrDefault(s => s.Contains(tileX, tileY)) ?? Signs.LastOrDefault(s => s.Contains(tileX, tileY));
        public static void SendTo(this BSign sign, BPlayer target, bool watch = false)
        {
            if (target.LastWatchingSignIndex > 998)
                target.LastWatchingSignIndex = -1;
            target.LastWatchingSignIndex++;
            if(watch)
                target.WatchingSign = sign;

            target.SendPacket(new ReadSign()
            {
                PlayerSlot = target.Index,
                Position = new((short)sign.X, (short)sign.Y),
                Text = sign.Text,
                SignSlot = (short)target.LastWatchingSignIndex,
                Bit1 = new Terraria.BitsByte(watch)
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
