using BossFramework;
using BossFramework.BInterfaces;
using BossFramework.BModels;
using EnchCoreApi.TrProtocol.NetPackets;
using Terraria;
using TShockAPI;
using ProtocalBitByte = TrProtocol.Models.BitsByte;

public class WorldTag : BaseRegionTagProcessor
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
            var worldData = ChangePacket(region);
            var data = worldData.SerializePacket();
            plrs.ForEach(p => p.SendRawData(data));
        }
    }
    private static WorldData ChangePacket(BRegion region, WorldData data = null)
    {
        var worldData = data != null ? data : BUtils.GetCurrentWorldData();
        region.Tags?.ForEach(t =>
        {
            switch (t.ToLower())
            {
                case "world.lockday":
                    var bb = new ProtocalBitByte();
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
                    worldData.WorldSurface = (short)1560;
                    worldData.RockLayer = (short)(worldData.WorldSurface + 10);
                    break;
                case "world.zenith":
                    var flags10 = worldData.EventInfo10;
                    flags10[1] = true;
                    worldData.EventInfo10 = flags10;
                    break;
                case "world.remix":
                    var flags8 = worldData.EventInfo8;
                    flags8[4] = true;
                    worldData.EventInfo8 = flags8;
                    break;
                case "world.getgood":
                    var flags7 = worldData.EventInfo7;
                    flags7[7] = true;
                    worldData.EventInfo7 = flags7;
                    break;
            }
        });

        return worldData;
    }
    public override void OnSendPacket(BRegion region, BEventArgs.PacketEventArgs args)
    {
        if (args.PacketType == PacketTypes.WorldInfo)
        {
            //args.Handled = true;
            args.Player.SendRawData(ChangePacket(region, (WorldData)args.Packet).SerializePacket());
        }
    }
    public override void LeaveRegion(BRegion region, BPlayer plr)
    {
        var worldData = BUtils.GetCurrentWorldData();
        worldData.WorldSurface = (short)Main.worldSurface;
        worldData.RockLayer = (short)Main.rockLayer;
        plr.SendPacket(worldData);
    }
}
