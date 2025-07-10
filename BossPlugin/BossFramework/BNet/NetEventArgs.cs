using System.ComponentModel;

namespace BossFramework.BNet
{
    /// <summary>
    /// 为网络包事件提供基础数据。
    /// </summary>
    public class NetPacketEventArgs : EventArgs
    {
        /// <summary>
        /// 获取相关玩家的索引。
        /// </summary>
        public int PlayerIndex { get; }

        /// <summary>
        /// 获取或设置数据包内容的只读内存视图。
        /// 设置此属性将允许修改在事件链中传递的数据包内容。
        /// 警告：修改此值时，请务必使用高性能方式（如 ArrayPool）来分配新内存，以避免GC压力。
        /// </summary>
        public ReadOnlyMemory<byte> Data { get; set; }

        public NetPacketEventArgs(int playerIndex, ReadOnlyMemory<byte> data)
        {
            PlayerIndex = playerIndex;
            Data = data;
        }
    }

    /// <summary>
    /// 为数据包发送事件提供数据。
    /// </summary>
    public class PacketSendingEventArgs : NetPacketEventArgs
    {
        /// <summary>
        /// 获取或设置一个值，该值指示该数据包是否已被处理。
        /// 如果设为 <c>true</c>，则默认的发送逻辑将不会执行。
        /// 默认为 <c>false</c>。
        /// </summary>
        [DefaultValue(false)]
        public bool Handled { get; set; }

        public PacketSendingEventArgs(int playerIndex, ReadOnlyMemory<byte> data) : base(playerIndex, data)
        {
        }
    }

    /// <summary>
    /// 为数据包接收事件提供数据。
    /// </summary>
    public class PacketReceivedEventArgs : NetPacketEventArgs
    {
        /// <summary>
        /// 获取或设置一个值，该值指示该数据包是否已被处理。
        /// 如果设为 <c>true</c>，则默认的接收逻辑将不会执行。
        /// 默认为 <c>false</c>。
        /// </summary>
        [DefaultValue(false)]
        public bool Handled { get; set; }

        public PacketReceivedEventArgs(int playerIndex, ReadOnlyMemory<byte> data) : base(playerIndex, data)
        {
        }
    }
}