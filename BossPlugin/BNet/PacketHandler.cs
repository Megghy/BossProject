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
        public static void OnGetPacket(object? sender, Hooks.MessageBuffer.GetDataEventArgs args)
        {
            if (Netplay.Clients[args.Instance.whoAmI].State < 10 && args.PacketId == 22)
            {
                args.Result = HookResult.Cancel;
                return; //很怪
            }
            if (Handlers.TryGetValue((PacketTypes)args.PacketId, out var handler))
            {
                try
                {
                    args.Instance.reader.BaseStream.Position = args.Start - 2;
                    args.Result = handler.GetPacket(TShock.Players[args.Instance.whoAmI]?.GetBPlayer(), Serializer.Deserialize(args.Instance.reader)) ? HookResult.Cancel : HookResult.Continue;
                }
                catch (Exception ex)
                {
                    BLog.Error($"数据包处理失败{Environment.NewLine}{ex}");
                }
            }
        }
    }
}
