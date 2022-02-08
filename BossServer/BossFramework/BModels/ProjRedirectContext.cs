using BossFramework.BCore;
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
        public SyncProjectile[] Projs { get; private set; } = new SyncProjectile[1000];
        public BRegion BindingRegion { get; private set; }

        public void CreateOrSyncProj(BPlayer from, SyncProjectile proj)
        {
            int slot = proj.ProjSlot;
            if (slot == 1000) //创建弹幕
            {
                for (int i = 0; i < 1000; i++)
                {
                    if (Projs[slot] is null)
                    {
                        slot = i;
                        break;
                    }
                }
                if (slot == 1000)
                    slot = 0;
                proj.ProjSlot = (short)slot;
            }
            BLog.DEBUG($"弹幕同步: {BindingRegion}:[{proj.ProjSlot}]");
            Projs[slot] = proj;
            var rawData = proj.SerializePacket();
            BindingRegion.GetAllPlayerInRegion()
                .Where(p => p != from)
                .ForEach(p => p.TsPlayer?.SendRawData(rawData));
        }
        public void DestroyProj(BPlayer from, short projSlot, bool ignoreSelf = true)
            => DestroyProj(new KillProjectile() { PlayerSlot = from.Index, ProjSlot = projSlot }, ignoreSelf);
        public void DestroyProj(SyncProjectile proj, bool ignoreSelf = true)
            => DestroyProj(new KillProjectile() { PlayerSlot = proj.PlayerSlot, ProjSlot = proj.ProjSlot }, ignoreSelf);
        public void DestroyProj(KillProjectile killProj, bool ignoreSelf = true)
        {
            BLog.DEBUG($"弹幕移除: {BindingRegion}:[{killProj.ProjSlot}]");
            Projs[killProj.ProjSlot] = null;
            var rawData = killProj.SerializePacket();
            var plrs = BindingRegion.GetAllPlayerInRegion();
            if (ignoreSelf)
                plrs.Where(p => p.Index != killProj.PlayerSlot)
                .ForEach(p => p.TsPlayer?.SendRawData(rawData));
            else
                plrs.ForEach(p => p.TsPlayer?.SendRawData(rawData));
        }
    }
}
