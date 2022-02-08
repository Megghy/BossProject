using System.Collections.Generic;
using Terraria;
using TShockAPI.DB;

namespace BossFramework.BModels
{
    public sealed class ProjRedirectContext
    {
        public ProjRedirectContext(Region bindingRegion)
        {
            BindingRegion = bindingRegion;
        }
        public List<Projectile> Projs { get; private set; } = new();
        public Region BindingRegion { get; private set; }
    }
}
