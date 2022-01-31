using Terraria.ID;
namespace WorldEdit.Commands.Biomes
{
    public class Crimson : Biome
    {
        public override int Dirt => TileID.Dirt;
        public override int[] Grass => new int[] { TileID.CrimsonGrass };
        public override int Stone => TileID.Crimstone;
        public override int Ice => TileID.FleshIce;
        public override int Clay => TileID.ClayBlock;
        public override int Sand => TileID.Crimsand;
        public override int HardenedSand => TileID.CrimsonHardenedSand;
        public override int Sandstone => TileID.CrimsonSandstone;
        public override int Plants => TileID.CrimsonPlants;
        public override int TallPlants => -1;
        public override int Vines => TileID.CrimsonVines;
        public override int Thorn => TileID.CrimsonThorns;

        public override ushort DirtWall => WallID.Dirt;
        public override ushort DirtWallUnsafe => WallID.DirtUnsafe;
        public override ushort CaveWall => WallID.CaveWall;
        public override ushort DirtWallUnsafe1 => WallID.DirtUnsafe1;
        public override ushort DirtWallUnsafe2 => WallID.DirtUnsafe2;
        public override ushort DirtWallUnsafe3 => WallID.DirtUnsafe3;
        public override ushort DirtWallUnsafe4 => WallID.DirtUnsafe4;
        public override ushort StoneWall => WallID.CrimstoneUnsafe;
        public override ushort HardenedSandWall => WallID.CrimsonHardenedSand;
        public override ushort SandstoneWall => WallID.CrimsonSandstone;
        public override ushort GrassWall => WallID.CrimsonGrassUnsafe;
        public override ushort GrassWallUnsafe => WallID.CrimsonGrassUnsafe;
        public override ushort FlowerWall => WallID.CrimsonGrassUnsafe;
        public override ushort FlowerWallUnsafe => WallID.CrimsonGrassUnsafe;

        public override ushort CaveWall1 => WallID.CrimsonUnsafe1;
        public override ushort CaveWall2 => WallID.CrimsonUnsafe2;
        public override ushort CaveWall3 => WallID.CrimsonUnsafe3;
        public override ushort CaveWall4 => WallID.CrimsonUnsafe4;
    }
}