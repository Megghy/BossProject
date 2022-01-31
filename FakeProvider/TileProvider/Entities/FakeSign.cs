#region Using
using Terraria;
using Terraria.ID;
#endregion
namespace FakeProvider
{
    public class FakeSign : Sign, IFake
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
            TileID.Signs,
            TileID.AnnouncementBox,
            TileID.Tombstones
        };
        public ushort[] TileTypes => _TileTypes;
        public int RelativeX { get; set; }
        public int RelativeY { get; set; }

        #endregion

        #region Constructor

        public FakeSign(TileProvider Provider, int Index, int X, int Y, string Text = "")
        {
            this.Provider = Provider;
            this.Index = Index;
            this.RelativeX = X;
            this.RelativeY = Y;
            this.x = Provider.ProviderCollection.OffsetX + Provider.X + X;
            this.y = Provider.ProviderCollection.OffsetY + Provider.Y + Y;
            this.text = Text;
        }

        #endregion
    }
}
