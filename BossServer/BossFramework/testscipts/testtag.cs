using BossFramework;
using BossFramework.BInterfaces;
using BossFramework.BModels;
using System.Linq;

public class testtag : BaseRegionTag
{
    public testtag(BRegion region) : base(region)
    {
    }

    public override string Name => "test";

    public override string Description => "锁定白天";
    public override void GameUpdate(long gameTime)
    {
        if(gameTime / 60 == 0 && Players.Any())
        {
            var worldData = BUtils.GetCurrentWorldData();
            worldData.Time = 7200;
            var data = worldData.SerializePacket();
            Players.ForEach(p => p.SendRawData(data));
        }
    }
}
