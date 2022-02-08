using System.Collections.Generic;
using System.Threading.Tasks;
using Terraria;
using TrProtocol.Packets;
using TShockAPI.DB;

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

        public void CreateOrSyncProj(SyncProjectile proj)
        {
            int slot = proj.ProjSlot;
            if(slot == 1000)
            {
                for (int i = 0; i < 1000; i++)
                {
                    if(Projs[slot] is null)
                    {
                        slot = i;
                        break;
                    }    
                }
            }
            if(Projs[proj.ProjSlot] is { } oldProj)
            {

            }
        }
        
    }
}
