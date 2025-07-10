using BossFramework.BInterfaces;
using BossFramework.BModels;
using Terraria;
using TrProtocol.Packets;

public class disablepvpbuff : BaseRegionTagProcessor
{
    public override void Dispose()
    {
        //GetDataHandlers.PlayerBuff -= HandlePlayerBuff;
    }

    public override void Init()
    {
        //GetDataHandlers.PlayerBuff += HandlePlayerBuff;
    }
    public override void OnGetPacket(BRegion region, BEventArgs.PacketEventArgs args)
    {
        if (args.Player.CurrentRegion.Tags.Contains("disable.pvpbuff") && args.PacketType == PacketTypes.PlayerAddBuff && args.Packet is AddPlayerBuff p)
        {
            //TShock.Players.FirstOrDefault(p => p.Name == "Megghy")?.SendMessage($"type: {p.BuffType} pvpbuff: {Main.pvpBuff}, meleebuff: {Main.meleeBuff[p.BuffType]}", color: Microsoft.Xna.Framework.Color.White);
            if (Main.pvpBuff[p.BuffType])
            {
                args.Player.TSPlayer.SendData(PacketTypes.PlayerAddBuff, "", args.Player.Index);
                args.Handled = true;
            }
        }
    }
    /*private void HandlePlayerBuff(object? sender, GetDataHandlers.PlayerBuffEventArgs e)
    {
        if (CurrentRegion.Tags.Contains("disable.pvpbuff"))
        {
            var player = e.Player.GetBPlayer();
            TShock.Players.FirstOrDefault(p => p.Name == "Megghy")?.SendMessage($"type: {e.Type} pvpbuff: {Main.pvpBuff}, meleebuff: {Main.meleeBuff[e.Type]}", color: Microsoft.Xna.Framework.Color.White);
            if (Main.pvpBuff[e.Type] || Main.meleeBuff[e.Type])
            {
                player.TSPlayer.SendData(PacketTypes.PlayerAddBuff, "", player.Index);
                e.Handled = true;
            }
        }
    }*/
}