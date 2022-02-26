namespace TrProtocol.Packets
{
    public struct RequestTileData : IPacket
    {
        public MessageID Type => MessageID.RequestTileData;
        public Point Position { get; set; }
    }
}
