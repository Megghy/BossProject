namespace FakeProvider
{
    public interface IFake
    {
        TileProvider Provider { get; }
        int Index { get; set; }
        int RelativeX { get; set; }
        int RelativeY { get; set; }
        int X { get; set; }
        int Y { get; set; }
        ushort[] TileTypes { get; }
    }
}
