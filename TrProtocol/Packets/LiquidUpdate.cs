namespace TrProtocol.Packets
{
    public struct LiquidUpdate : IPacket
    {
#pragma warning disable CS0618 // 类型或成员已过时
        public MessageID Type => MessageID.LiquidUpdate;
#pragma warning restore CS0618 // 类型或成员已过时
        public short TileX { get; set; }
        public short TileY { get; set; }
        public byte Liquid { get; set; }
        public byte LiquidType { get; set; }
    }
}
