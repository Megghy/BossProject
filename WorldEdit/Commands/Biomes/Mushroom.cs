using Terraria.ID;
namespace WorldEdit.Commands.Biomes
{
    public class Mushroom : Biome
    {
        public override int Dirt => TileID.Mud;
        public override int[] Grass => new int[] { TileID.MushroomGrass };
        public override int Stone => TileID.Stone;
        public override int Ice => TileID.Stone;
        public override int Clay => TileID.ClayBlock;
        public override int Sand => TileID.Mud;
        public override int HardenedSand => TileID.Mud;
        public override int Sandstone => TileID.Stone;
        public override int Plants => TileID.MushroomPlants;
        public override int TallPlants => -1;
        public override int Vines => -1;
        public override int Thorn => -1;

        public override ushort DirtWall => WallID.Mushroom;
        public override ushort DirtWallUnsafe => WallID.Mushroom;
        public override ushort CaveWall => WallID.Mushroom;
        public override ushort DirtWallUnsafe1 => WallID.Mushroom;
        public override ushort DirtWallUnsafe2 => WallID.Mushroom;
        public override ushort DirtWallUnsafe3 => WallID.Mushroom;
        public override ushort DirtWallUnsafe4 => WallID.Mushroom;
        public override ushort StoneWall => WallID.Stone;
        public override ushort HardenedSandWall => WallID.Mushroom;
        public override ushort SandstoneWall => WallID.Mushroom;
        public override ushort GrassWall => WallID.Mushroom;
        public override ushort GrassWallUnsafe => WallID.MushroomUnsafe;
        public override ushort FlowerWall => WallID.Mushroom;
        public override ushort FlowerWallUnsafe => WallID.MushroomUnsafe;

        public override ushort CaveWall1 => 0;
        public override ushort CaveWall2 => 0;
        public override ushort CaveWall3 => 0;
        public override ushort CaveWall4 => 0;
    }
}