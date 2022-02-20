using BossFramework.BAttributes;
using BossFramework.BCore;
using BossFramework.BInterfaces;
using BossFramework.BModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TrProtocol;
using TShockAPI;
using static BossFramework.BModels.BEventArgs;

namespace BossFramework.BNet
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
                                    (PacketTypes)((Packet)Activator.CreateInstance(t.BaseType.GetGenericArguments().First())!).Type,
                                    (IPacketHandler)Activator.CreateInstance(t, Array.Empty<object>())!);
                        });
        }
        public static void OnSendData(SendBytesEventArgs args)
        {
            var type = (PacketTypes)args.Buffer[2];
            var plr = TShock.Players[args.Socket.Id]?.GetBPlayer() ?? new(TShock.Players[args.Socket.Id]);
            var reader = new BinaryBufferReader(args.Buffer);
            if (Handlers.TryGetValue(type, out var handler))
            {
                try
                {
                    args.Handled = handler.GetPacket(plr, Serializer.Deserialize(reader));
                }
                catch (Exception ex)
                {
                    BLog.Error($"数据包处理失败{Environment.NewLine}{ex}");
                }
            }
            else
            {
                args.Handled = OnSendPacket(plr, type, reader);
            }
        }
        public static void OnGetData(GetDataEventArgs args)
        {
            if (Netplay.Clients[args.Msg.whoAmI].State < 10 && args.MsgID == PacketTypes.ItemOwner)
            {
                args.Handled = true; //很怪
                return;
            }
            var type = args.MsgID;
            var plr = TShock.Players[args.Msg.whoAmI]?.GetBPlayer() ?? new(TShock.Players[args.Msg.whoAmI]);
            var reader = new BinaryBufferReader(args.Msg.readBuffer, args.Index - 3, BitConverter.ToInt16(args.Msg.readBuffer, args.Index - 3));
            if (Handlers.TryGetValue(type, out var handler))
            {
                try
                {
                    args.Handled = handler.GetPacket(plr, Serializer.Deserialize(reader));
                }
                catch (Exception ex)
                {
                    BLog.Error($"数据包处理失败{Environment.NewLine}{ex}");
                }
            }
            else
            {
                args.Handled = OnGetPacket(plr, type, reader);
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
        internal static bool OnSendPacket(BPlayer plr, PacketTypes type, BinaryBufferReader reader)
        {
            if (SendPacketHandlers.TryGetValue(type, out var list) && list.Any())
            {
                try
                {
                    var args = new PacketEventArgs(plr, type, reader);

                    list.ForEach(h => h.Invoke(args));
                    BRegionSystem.AllBRegion.ForEach(r => BRegionSystem.RegionTagProcessers.ForEach(t => t.OnSendPacket(r, args)));

                    return args.Handled;
                }
                catch (Exception ex)
                {
                    BLog.Error($"发送数据包处理失败{Environment.NewLine}{ex}");
                }
            }
            return false;
        }
        internal static bool OnGetPacket(BPlayer plr, PacketTypes type, BinaryBufferReader reader)
        {
            if (GetPacketHandlers.TryGetValue(type, out var list) && list.Any())
            {
                try
                {
                    var args = new PacketEventArgs(plr, type, reader);

                    list.ForEach(h => h.Invoke(args));
                    BRegionSystem.AllBRegion.ForEach(r => BRegionSystem.RegionTagProcessers.ForEach(t => t.OnGetPacket(r, args)));

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
