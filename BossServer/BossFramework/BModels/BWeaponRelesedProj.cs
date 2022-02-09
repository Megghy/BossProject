using BossFramework.BInterfaces;
using TrProtocol.Packets;

namespace BossFramework.BModels
{
    public class BWeaponRelesedProj : SyncProjectile
    {
        public BWeaponRelesedProj(SyncProjectile proj, BaseBWeapon fromWeapon)
        {
            this.Bit1 = proj.Bit1;
            this.AI1 = proj.AI1;
            this.AI2 = proj.AI2;
            this.BannerId = proj.BannerId;
            this.Damange = proj.Damange;
            this.Knockback = proj.Knockback;
            this.OriginalDamage = proj.OriginalDamage;
            this.PlayerSlot = proj.PlayerSlot;
            this.ProjSlot = proj.ProjSlot;
            this.BannerId = proj.BannerId;
            this.Position = proj.Position;
            this.Velocity = proj.Velocity;
            this.ProjType = proj.ProjType;
            this.UUID = proj.UUID;
            FromWeapon = fromWeapon;
        }
        public BaseBWeapon FromWeapon { get; private set; }
    }
}
