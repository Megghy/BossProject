using BossFramework;
using BossFramework.BInterfaces;
using BossFramework.BModels;
using System.Linq;
using Terraria;
using TrProtocol.Packets;
using ProtocalBitByte = TrProtocol.Models.BitsByte;

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
            var worldData = ChangePacket(region);
            var data = worldData.SerializePacket();
            plrs.ForEach(p => p.SendRawData(data));
        }
    }
    private static WorldData ChangePacket(BRegion region, WorldData? data = null)
    {
        var worldData = data == null ? data : BUtils.GetCurrentWorldData();
        region.Tags.ForEach(t =>
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
                    worldData.WorldSurface = (short)region.OriginRegion.Area.Bottom;
                    worldData.RockLayer = (short)(worldData.WorldSurface + 20);
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
public class playertag : BaseRegionTagProcessor
{
    public override void Dispose()
    {
    }

    public override void Init()
    {
    }

    public override void GameUpdate(BRegion region, long gameTime)
    {
        base.GameUpdate(region, gameTime);
    }
    public override void EnterRegion(BRegion region, BPlayer plr)
    {
        var result = CheckBuff(region, plr.TrPlayer.buffType);
        if (result.Item1)
        {
            plr.SendPacket(BUtils.GetCurrentWorldData(true));
            plr.SendPacket(new PlayerBuffs() { BuffTypes = result.Item2, PlayerSlot = plr.Index });
        }
    }

    public override void OnGetPacket(BRegion region, BEventArgs.PacketEventArgs args)
    {
        if (args.PacketType == PacketTypes.PlayerAddBuff)
        {
            var bannedBuffs = region.Tags.Where(t => t.StartsWith("player.banbuff.")).Select(t => int.TryParse(t.Replace("player.banbuff.", ""), out var buffId) ? buffId : -1);
            if (bannedBuffs.Contains(((AddPlayerBuff)args.Packet).BuffType))
            {
                var result = CheckBuff(region, args.Player.TrPlayer.buffType);
                if (result.Item1)
                {
                    args.Player.SendPacket(BUtils.GetCurrentWorldData(true));
                    args.Player.SendPacket(new PlayerBuffs() { BuffTypes = result.Item2, PlayerSlot = args.Player.Index });
                    args.Handled = true;
                }
            }
        }
        if (args.PacketType == PacketTypes.PlayerBuff)
        {
            Console.WriteLine(String.Join(", ", ((PlayerBuffs)args.Packet).BuffTypes.Select(b => b.ToString())));
            var result = CheckBuff(region, ((PlayerBuffs)args.Packet).BuffTypes.Select(b => (int)b).ToArray());
            if (result.Item1)
            {
                args.Player.SendPacket(BUtils.GetCurrentWorldData(true));
                BUtils.SendPacketToAll(new PlayerBuffs() { BuffTypes = result.Item2, PlayerSlot = args.Player.Index });
                args.Handled = true;
            }
        }
    }
    public (bool, ushort[]) CheckBuff(BRegion region, int[] buffIds)
    {
        var bannedBuff = region.Tags.Where(t => t.StartsWith("player.banbuff.")).Select(t => int.TryParse(t.Replace("player.banbuff.", ""), out var buffId) ? buffId : -1);
        if (!bannedBuff.Any())
            return (false, Array.Empty<ushort>());
        var list = new ushort[22];
        bool have = false;
        for (var i = 0; i < 22; i++)
        {
            if (bannedBuff.Contains(buffIds[i]))
            {
                have = true;
                list[i] = 0;
            }
            else
                list[i] = (ushort)buffIds[i];
        }
        return (have, list);
    }
}
