using BossFramework.BModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BossFramework.BInterfaces
{
    public abstract class BaseRegionTag : IDisposable
    {
        public BaseRegionTag(BRegion region) { Region = region; }
        public abstract string Name { get; }
        public abstract string Description { get; }
        public bool IncludeChild { get; protected set; } = true;
        public BRegion Region { get; protected set; }
        public BPlayer[] Players 
            => Region.GetPlayers(IncludeChild);
        public BaseRegionTag CreateInstance(BRegion region)
        {
            return Activator.CreateInstance(GetType(), new object[] { region }) as BaseRegionTag;
        }

        public virtual void Dispose()
        {
            Region = null;
        }

        public virtual void EnterRegion(BPlayer plr)
        {

        }
        public virtual void LeaveRegion(BPlayer plr)
        {

        }
        public virtual void GameUpdate(long gameTime)
        {

        }
    }
}
