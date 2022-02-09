using Terraria;

namespace Colosseo
{
    internal static class Extensions
    {
        public static int GetTileX(this NPC npc)
        {
            return (int)npc.position.X / 16;
        }

        public static int GetTileY(this NPC npc)
        {
            return (int)npc.position.Y / 16;
        }
    }
}
