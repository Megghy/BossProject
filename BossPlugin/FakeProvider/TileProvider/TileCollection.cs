using Terraria;

namespace FakeProvider
{
    public class TileCollection(ITile[,] collection) : ModFramework.ICollection<ITile>
    {
        protected ITile[,] Tiles = collection;
        public int Width => Tiles.GetLength(0);
        public int Height => Tiles.GetLength(1);

        public virtual ITile this[int x, int y]
        {
            get
            {
                return Tiles[x, y];
            }
            set
            {
                Tiles[x, y] = value;
            }
        }
    }
}
