#region Using
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Tile_Entities;
using Terraria.ID;
#endregion
namespace FakeProvider
{
    public class FakeHatRack : TEHatRack, IFake
    {
        #region Data

        public TileProvider Provider { get; }
        public int Index
        {
            get => ID;
            set => ID = value;
        }
        public int X
        {
            get => Position.X;
            set => Position = new Point16((short)value, Position.Y);
        }
        public int Y
        {
            get => Position.Y;
            set => Position = new Point16(Position.X, (short)value);
        }
        internal static ushort[] _TileTypes = new ushort[]
        {
            TileID.HatRack
        };
        public ushort[] TileTypes => _TileTypes;
        public int RelativeX { get; set; }
        public int RelativeY { get; set; }

        #endregion

        #region Constructor

        public FakeHatRack(TileProvider Provider, int Index, int X, int Y, Item[] Items = null, Item[] Dyes = null)
        {
            this.Provider = Provider;
            this.ID = Index;
            this.RelativeX = X;
            this.RelativeY = Y;
            this.Position = new Point16(X, Y);
            this.type = _myEntityID;
            this._items = Items ?? new Item[2];
            for (int i = 0; i < 2; i++)
                this._items[i] = this._items[i] ?? new Item();
            this._dyes = Dyes ?? new Item[2];
            for (int i = 0; i < 2; i++)
                this._dyes[i] = this._dyes[i] ?? new Item();
        }

        #endregion
    }
}
