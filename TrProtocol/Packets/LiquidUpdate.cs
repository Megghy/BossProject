namespace TrProtocol.Packets
{
    public class LiquidUpdate : Packet
    {
#pragma warning disable CS0618 // 类型或成员已过时
        public override MessageID Type => MessageID.LiquidUpdate;
#pragma warning restore CS0618 // 类型或成员已过时
        public short TileX { get; set; }
        public short TileY { get; set; }
        public byte Liquid { get; set; }
        public byte LiquidType { get; set; }
    }
}
