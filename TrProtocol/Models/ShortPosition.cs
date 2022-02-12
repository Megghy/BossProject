namespace TrProtocol.Models
{
    public partial struct ShortPosition
    {
        public ShortPosition(short x, short y)
        {
            X = x;
            Y = y;
        }
        public ShortPosition(int x, int y)
        {
            X = (short)x;
            Y = (short)y;
        }
        public short X, Y;
        public override string ToString()
        {
            return $"[{X}, {Y}]";
        }
    }
}
