using BossFramework.BModels;
using TrProtocol;

namespace BossFramework.BInterfaces
{
    public interface IPacketHandler
    {
        public bool GetPacket(BPlayer plr, Packet packet);
        public bool SendPacket(BPlayer plr, Packet packet);
    }
    public abstract class PacketHandlerBase<T> : IPacketHandler where T : Packet
    {
        public delegate void OnGet(BEventArgs.PacketHookArgs<T> hurt);
        public static event OnGet Get;
        public delegate void OnSend(BEventArgs.PacketHookArgs<T> hurt);
        public static event OnSend Send;
        public bool GetPacket(BPlayer plr, Packet packet) => OnGetPacket(plr, (T)packet);

        public bool SendPacket(BPlayer plr, Packet packet) => OnSendPacket(plr, (T)packet);
        /// <summary>
        /// 接收到数据包
        /// </summary>
        /// <param name="packet"></param>
        /// <returns>是否handled</returns>
        public virtual bool OnGetPacket(BPlayer plr, T packet)
        {
            var args = new BEventArgs.PacketHookArgs<T>(packet, plr);
            Get?.Invoke(args);
            return args.Handled;
        }
        public virtual bool OnSendPacket(BPlayer plr, T packet)
        {
            var args = new BEventArgs.PacketHookArgs<T>(packet, plr);
            Send?.Invoke(args);
            return args.Handled;
        }
    }
}
