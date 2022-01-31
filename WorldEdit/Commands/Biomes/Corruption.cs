using Terraria.ID;
namespace WorldEdit.Commands.Biomes
{
    public class Corruption : Biome
    {
        public override int Dirt => TileID.Dirt;
        public override int[] Grass => new int[] { TileID.CorruptGrass };
        public override int Stone => TileID.Ebonstone;
        public override int Ice => TileID.CorruptIce;
        public override int Clay => TileID.ClayBlock;
        public override int Sand => TileID.Ebonsand;
        public override int HardenedSand => TileID.CorruptHardenedSand;
        public override int Sandstone => TileID.CorruptSandstone;
        public override int Plants => TileID.CorruptPlants;
        public override int TallPlants => -1;
        public override int Vines => -1;
        public override int Thorn => TileID.CorruptThorns;

        public override ushort DirtWall => WallID.Dirt;
        public override ushort DirtWallUnsafe => WallID.DirtUnsafe;
        public override ushort CaveWall => WallID.CaveWall;
        public override ushort DirtWallUnsafe1 => WallID.DirtUnsafe1;
        public override ushort DirtWallUnsafe2 => WallID.DirtUnsafe2;
        public override ushort DirtWallUnsafe3 => WallID.DirtUnsafe3;
        public override ushort DirtWallUnsafe4 => WallID.DirtUnsafe4;
        public override ushort StoneWall => WallID.EbonstoneUnsafe;
        public override ushort HardenedSandWall => WallID.CorruptHardenedSand;
        public override ushort SandstoneWall => WallID.CorruptSandstone;
        public override ushort GrassWall => WallID.CorruptGrassUnsafe;
        public override ushort GrassWallUnsafe => WallID.CorruptGrassUnsafe;
        public override ushort FlowerWall => WallID.CorruptGrassUnsafe;
        public override ushort FlowerWallUnsafe => WallID.CorruptGrassUnsafe;

        public override ushort CaveWall1 => WallID.CorruptionUnsafe1;
        public override ushort CaveWall2 => WallID.CorruptionUnsafe2;
        public override ushort CaveWall3 => WallID.CorruptionUnsafe3;
        public override ushort CaveWall4 => WallID.CorruptionUnsafe4;
    }
}