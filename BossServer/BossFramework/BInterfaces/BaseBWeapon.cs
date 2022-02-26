using BossFramework.BModels;
using Microsoft.Xna.Framework;
using TrProtocol.Models;
using TrProtocol.Packets;
using TShockAPI;

namespace BossFramework.BInterfaces
{
    public abstract class BaseBWeapon
    {
        private ItemTweaker? _tweakePacket;
        public ItemTweaker TweakePacket
        {
            get
            {
                if (_tweakePacket is null)
                {
                    var b1 = new ProtocolBitsByte();
                    b1[0] = Color.HasValue;
                    b1[1] = Damage.HasValue;
                    b1[2] = KnockBack.HasValue;
                    b1[3] = AnimationTime.HasValue;
                    b1[4] = UseTime.HasValue;
                    b1[5] = ShootProj.HasValue;
                    b1[6] = ShootSpeed.HasValue;
                    b1[7] = true;
                    var b2 = new ProtocolBitsByte();
                    b2[0] = Width.HasValue;
                    b2[1] = Height.HasValue;
                    b2[2] = Size.HasValue;
                    b2[3] = Ammo.HasValue;
                    b2[4] = UseAmmo.HasValue;
                    b2[5] = NotAmmo.HasValue;
                    var item = new Terraria.Item();
                    item.SetDefaults(ItemID);
                    _tweakePacket = new ItemTweaker()
                    {
                        Bit1 = b1,
                        Bit2 = b2,
                        PackedColor = Color?.PackedValue ?? item.color.PackedValue,
                        Damage = (ushort)(Damage ?? item.damage),
                        Knockback = (ushort)(KnockBack ?? item.knockBack),
                        UseAnimation = (ushort)(AnimationTime ?? item.useAnimation),
                        UseTime = (ushort)(UseTime ?? item.useTime),
                        Shoot = (short)(ShootProj ?? item.shoot),
                        ShootSpeed = ShootSpeed ?? item.shootSpeed,
                        Width = (short)(Width ?? item.width),
                        Height = (short)(Height ?? item.height),
                        Scale = Size ?? item.scale,
                        Ammo = (short)(Ammo ?? item.ammo),
                        UseAmmo = (short)(UseAmmo ?? item.useAmmo),
                        NotAmmo = NotAmmo ?? item.notAmmo,
                    };
                }
                return (ItemTweaker)_tweakePacket;
            }
        }
        public abstract string Name { get; }
        public abstract int ItemID { get; }
        public abstract int Prefix { get; }
        public abstract int Stack { get; }

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
        public virtual bool? NotAmmo { get; }

        /// <summary>
        /// 武器本体击中其他玩家时
        /// </summary>
        /// <param name="from">攻击者</param>
        /// <param name="target">被攻击者</param>
        /// <param name="damage">伤害</param>
        /// <param name="direction">方向</param>
        /// <param name="coolDown">无敌帧</param>
        /// <returns></returns>
        public virtual bool OnHit(BPlayer from, BPlayer target, int damage, byte direction, byte coolDown)
        {
            return false;
        }
        /// <summary>
        /// 武器自身发射弹幕时调用
        /// </summary>
        /// <param name="plr"></param>
        /// <param name="proj"></param>
        /// /// <param name="isDefaultProj">是否为该物品的默认发射弹幕</param>
        /// <returns>返回是否取消自带弹幕发射</returns>
        public virtual bool OnShootProj(BPlayer plr, SyncProjectile proj, Microsoft.Xna.Framework.Vector2 velocity, bool isDefaultProj)
        {
            return false;
        }
        /// <summary>
        /// 武器发出的弹幕消失时调用
        /// </summary>
        /// <param name="plr">攻击者</param>
        /// <param name="killProj">消失的弹幕的信息</param>
        public virtual void OnProjDestroy(BPlayer plr, KillProjectile killProj)
        {
        }
        /// <summary>
        /// 发出的弹幕击中其他玩家时调用
        /// </summary>
        /// <param name="from">攻击者</param>
        /// <param name="target">被攻击者</param>
        /// <param name="proj">弹幕信息</param>
        /// <param name="damage">本次伤害</param>
        /// <param name="direction">方向</param>
        /// <param name="coolDown">无敌时间</param>
        /// <see cref="CreateProj(BPlayer, int, Vector2, Vector2, int, float, float, float)"/>
        /// <returns></returns>
        public virtual bool OnProjHit(BPlayer from, BPlayer target, SyncProjectile proj, int damage, byte direction, byte coolDown)
        {
            return false;
        }
        /// <summary>
        /// 当按住左键时调用, 每游戏帧一次, 每秒60次
        /// </summary>
        /// <param name="plr">攻击者</param>
        /// <param name="gameTime">游戏总时间</param>
        public virtual void OnUseItem(BPlayer plr, long gameTime)
        {

        }

        protected Terraria.Projectile _proj = new();
        public void CreateProj(BPlayer plr, SyncProjectile proj)
        {
            plr.RelesedProjs.Add(new(plr, plr.ProjContext.CreateOrSyncProj(plr, proj, true), this, plr.CurrentRegion));
        }
        public void CreateProj(BPlayer plr, int projID, Vector2 position, Vector2 velocity, int damage = -1, float knockBack = -1, float ai0 = -1, float ai1 = -1)
        {
            _proj.SetDefaults(projID);
            var bb = new ProtocolBitsByte();
            bb[0] = ai0 != -1;
            bb[1] = ai1 != -1;
            bb[3] = true; //bannerid
            bb[4] = damage != -1;
            bb[5] = knockBack != -1;
            bb[6] = true;
            plr.RelesedProjs.Add(new(plr, plr.ProjContext.CreateOrSyncProj(plr, new()
            {
                Bit1 = bb,
                AI1 = ai0 == -1 ? _proj.ai[0] : ai0,
                AI2 = ai1 == -1 ? _proj.ai[1] : ai1,
                Damange = (short)(damage == -1 ? _proj.damage : damage),
                OriginalDamage = (ushort)_proj.originalDamage,
                Knockback = knockBack == -1 ? _proj.knockBack : knockBack,
                PlayerSlot = plr.Index,
                Position = position,
                Velocity = velocity,
                ProjSlot = 1000,
                ProjType = (short)projID,
                BannerId = (ushort)_proj.bannerIdToRespondTo
            }, true), this, plr.CurrentRegion));
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            else if (obj is SyncEquipment syncItem)
                return syncItem.ItemType == ItemID && syncItem.Prefix == Prefix && syncItem.Stack == Stack;
            else if (obj is Terraria.Item item)
                return item.type == ItemID && item.prefix == Prefix && item.stack == Stack;
            else if (obj is NetItem netItem)
                return netItem.NetId == ItemID && netItem.PrefixId == Prefix && netItem.Stack == Stack;
            else
                return false;
        }

        public override int GetHashCode()
            => base.GetHashCode();
    }
}
