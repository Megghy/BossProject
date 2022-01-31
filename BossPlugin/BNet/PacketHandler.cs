using BossPlugin.BAttributes;
using BossPlugin.BInterfaces;
using BossPlugin.BModels;
using OTAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using TrProtocol;
using TShockAPI;
using static BossPlugin.BModels.EventArgs;

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
        public static void OnGetData(object sender, Hooks.MessageBuffer.GetDataEventArgs args)
        {
            if (Netplay.Clients[args.Instance.whoAmI].State < 10 && args.PacketId == 22)
            {
                args.Result = HookResult.Cancel; //很怪
                return;
            }
            var type = (PacketTypes)args.PacketId;
            var plr = TShock.Players[args.Instance.whoAmI]?.GetBPlayer();
            if (Handlers.TryGetValue(type, out var handler))
            {
                try
                {
                    args.Instance.reader.BaseStream.Position = args.Start - 2;
                    args.Result = handler.GetPacket(plr, Serializer.Deserialize(args.Instance.reader)) ? HookResult.Cancel : HookResult.Continue;
                }
                catch (Exception ex)
                {
                    BLog.Error($"数据包处理失败{Environment.NewLine}{ex}");
                }
            }
            else
            {
                args.Instance.reader.BaseStream.Position = args.Start - 2;
                args.Result = OnGetPacket(plr, type, args.Instance.reader) ? HookResult.Cancel : HookResult.Continue;
            }
        }

        private readonly static Dictionary<PacketTypes, List<Action<PacketEventArgs>>> SendPacketHandlers = new();
        private readonly static Dictionary<PacketTypes, List<Action<PacketEventArgs>>> GetPacketHandlers = new();
        public static void RegisteSendPacketHandler(PacketTypes type, Action<PacketEventArgs> action)
        {
            if (!SendPacketHandlers.ContainsKey(type))
                SendPacketHandlers.Add(type, new() { action });
            else
                SendPacketHandlers[type].Add(action);
        }
        public static void DeregistePacketHandler(Action<PacketEventArgs> action)
        {
            SendPacketHandlers.ForEach(s => s.Value.Remove(action));
            GetPacketHandlers.ForEach(s => s.Value.Remove(action));
        }
        public static void RegisteGetPacketHandler(PacketTypes type, Action<PacketEventArgs> action)
        {
            if (!GetPacketHandlers.ContainsKey(type))
                GetPacketHandlers.Add(type, new() { action });
            else
                GetPacketHandlers[type].Add(action);
        }
        internal static bool OnSendPacket(BPlayer plr, PacketTypes type, BinaryReader reader)
        {
            if (SendPacketHandlers.TryGetValue(type, out var list) && list.Any())
            {
                try
                {
                    var args = new PacketEventArgs(plr, Serializer.Deserialize(reader));
                    list.ForEach(h =>
                    {
                        if (!args.Handled)
                            h.Invoke(args);
                    });
                    return args.Handled;
                }
                catch (Exception ex)
                {
                    BLog.Error($"发送数据包处理失败{Environment.NewLine}{ex}");
                }
            }
            return false;
        }
        internal static bool OnGetPacket(BPlayer plr, PacketTypes type, BinaryReader reader)
        {
            if (GetPacketHandlers.TryGetValue(type, out var list) && list.Any())
            {
                try
                {
                    var args = new PacketEventArgs(plr, Serializer.Deserialize(reader));
                    list.ForEach(h =>
                    {
                        if (!args.Handled)
                            h.Invoke(args);
                    });
                    return args.Handled;
                }
                catch (Exception ex)
                {
                    BLog.Error($"发送数据包处理失败{Environment.NewLine}{ex}");
                }
            }
            return false;
        }
    }
}
