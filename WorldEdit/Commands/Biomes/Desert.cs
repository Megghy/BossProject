using Terraria.ID;
namespace WorldEdit.Commands.Biomes
{
    public class Desert : Biome
    {
        public override int Dirt => TileID.Sand;
        public override int Clay => TileID.Sand;
        public override int Stone => TileID.Sandstone;
        public override int Ice => TileID.Sandstone;
        public override int Sand => TileID.Sand;
        public override int HardenedSand => TileID.HardenedSand;
        public override int Sandstone => TileID.Sandstone;
        public override int[] Grass => new int[] { TileID.Sand };
        public override int Plants => -1;
        public override int TallPlants => -1;
        public override int Vines => -1;
        public override int Thorn => -1;

        public override ushort DirtWall => WallID.HardenedSand;
        public override ushort DirtWallUnsafe => WallID.HardenedSand;
        public override ushort CaveWall => WallID.HardenedSand;
        public override ushort DirtWallUnsafe1 => WallID.HardenedSand;
        public override ushort DirtWallUnsafe2 => WallID.Sandstone;
        public override ushort DirtWallUnsafe3 => WallID.HardenedSand;
        public override ushort DirtWallUnsafe4 => WallID.Sandstone;
        public override ushort StoneWall => WallID.Sandstone;
        public override ushort HardenedSandWall => WallID.HardenedSand;
        public override ushort SandstoneWall => WallID.Sandstone;
        public override ushort GrassWall => WallID.HardenedSand;
        public override ushort GrassWallUnsafe => WallID.HardenedSand;
        public override ushort FlowerWall => WallID.HardenedSand;
        public override ushort FlowerWallUnsafe => WallID.HardenedSand;

        public override ushort CaveWall1 => 0;
        public override ushort CaveWall2 => 0;
        public override ushort CaveWall3 => 0;
        public override ushort CaveWall4 => 0;
    }
}