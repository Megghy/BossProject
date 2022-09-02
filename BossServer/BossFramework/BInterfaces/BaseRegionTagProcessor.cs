using BossFramework.BModels;

namespace BossFramework.BInterfaces
{
    public abstract class BaseRegionTagProcessor : IDisposable
    {
        public bool IncludeChild { get; protected set; } = true;
        public BaseRegionTagProcessor CreateInstance(BRegion region, bool init = true)
        {
            var tag = Activator.CreateInstance(GetType(), new object[] { region }) as BaseRegionTagProcessor;
            if (init)
                tag.Init();
            return tag;
        }
        public abstract void Init();

        public abstract void Dispose();

        public virtual void EnterRegion(BRegion region, BPlayer plr) { }
        public virtual void LeaveRegion(BRegion region, BPlayer plr) { }
        public virtual void GameUpdate(BRegion region, long gameTime) { }
        public virtual void OnGetPacket(BRegion region, BEventArgs.PacketEventArgs args) { }
        public virtual void OnSendPacket(BRegion region, BEventArgs.PacketEventArgs args) { }
    }
}
