using BossFramework.BInterfaces;
using BossFramework.BModels;
using TrProtocol.Packets;
using Vector2 = Microsoft.Xna.Framework.Vector2;

public class testweapon : BaseBWeapon
{
    public override string Name => "test";
    public override int ItemID => 757;
    public override int Prefix => 0;
    public override int Stack => 1;

    public override int? Damage => 10;

    public long lastUse = 0;
    public override void OnUseItem(BPlayer plr, long gameTime)
    {
    }
    public override bool OnShootProj(BPlayer plr, SyncProjectile proj, Vector2 velocity, bool isDefaultProj)
    {
        if (isDefaultProj)
        {
            CreateProj(plr, 950, plr.TrPlayer.position, velocity, 0);
            return true;
        }
        return false;
    }
    public override bool OnHit(BPlayer from, BPlayer target, int damage, byte direction, byte coolDown)
    {
        return false;
    }
    public override bool OnProjHit(BPlayer from, BPlayer target, SyncProjectile proj, int damage, byte direction, byte coolDown)
    {
        target.TsPlayer.Heal(10);
        return false;
    }
    public override void OnProjDestroy(BPlayer plr, KillProjectile killProj)
    {

    }
}
