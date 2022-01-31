using Terraria.ID;
namespace WorldEdit.Commands.Biomes
{
    public class Jungle : Biome
    {
        public override int Dirt => TileID.Mud;
        public override int[] Grass => new int[] { TileID.JungleGrass };
        public override int Stone => TileID.Stone;
        public override int Ice => TileID.Stone;
        public override int Clay => TileID.ClayBlock;
        public override int Sand => TileID.Sand;
        public override int HardenedSand => TileID.HardenedSand;
        public override int Sandstone => TileID.Sandstone;
        public override int Plants => TileID.JunglePlants;
        public override int TallPlants => TileID.JunglePlants2;
        public override int Vines => TileID.JungleVines;
        public override int Thorn => TileID.JungleThorns;

        public override ushort DirtWall => WallID.MudUnsafe;
        public override ushort DirtWallUnsafe => WallID.MudUnsafe;
        public override ushort CaveWall => WallID.MudUnsafe;
        public override ushort DirtWallUnsafe1 => WallID.MudUnsafe;
        public override ushort DirtWallUnsafe2 => WallID.MudUnsafe;
        public override ushort DirtWallUnsafe3 => WallID.MudUnsafe;
        public override ushort DirtWallUnsafe4 => WallID.MudUnsafe;
        public override ushort StoneWall => WallID.Stone;
        public override ushort HardenedSandWall => WallID.HardenedSand;
        public override ushort SandstoneWall => WallID.Sandstone;
        public override ushort GrassWall => WallID.Jungle;
        public override ushort GrassWallUnsafe => WallID.JungleUnsafe;
        public override ushort FlowerWall => WallID.Flower;
        public override ushort FlowerWallUnsafe => WallID.FlowerUnsafe;

        public override ushort CaveWall1 => WallID.JungleUnsafe1;
        public override ushort CaveWall2 => WallID.JungleUnsafe2;
        public override ushort CaveWall3 => WallID.JungleUnsafe3;
        public override ushort CaveWall4 => WallID.JungleUnsafe4;
    }
}