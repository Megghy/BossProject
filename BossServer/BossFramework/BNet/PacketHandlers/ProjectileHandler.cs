using BossFramework.BAttributes;
using BossFramework.BInterfaces;
using BossFramework.BModels;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using TrProtocol.Packets;

namespace BossFramework.BNet.PacketHandlers
{
    public class SyncProjectileHandler : PacketHandlerBase<SyncProjectile>
    {
        [AutoInit]
        public static void InitProjQueue()
        {
            Task.Run(ProcessProjLoop);
        }
        private static void ProcessProjLoop()
        {
            while (!TShockAPI.TShock.ShuttingDown)
            {
                if (ProjQueue.TryDequeue(out var proj))
                {
                    if (proj.UUID >= 1000)
                        proj.UUID = -1;
                    if (proj.ProjType == 949)
                        proj.PlayerSlot = 255;
                    else if (Main.projHostile[proj.ProjType])
                        continue;
                    int projSlot = 1000;
                    for (int num = 0; num < 1000; num++)
                    {
                        if (Main.projectile[num].owner == proj.PlayerSlot && Main.projectile[num].identity == proj.ProjSlot && Main.projectile[num].active)
                        {
                            projSlot = num;
                            break;
                        }
                    }
                    if (projSlot == 1000)
                    {
                        for (int num = 0; num < 1000; num++)
                        {
                            if (!Main.projectile[num].active)
                            {
                                projSlot = num;
                                break;
                            }
                        }
                    }
                    if (projSlot == 1000)
                    {
                        projSlot = Projectile.FindOldestProjectile();
                    }
                    Projectile projectile = Main.projectile[projSlot];
                    if (!projectile.active || projectile.type != proj.ProjType)
                    {
                        projectile.SetDefaults(proj.ProjType);
                        Netplay.Clients[proj.PlayerSlot].SpamProjectile += 1f;
                    }
                    projectile.identity = proj.ProjSlot;
                    projectile.position = proj.Position.Get();
                    projectile.velocity = proj.Velocity.Get();
                    projectile.type = proj.ProjType;
                    projectile.damage = proj.Damange;
                    projectile.bannerIdToRespondTo = proj.BannerId;
                    projectile.originalDamage = proj.OriginalDamage;
                    projectile.knockBack = proj.Knockback;
                    projectile.owner = proj.PlayerSlot;
                    projectile.ai[0] = proj.AI1;
                    projectile.ai[1] = proj.AI2;
                    if (proj.UUID >= 0)
                    {
                        projectile.projUUID = proj.UUID;
                        Main.projectileIdentity[proj.PlayerSlot, proj.UUID] = projSlot;
                    }
                    projectile.ProjectileFixDesperation();
                    NetMessage.TrySendData(27, -1, proj.PlayerSlot, null, projSlot);
                    //BUtils.SendPacketToAll(proj, proj.PlayerSlot);
                }
                else
                    Thread.Sleep(1);
            }
        }
        public static readonly ConcurrentQueue<SyncProjectile> ProjQueue = new();
        public override bool OnGetPacket(BPlayer plr, SyncProjectile packet)
        {
            bool handled = base.OnGetPacket(plr, packet);
            if (BConfig.Instance.EnableProjQueue && !handled)
            {
                packet.PlayerSlot = plr.Index;
                ProjQueue.Enqueue(packet);
                return true;
            }
            else
                return handled;
        }

        public override bool OnSendPacket(BPlayer plr, SyncProjectile packet)
        {
            return base.OnSendPacket(plr, packet);
        }
    }
    public class DestroyProjectileHandler : PacketHandlerBase<KillProjectile>
    {
        [AutoInit]
        public static void InitProjQueue()
        {
            Task.Run(ProcessProjLoop);
        }
        private static void ProcessProjLoop()
        {
            while (!TShockAPI.TShock.ShuttingDown)
            {
                if (ProjQueue.TryDequeue(out var proj))
                {
                    for (int num = 0; num < 1000; num++)
                    {
                        if (Main.projectile[num].owner == proj.PlayerSlot && Main.projectile[num].identity == proj.ProjSlot && Main.projectile[num].active)
                        {
                            Main.projectile[num].Kill();
                            break;
                        }
                    }
                    NetMessage.TrySendData(29, -1, proj.PlayerSlot, null, proj.ProjSlot, proj.PlayerSlot);
                    //BUtils.SendPacketToAll(proj, proj.PlayerSlot);
                }
                else
                    Thread.Sleep(1);
            }
        }
        public static readonly ConcurrentQueue<KillProjectile> ProjQueue = new();
        public override bool OnGetPacket(BPlayer plr, KillProjectile packet)
        {
            bool handled = base.OnGetPacket(plr, packet);
            if (BConfig.Instance.EnableProjQueue && !handled)
            {
                packet.PlayerSlot = plr.Index;
                ProjQueue.Enqueue(packet);
                return true;
            }
            else
                return handled;
        }

        public override bool OnSendPacket(BPlayer plr, KillProjectile packet)
        {
            return base.OnSendPacket(plr, packet);
        }
    }
}
