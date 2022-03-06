using BossFramework;
using BossFramework.BInterfaces;
using BossFramework.BModels;
using Terraria;
using TrProtocol.Packets;

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
        if (gameTime % 150 == 0 && region.GetPlayers() is { } plrs && plrs.Length > 0 && region.Tags.Exists(t => t.StartsWith("world")))
        {
            var worldData = ChangePacket(region);
            var data = worldData.SerializePacket();
            plrs.ForEach(p => p.SendRawData(data));
        }
    }
    private static WorldData ChangePacket(BRegion region, WorldData? data = null)
    {
        var worldData = data.HasValue ? data.Value : BUtils.GetCurrentWorldData();

        region.Tags.ForEach(t =>
        {
            switch (t.ToLower())
            {
                case "world.lockday":
                    var bb = new BitsByte();
                    bb[0] = true;
                    worldData.DayAndMoonInfo = bb;
                    worldData.Time = 30000;
                    break;
                case "world.locktime":
                    worldData.Time = int.Parse(t.ToLower().Remove(0, 15));
                    break;
                case "world.lockrain":
                    worldData.Rain = 10;
                    break;
                case "world.nobackground":
                    worldData.WorldSurface = (short)region.OriginRegion.Area.Bottom;
                    worldData.RockLayer = (short)(worldData.WorldSurface + 10);
                    break;
            }
        });

        return worldData;
    }
    public override void OnSendPacket(BRegion region, BEventArgs.PacketEventArgs args)
    {
        if (args.PacketType == PacketTypes.WorldInfo)
        {
            args.Handled = true;
            args.Player.SendRawData(ChangePacket(region, (WorldData)args.Packet).SerializePacket());
        }
    }
    public override void LeaveRegion(BRegion region, BPlayer plr)
    {
        var worldData = BUtils.GetCurrentWorldData();

        region.Tags.ForEach(t =>
        {
            switch (t.ToLower())
            {
                case "world.lockday":
                    worldData.Time = (int)Main.time;
                    break;
                case "world.lockrain":
                    worldData.Rain = Main.maxRaining;
                    break;
                case "world.nobackground":
                    worldData.WorldSurface = (short)Main.worldSurface;
                    worldData.RockLayer = (short)Main.rockLayer;
                    break;
            }
        });

        plr.SendPacket(worldData);
    }
}
