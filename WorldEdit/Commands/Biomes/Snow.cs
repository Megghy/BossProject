using Terraria.ID;
namespace WorldEdit.Commands.Biomes
{
    public class Snow : Biome
    {
        public override int Dirt => TileID.SnowBlock;
        public override int[] Grass => new int[] { TileID.SnowBlock };
        public override int Stone => TileID.IceBlock;
        public override int Ice => TileID.IceBlock;
        public override int Clay => TileID.SnowBlock;
        public override int Sand => TileID.SnowBlock;
        public override int HardenedSand => TileID.SnowBlock;
        public override int Sandstone => TileID.IceBlock;
        public override int Plants => -1;
        public override int TallPlants => -1;
        public override int Vines => -1;
        public override int Thorn => -1;

        public override ushort DirtWall => WallID.SnowWallUnsafe;
        public override ushort DirtWallUnsafe => WallID.SnowWallUnsafe;
        public override ushort CaveWall => WallID.SnowWallUnsafe;
        public override ushort DirtWallUnsafe1 => WallID.SnowWallUnsafe;
        public override ushort DirtWallUnsafe2 => WallID.SnowWallUnsafe;
        public override ushort DirtWallUnsafe3 => WallID.SnowWallUnsafe;
        public override ushort DirtWallUnsafe4 => WallID.SnowWallUnsafe;
        public override ushort StoneWall => WallID.IceUnsafe;
        public override ushort HardenedSandWall => WallID.IceUnsafe;
        public override ushort SandstoneWall => WallID.IceUnsafe;
        public override ushort GrassWall => WallID.IceUnsafe;
        public override ushort GrassWallUnsafe => WallID.IceUnsafe;
        public override ushort FlowerWall => WallID.IceUnsafe;
        public override ushort FlowerWallUnsafe => WallID.IceUnsafe;

        public override ushort CaveWall1 => 0;
        public override ushort CaveWall2 => 0;
        public override ushort CaveWall3 => 0;
        public override ushort CaveWall4 => 0;
    }
}