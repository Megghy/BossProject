using System.ComponentModel;
using Terraria;

namespace TerrariaApi.Server
{
    public class ProjectileAiUpdateEventArgs : HandledEventArgs
    {
        public Projectile Projectile { get; set; }
    }
}
