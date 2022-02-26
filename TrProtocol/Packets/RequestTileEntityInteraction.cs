namespace TrProtocol.Packets
{
    public struct RequestTileEntityInteraction : IPacket, IPlayerSlot
    {
        public MessageID Type => MessageID.RequestTileEntityInteraction;
        public int TileEntityID { get; set; }
        public byte PlayerSlot { get; set; }
    }
}