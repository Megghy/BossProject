using BossPlugin.BAttributes;
using BossPlugin.BInterfaces;
using OTAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using TrProtocol;
using TShockAPI;

namespace BossPlugin.BNet
{
    public static class PacketHandler
    {
        public static readonly PacketSerializer Serializer = new(false);

        public static readonly Dictionary<PacketTypes, IPacketHandler> Handlers = new();
        [AutoInit("初始化数据包处理")]
        public static void InitPacketHandlers()
        {
            Assembly.GetExecutingAssembly()
                        .GetTypes()
                        .ForEach(t =>
                        {
                            if (t.BaseType?.Name == "PacketHandlerBase`1")
                                Handlers.Add(
                                    (PacketTypes)((Packet)Activator.CreateInstance(t.BaseType.GetGenericArguments().First())).Type,
                                    (IPacketHandler)Activator.CreateInstance(t, Array.Empty<object>()));
                        });
        }
        public static HookResult OnGetPacket(MessageBuffer buffer, ref byte packetId, ref int readOffset, ref int start, ref int length)
        {
            if (Netplay.Clients[buffer.whoAmI].State < 10 && packetId == 22)
                return HookResult.Cancel; //很怪
            if (Handlers.TryGetValue((PacketTypes)packetId, out var handler))
            {
                try
                {
                    buffer.reader.BaseStream.Position = start - 2;
                    return handler.GetPacket(TShock.Players[buffer.whoAmI]?.GetBPlayer(), Serializer.Deserialize(buffer.reader)) ? HookResult.Cancel : HookResult.Continue;
                }
                catch (Exception ex)
                {
                    BLog.Error($"数据包处理失败{Environment.NewLine}{ex}");
                }
            }
            return HookResult.Continue;
        }
    }
}
