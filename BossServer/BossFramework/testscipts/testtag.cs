using BossFramework;
using BossFramework.BInterfaces;
using BossFramework.BModels;
using System;

public class testtag : BaseRegionTagProcessor
{

    public override void Init()
    {
    }
    public override void Dispose()
    {
    }
    public override void GameUpdate(BRegion region, long gameTime)
    {
        if (gameTime % 60 == 0 && region.GetPlayers() is { } plrs && plrs.Length > 0 && region.Tags.Exists(t => t.StartsWith("world")))
        {
            var worldData = BUtils.GetCurrentWorldData();
            Console.WriteLine($"{region.Name}");

            region.Tags.ForEach(t =>
            {
                switch (t.ToLower())
                {
                    case "world.lockday":
                        worldData.Time = 7200;
                        break;
                    case "world.lockrain":
                        worldData.Rain = 10;
                        break;
                }
            });

            var data = worldData.SerializePacket();
            plrs.ForEach(p => p.SendRawData(data));
        }
    }
}
