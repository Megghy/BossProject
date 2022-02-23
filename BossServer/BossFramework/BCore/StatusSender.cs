using BossFramework.BAttributes;
using BossFramework.BModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrProtocol.Packets;

namespace BossFramework.BCore
{
    public static class StatusSender
    {
        public const int PING_ITEM_SLOT = 400;
        internal static readonly Dictionary<Func<BPlayer, string>, int> _statusCallback = new() { { DefaultStatus, 99 } };
        [SimpleTimer(Time = 1)]
        public static void UpdateStatus()
        {
            BInfo.OnlinePlayers.ForEach(p =>
            {
                var text = _statusCallback.OrderByDescending(s => s.Value)
                .First().Key(p);
                text = "                                                                           \r\n"
                 + text + RepeatLineBreaks(50);
                p.SendPacket(new StatusText()
                {
                    Text = Terraria.Localization.NetworkText.FromLiteral(text),
                    Max = 0,
                    Flag = 0,
                });
            });
        }
        private static string DefaultStatus(BPlayer plr)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Ping: {(plr.LastPing < 70 ? "[i:3738]" : "[i:3736]")} {plr.LastPing} ms");
            sb.AppendLine($"在线: {BInfo.OnlinePlayers.Length} 人");
            return sb.ToString();
        }
        internal static void GetPingBackPacket(BPlayer plr)
        {
            Task.Run(() =>
            {
                plr.LastPing = (int)plr.PingChecker.ElapsedMilliseconds;
                Task.Delay(500).Wait();
                plr.PingChecker.Restart();
                plr.SendPacket(new ResetItemOwner() { ItemSlot = PING_ITEM_SLOT });
            });
        }
        /// <summary>
        /// 注册状态信息
        /// </summary>
        /// <param name="func"></param>
        /// <param name="order">越高越靠前</param>
        public static void RegisteStatus(Func<BPlayer, string> func, int order = 100)
        {
            if (_statusCallback.ContainsKey(func))
                return;
            _statusCallback.Add(func, order);
        }
        public static void DeregisteStatus(Func<BPlayer, string> func)
        {
            if (_statusCallback.ContainsKey(func))
                _statusCallback.Remove(func);
        }
        private static string RepeatLineBreaks(int v)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < v; i++)
            {
                stringBuilder.Append("\r\n");
            }
            return stringBuilder.ToString();
        }
    }
}
