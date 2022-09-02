using BossFramework.BInterfaces;
using TrProtocol.Packets;

namespace BossFramework.BModels
{
    public sealed record BWeaponRelesedProj
    {
        public BWeaponRelesedProj(BPlayer owner, SyncProjectile proj, BaseBWeapon fromWeapon, BRegion region = null)
        {
            Owner = owner;
            Proj = proj;
            FromWeapon = fromWeapon;
            Region = region;
            CreateTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        }
        public BPlayer Owner { get; init; }
        public SyncProjectile Proj { get; init; }
        public BaseBWeapon FromWeapon { get; init; }
        public BRegion Region { get; init; }
        public long CreateTime { get; init; }
        public void KillProj()
        {
            Region?.ProjContext.DestroyProj(Proj, false);
        }
    }
}
