using Terraria.ID;
namespace WorldEdit.Commands.Biomes
{
    public class Forest : Biome
    {
        public override int Dirt => TileID.Dirt;
        public override int[] Grass => new int[] { TileID.Grass, TileID.GolfGrass };
        public override int Stone => TileID.Stone;
        public override int Ice => TileID.IceBlock;
        public override int Clay => TileID.ClayBlock;
        public override int Sand => TileID.Sand;
        public override int HardenedSand => TileID.HardenedSand;
        public override int Sandstone => TileID.Sandstone;
        public override int Plants => TileID.Plants;
        public override int TallPlants => TileID.Plants2;
        public override int Vines => TileID.Vines;
        public override int Thorn => -1;

        public override ushort DirtWall => WallID.Dirt;
        public override ushort DirtWallUnsafe => WallID.DirtUnsafe;
        public override ushort CaveWall => WallID.CaveWall;
        public override ushort DirtWallUnsafe1 => WallID.DirtUnsafe1;
        public override ushort DirtWallUnsafe2 => WallID.DirtUnsafe2;
        public override ushort DirtWallUnsafe3 => WallID.DirtUnsafe3;
        public override ushort DirtWallUnsafe4 => WallID.DirtUnsafe4;
        public override ushort StoneWall => WallID.Stone;
        public override ushort HardenedSandWall => WallID.HardenedSand;
        public override ushort SandstoneWall => WallID.Sandstone;
        public override ushort GrassWall => WallID.Grass;
        public override ushort GrassWallUnsafe => WallID.GrassUnsafe;
        public override ushort FlowerWall => WallID.Flower;
        public override ushort FlowerWallUnsafe => WallID.FlowerUnsafe;

        public override ushort CaveWall1 => 0;
        public override ushort CaveWall2 => 0;
        public override ushort CaveWall3 => 0;
        public override ushort CaveWall4 => 0;
    }
}