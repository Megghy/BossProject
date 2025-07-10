using BossFramework.BInterfaces;
using BossFramework.BModels;
using Terraria.ID;
using TrProtocol.Packets;
using Vector2 = Microsoft.Xna.Framework.Vector2;

public class testweapon2 : BaseBWeapon
{
    public override string Name => "test5";
    public override int ItemID => 1266;
    public override int Prefix => 8;
    public override int Stack => 1;

    bool isRunning = false;
    public override bool OnShootProj(BPlayer plr, SyncProjectile proj, Vector2 velocity, bool isDefaultProj)
    {
        plr.TSPlayer.SendInfoMessage("1");
        if (proj.ProjType != 985)
            return false;
        if (isRunning)
            return false;
        isRunning = true;
        var vecp = new Microsoft.Xna.Framework.Vector2(plr.TSPlayer.X, plr.TSPlayer.Y);
        Task.Run(async () =>
        {
            for (var t = 0; t < 10; t++)
            {
                for (var i = -4; i < 5; i++)
                {
                    plr.SendMsg($"{t}, {i}");

                    CreateProj(plr, ProjectileID.SwordBeam, plr.TRPlayer.position + new Vector2(i * 40, -80) + new Vector2(10, 10), new Vector2(0, 13), 70, 1);

                    var projId = Terraria.Projectile.NewProjectile(Terraria.Projectile.GetNoneSource(), vecp + new Vector2(i * 40, -80) + new Vector2(10, 10), new Vector2(0, 13), 116, 70, 1, plr.Index);
                    Terraria.NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, projId);
                    projId = Terraria.Projectile.NewProjectile(new Terraria.DataStructures.EntitySource_DebugCommand(), vecp + new Vector2(-i * 40, -80) + new Vector2(10, 10), new Vector2(0, -13), 116, 70, 1, plr.Index);
                    Terraria.NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, projId);
                    await Task.Delay(50);
                }
            }
            isRunning = false;
        });
        return false;
    }
}
