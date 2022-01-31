using System;

namespace TerrariaApi.Server
{
    public sealed class NpcKilledEventArgs : EventArgs
    {
        public Terraria.NPC npc { get; internal set; }
    }
}
