#region Using
using Terraria;
using Terraria.ID;
#endregion
namespace FakeProvider
{
    public class FakeChest : Chest, IFake
    {
        #region Data

        public TileProvider Provider { get; }
        public int Index { get; set; }
        public int X
        {
            get => x;
            set => x = value;
        }
        public int Y
        {
            get => y;
            set => y = value;
        }
        internal static ushort[] _TileTypes = new ushort[]
        {
            TileID.Containers,
            TileID.Containers2,
            TileID.Dressers
        };
        public ushort[] TileTypes => _TileTypes;
        public int RelativeX { get; set; }
        public int RelativeY { get; set; }

        #endregion

        #region Constructor

        public FakeChest(TileProvider Provider, int Index, int X, int Y, Item[] Items = null)
        {
            this.Provider = Provider;
            this.Index = Index;
            this.RelativeX = X;
            this.RelativeY = Y;
            this.x = Provider.X + X;
            this.y = Provider.Y + Y;
            this.item = Items ?? new Item[40];
            for (int i = 0; i < 40; i++)
                this.item[i] = this.item[i] ?? new Item();
        }

        #endregion
    }
}
