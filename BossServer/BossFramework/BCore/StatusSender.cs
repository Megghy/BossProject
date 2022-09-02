using BossFramework.BAttributes;
using BossFramework.BModels;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TrProtocol.Packets;

namespace BossFramework.BCore
{
    public static class StatusSender
    {
        public static readonly byte[] PingPacketData = new ResetItemOwner() { ItemSlot = PING_ITEM_SLOT }.SerializePacket();
        public const int PING_ITEM_SLOT = 400;
        internal static readonly Dictionary<Func<BEventArgs.BaseEventArgs, string>, int> _statusCallback = new() { { DefaultStatus, 99 } };
        [SimpleTimer(Time = 1)]
        public static void UpdateStatus()
        {
            var sb = new StringBuilder();
            BInfo.OnlinePlayers.ForEach(p =>
            {
                sb.Clear();
                sb.AppendLine("                                                                           ");
                var dict = p.PlayerStatusCallback.Concat(_statusCallback).OrderByDescending(i => i.Value);
                dict.OrderByDescending(s => s.Value)
                .ForEach(callback =>
                {
                    sb.AppendLine(callback.Key(new(p)));
                });
                sb.AppendLine(RepeatLineBreaks(50));
                p.SendPacket(new StatusText()
                {
                    Text = TrProtocol.Models.NetworkText.FromLiteral(sb.ToString()),
                    Max = 0,
                    Flag = 0,
                });
                if (!p.WaitingPing)
                {
                    p.WaitingPing = true;
                    p.SendRawData(PingPacketData);
                    p.PingChecker.Restart();
                }
            });
        }
        private static string DefaultStatus(BEventArgs.BaseEventArgs args)
        {
            if (args.Handled)
                return string.Empty;
            var sb = new StringBuilder();
            sb.AppendLine($"Ping: {(args.Player.LastPing < 100 ? "[i:3738]" : "[i:3736]")} {args.Player.LastPing} ms");
            sb.AppendLine($"在线: {BInfo.OnlinePlayers.Length} 人");
            return sb.ToString();
        }
        internal static void GetPingBackPacket(BPlayer plr)
        {
            plr.WaitingPing = false;
            plr.LastPing = (int)plr.PingChecker.ElapsedMilliseconds;
            plr.LastPingTime = BInfo.GameTick;
            plr.PingChecker.Stop();
        }
        /// <summary>
        /// 注册状态信息
        /// </summary>
        /// <param name="func"></param>
        /// <param name="order">越高越靠前</param>
        public static void RegisteStatus(Func<BEventArgs.BaseEventArgs, string> func, int order = 100)
        {
            if (_statusCallback.ContainsKey(func))
                return;
            _statusCallback.Add(func, order);
        }
        public static void DeregisteStatus(Func<BEventArgs.BaseEventArgs, string> func)
        {
            if (_statusCallback.ContainsKey(func))
                _statusCallback.Remove(func);
        }
        /// <summary>
        /// 注册状态信息
        /// </summary>
        /// <param name="func"></param>
        /// <param name="order">越高越靠前</param>
        public static void RegistePlayerStatus(this BPlayer plr, Func<BEventArgs.BaseEventArgs, string> func, int order = 100)
        {
            if (plr.PlayerStatusCallback.ContainsKey(func))
                return;
            plr.PlayerStatusCallback.Add(func, order);
        }
        public static void DeregistePlayerStatus(this BPlayer plr, Func<BEventArgs.BaseEventArgs, string> func)
        {
            if (plr.PlayerStatusCallback.ContainsKey(func))
                plr.PlayerStatusCallback.Remove(func);
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
