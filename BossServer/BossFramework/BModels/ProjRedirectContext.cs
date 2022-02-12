using BossFramework.BCore;
using System;
using System.Linq;
using TrProtocol.Packets;

namespace BossFramework.BModels
{
    public sealed class ProjRedirectContext
    {
        public ProjRedirectContext(BRegion bindingRegion)
        {
            BindingRegion = bindingRegion;
        }
        public SyncProjectile[] Projs { get; private set; } = new SyncProjectile[1001];
        public BRegion BindingRegion { get; private set; }

        /// <summary>
        /// 在当前弹幕上下文中创建弹幕
        /// </summary>
        /// <param name="from"></param>
        /// <param name="proj"></param>
        /// <returns>弹幕编号</returns>
        public SyncProjectile CreateOrSyncProj(BPlayer from, SyncProjectile proj, bool sendToSelf = false)
        {
            try
            {
                lock (Projs)
                {
                    int slot = proj.ProjSlot;

                    if (slot >= 1000) //创建弹幕
                    {
                        for (int i = 0; i < 1000; i++)
                        {
                            if (Projs[i] is null)
                            {
                                slot = i;
                                break;
                            }
                        }
                        if (slot >= 1000)
                            slot = 0;
                        proj.ProjSlot = (short)slot;
                    }
                    BLog.DEBUG($"弹幕同步: {BindingRegion}:[{proj.ProjSlot}] {Projs.Where(p => p != null).Count()}");
                    Projs[slot] = proj;
                    var rawData = proj.SerializePacket();
                    var all = BindingRegion.GetAllPlayerInRegion();
                    (sendToSelf ? all : all.Where(p => p != from))
                        .BForEach(p => p.TsPlayer?.SendRawData(rawData));
                }
            }
            catch (Exception ex)
            {
                BLog.Warn(ex);
            }
            return proj;
        }
        public void DestroyProj(BPlayer from, short projSlot, bool ignoreSelf = true)
            => DestroyProj(new KillProjectile() { PlayerSlot = from.Index, ProjSlot = projSlot }, ignoreSelf);
        public void DestroyProj(SyncProjectile proj, bool ignoreSelf = true)
            => DestroyProj(new KillProjectile() { PlayerSlot = proj.PlayerSlot, ProjSlot = proj.ProjSlot }, ignoreSelf);
        public void DestroyProj(KillProjectile killProj, bool ignoreSelf = true)
        {
            if (Projs[killProj.ProjSlot]?.PlayerSlot != killProj.PlayerSlot)
                return;
            BLog.DEBUG($"弹幕移除: {BindingRegion}:[{killProj.ProjSlot}]");
            Projs[killProj.ProjSlot] = null;
            var rawData = killProj.SerializePacket();
            var plrs = BindingRegion.GetAllPlayerInRegion();
            if (ignoreSelf)
                plrs.Where(p => p.Index != killProj.PlayerSlot)
                .BForEach(p => p.TsPlayer?.SendRawData(rawData));
            else
                plrs.BForEach(p => p.TsPlayer?.SendRawData(rawData));
        }
    }
}
