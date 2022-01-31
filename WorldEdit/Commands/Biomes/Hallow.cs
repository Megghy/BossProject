using Terraria.ID;
namespace WorldEdit.Commands.Biomes
{
    public class Hallow : Biome
    {
        public override int Dirt => TileID.Dirt;
        public override int[] Grass => new int[] { TileID.HallowedGrass, TileID.GolfGrassHallowed };
        public override int Stone => TileID.Pearlstone;
        public override int Ice => TileID.HallowedIce;
        public override int Clay => TileID.ClayBlock;
        public override int Sand => TileID.Pearlsand;
        public override int HardenedSand => TileID.HallowHardenedSand;
        public override int Sandstone => TileID.HallowSandstone;
        public override int Plants => TileID.HallowedPlants;
        public override int TallPlants => TileID.HallowedPlants2;
        public override int Vines => TileID.HallowedVines;
        public override int Thorn => -1;

        public override ushort DirtWall => WallID.Dirt;
        public override ushort DirtWallUnsafe => WallID.DirtUnsafe;
        public override ushort CaveWall => WallID.CaveWall;
        public override ushort DirtWallUnsafe1 => WallID.DirtUnsafe1;
        public override ushort DirtWallUnsafe2 => WallID.DirtUnsafe2;
        public override ushort DirtWallUnsafe3 => WallID.DirtUnsafe3;
        public override ushort DirtWallUnsafe4 => WallID.DirtUnsafe4;
        public override ushort StoneWall => WallID.PearlstoneBrickUnsafe;
        public override ushort HardenedSandWall => WallID.HallowHardenedSand;
        public override ushort SandstoneWall => WallID.HallowSandstone;
        public override ushort GrassWall => WallID.HallowedGrassUnsafe;
        public override ushort GrassWallUnsafe => WallID.HallowedGrassUnsafe;
        public override ushort FlowerWall => WallID.HallowedGrassUnsafe;
        public override ushort FlowerWallUnsafe => WallID.HallowedGrassUnsafe;

        public override ushort CaveWall1 => WallID.HallowUnsafe1;
        public override ushort CaveWall2 => WallID.HallowUnsafe2;
        public override ushort CaveWall3 => WallID.HallowUnsafe3;
        public override ushort CaveWall4 => WallID.HallowUnsafe4;
    }
}