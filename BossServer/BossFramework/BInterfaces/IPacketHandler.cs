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
        public bool GetPacket(BPlayer plr, Packet packet) => OnGetPacket(plr, (T)packet);

        public bool SendPacket(BPlayer plr, Packet packet) => OnSendPacket(plr, (T)packet);
        /// <summary>
        /// 接收到数据包
        /// </summary>
        /// <param name="packet"></param>
        /// <returns>是否handled</returns>
        public abstract bool OnGetPacket(BPlayer plr, T packet);
        public abstract bool OnSendPacket(BPlayer plr, T packet);
    }
}
