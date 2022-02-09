using BossFramework.BModels;
using TrProtocol.Models;
using TrProtocol.Packets;

namespace BossFramework.BInterfaces
{
    public abstract class BaseBWeapon : IBWeapon
    {
        private ItemTweaker _tweakePacket;
        public ItemTweaker TweakePacket
        {
            get
            {
                var b1 = new BitsByte();
                b1[0] = Color.HasValue;
                b1[1] = Damage.HasValue;
                b1[2] = KnockBack.HasValue;
                b1[3] = AnimationTime.HasValue;
                b1[4] = UseTime.HasValue;
                b1[5] = ShootProj.HasValue;
                b1[6] = ShootSpeed.HasValue;
                b1[7] = true;
                var b2 = new BitsByte();
                b2[0] = Width.HasValue;
                b2[1] = Height.HasValue;
                b2[2] = Size.HasValue;
                b2[3] = Ammo.HasValue;
                b2[4] = UseAmmo.HasValue;
                b2[5] = NoAmmo.HasValue;
                _tweakePacket ??= new ItemTweaker()
                {
                    Bit1 = b1,
                    Bit2 = b2,
                    PackedColor = Color?.PackedValue ?? 0,
                    Damage = (ushort)(Damage ?? 0),
                    Knockback = (ushort)(KnockBack ?? 0),
                    UseAnimation = (ushort)(AnimationTime ?? 60),
                    UseTime = (ushort)(UseTime ?? 60),
                    Shoot = (short)(ShootProj ?? 0),
                    ShootSpeed = ShootSpeed ?? 0,
                    Width = (short)(Width ?? 32),
                    Height = (short)(Height ?? 32),
                    Scale = Size ?? 32,
                    Ammo = (short)(Ammo ?? 0),
                    UseAmmo = (short)(UseAmmo ?? 0),
                    NotAmmo = NoAmmo ?? true,
                };
                return _tweakePacket;
            }
        }
        public abstract string Name { get; }
        public abstract int ItemID { get; }
        public abstract int Prefix { get; }
        public virtual int? Stack { get; }

        public virtual int? Width { get; }
        public virtual int? Height { get; }
        public virtual int? Damage { get; }
        public virtual Microsoft.Xna.Framework.Color? Color { get; }
        public virtual int? KnockBack { get; }
        public virtual int? AnimationTime { get; }
        public virtual int? UseTime { get; }
        public virtual int? ShootProj { get; }
        public virtual int? ShootSpeed { get; }
        public virtual int? Size { get; }
        public virtual int? Ammo { get; }
        public virtual int? UseAmmo { get; }
        public virtual bool? NoAmmo { get; }

        public virtual void OnHit(BPlayer from, BPlayer target)
        {
        }

        public virtual void OnProjHit(BPlayer from, BPlayer target, SyncProjectile proj)
        {
        }

        public virtual void OnUseItem(BPlayer plr, long gameTime)
        {

        }
        public override bool Equals(object obj)
        {
            if (obj is Terraria.Item item)
                return item.type == ItemID && item.prefix == Prefix;
            else
                return false;
        }
    }
}
