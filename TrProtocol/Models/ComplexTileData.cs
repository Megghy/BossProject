namespace TrProtocol.Models
{
    public struct ComplexTileData
    {
        public ProtocolBitsByte Flags1;
        public ProtocolBitsByte Flags2;
        public ProtocolBitsByte Flags3;

        public ushort TileType { get; set; }
        public short FrameX { get; set; }
        public short FrameY { get; set; }
        public byte TileColor { get; set; }
        public ushort WallType { get; set; }
        public byte WallColor { get; set; }
        public byte Liquid { get; set; }

        public short Count { get; set; }
    }
}
