using System.Reflection;
using BossFramework.BAttributes;
using BossFramework.BCore;
using BossFramework.BInterfaces;
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

        public static readonly Dictionary<PacketTypes, IPacketHandler> Handlers = [];
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
                                    (IPacketHandler)Activator.CreateInstance(t, [])!);
                        });
        }
        internal static void OnSendData(SendBytesEventArgs args)
        {
            try
            {
                using var stream = new MemoryStream(args.Buffer);
                using var reader = new BinaryReader(stream);
                args.Handled = HandleSendData(args.Handled, new PacketEventArgs(TShock.Players[args.Socket.Id]?.GetBPlayer() ?? new(TShock.Players[args.Socket.Id]),
                    (PacketTypes)args.Buffer[2], reader
                    )
                {
                    Handled = args.Handled
                });
            }
            catch (Exception ex)
            {
                BLog.Error($"数据包发送处理失败{Environment.NewLine}{ex}");
            }
        }
        internal static bool HandleSendData(bool gameHandled, PacketEventArgs args)
        {
            if (Handlers.TryGetValue(args.PacketType, out var handler))
                args.Handled = handler.SendPacket(args.Player, args.Packet);
            if (SendPacketHandlers.TryGetValue(args.PacketType, out var list))
                list.ForEach(h => h.Invoke(args));
            BRegionSystem.AllBRegion.Where(r => r.IsPlayerInThis(args.Player)).ForEach(r => BRegionSystem.RegionTagProcessers.ForEach(t => t.OnSendPacket(r, args)));

            return gameHandled || args.Handled;
        }
        internal static void OnGetData(GetDataEventArgs args)
        {
            if (Netplay.Clients[args.Msg.whoAmI].State < 10 && args.MsgID == PacketTypes.ItemOwner)
            {
                args.Handled = true; //很怪
                return;
            }
            try
            {
                using var stream = new MemoryStream(args.Msg.readBuffer, args.Index - 3, BitConverter.ToInt16(args.Msg.readBuffer, args.Index - 3));
                using var reader = new BinaryReader(stream);
                var packetArgs = new PacketEventArgs(TShock.Players[args.Msg.whoAmI]?.GetBPlayer() ?? new(TShock.Players[args.Msg.whoAmI]),
                    args.MsgID,
                    reader)
                {
                    Handled = args.Handled
                };

                if (Handlers.TryGetValue(packetArgs.PacketType, out var handler))
                    packetArgs.Handled = handler.GetPacket(packetArgs.Player, packetArgs.Packet);
                if (GetPacketHandlers.TryGetValue(packetArgs.PacketType, out var list))
                    list.ForEach(h => h.Invoke(packetArgs));
                BRegionSystem.AllBRegion.Where(r => r.IsPlayerInThis(packetArgs.Player)).ForEach(r => BRegionSystem.RegionTagProcessers.ForEach(t => t.OnGetPacket(r, packetArgs)));

                args.Handled = packetArgs.Handled || args.Handled;
            }
            catch (Exception ex)
            {
                BLog.Error($"数据包接收处理失败{Environment.NewLine}{ex}");
            }
        }

        internal readonly static Dictionary<PacketTypes, List<Action<PacketEventArgs>>> SendPacketHandlers = new();
        internal readonly static Dictionary<PacketTypes, List<Action<PacketEventArgs>>> GetPacketHandlers = new();
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

        public static void RegisteGetPacketHandler<T>(Action<PacketHookArgs<Packet>> action) where T : Packet
        {
        }
    }
}
