using MultiSCore.Hooks;
using MultiSCore.Model;
using Terraria;
using Terraria.Localization;
using TerrariaApi.Server;
using TShockAPI;

namespace MultiSCore.Services
{
    /// <summary>
    /// 负责拦截、解析和分发所有进出服务器的网络数据包。
    /// </summary>
    public class NetworkService(Config config, SessionManager sessionManager)
    {
        private readonly SessionManager _sessionManager = sessionManager;
        private readonly Config _config = config;
        public static bool IsDebugMode { get; private set; }

        public static void SetDebugMode(bool enabled)
        {
            IsDebugMode = enabled;
            TShock.Log.ConsoleInfo($"[MSC调试] 调试模式已 {(enabled ? "开启" : "关闭")}.");
        }

        /// <summary>
        /// `OnPacketReceived` 事件处理，处理所有从客户端接收的数据包。
        /// </summary>
        public void OnReceiveData(object sender, BossFramework.BNet.PacketReceivedEventArgs args)
        {
            var playerIndex = args.PlayerIndex;
            var packetData = args.Data.Span;
            var packetId = packetData[2];

            try
            {
                var packetType = (PacketTypes)packetId;
                if (packetType == PacketTypes.ConnectRequest)
                {
                    if (HandleConnectRequest(packetData, playerIndex)) args.Handled = true;
                    return;
                }

                var session = _sessionManager.GetSession(playerIndex);
                if (session != null)
                {
                    // 状态检查：在玩家完全连接之前，阻止除关键数据包之外的所有数据包
                    if (packetId != 1 && packetId != 15 && packetId > 12 && packetId != 93 && packetId != 16 && packetId != 42 && packetId != 50 && packetId != 38 && packetId != 68 && packetId != 15 && session.State == SessionState.Handshaking)
                    {
                        args.Handled = true;
                        return;
                    }
                    args.Handled = true;
                    if (session.State == SessionState.Returning)
                    {
                        // 玩家正在返回主服务器，需要过滤掉在返回过程中子服务器发来的无关数据包。
                        switch (packetType)
                        {
                            // 忽略玩家状态、增益、宝箱等信息更新
                            case PacketTypes.PlayerInfo:       // 4
                            case PacketTypes.PlayerMana:       // 16
                            case PacketTypes.PlayerHp:       // 42
                            case PacketTypes.PlayerBuff:    // 50
                            case PacketTypes.ClientUUID:              // 68
                                return;

                            // 如果服务器配置为保留主服背包，则忽略背包更新
                            case PacketTypes.PlayerSlot:       // 5
                                if (!session.TargetServer.RememberHostInventory)
                                {
                                    args.Handled = false;
                                }
                                break;

                            case PacketTypes.ContinueConnecting2: // 6
                                session.Dispose();
                                return;
                        }
                    }
                    else
                    {
                        // 拦截并处理聊天数据包以实现 /msc 等命令和跨服聊天
                        if (packetType == PacketTypes.LoadNetModule && args.Data.Span[3] == 1)
                        {
                            if (HandleChatPacket(args, session)) // 是主服该处理的时候返回true
                            {
                                args.Handled = false;
                            }
                            else
                            {
                                args.Handled = true;
                                _ = session.SendToServerAsync(args.Data, true); // 异步转发
                            }
                        }
                        else
                        {
                            if ((PacketTypes)args.Data.Span[2] == PacketTypes.PlayerUpdate)
                            {
                                args.Handled = false;
                            }
                            // 如果玩家在跨服会话中，则将数据包转发到目标服务器
                            _ = session.SendToServerAsync(args.Data, true); // 异步转发
                        }
                    }

                    if (args.Handled)
                    {
                        if (IsDebugMode) LogPacket("客户端", "主服 (拦截)", playerIndex, packetId);
                    }
                    else
                    {
                        if (IsDebugMode) LogPacket("客户端", "主服", playerIndex, packetId);
                    }
                }
                else
                {
                    //if (IsDebugMode) LogPacket("客户端", "主服", playerIndex, packetId);
                }
                // 如果不是跨服玩家，则数据包正常由游戏处理，无需操作。

            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"<MultiSCore> 接收数据包时出错: {ex.Message}");
                args.Handled = true;
            }
        }

        /// <summary>
        /// `OnPacketSending` 事件处理，处理所有发送到客户端的数据包。
        /// </summary>
        public void OnSendData(object sender, BossFramework.BNet.PacketSendingEventArgs args)
        {
            var playerIndex = args.PlayerIndex;

            // 如果数据包的目标玩家正被代理到其他服务器，则取消发送。
            var session = _sessionManager.GetSession(playerIndex);
            if (session != null)
            {

                if (session.State != SessionState.Returning)
                {
                    var packet = (PacketTypes)args.Data.Span[2];
                    if ((packet == PacketTypes.LoadNetModule && args.Data.Span[3] == 1))
                    {
                        // 聊天包不拦截
                    }
                    else
                    {
                        if (IsDebugMode) LogPacket("主服", "客户端 (拦截)", playerIndex, args.Data.Span[2]);
                        args.Handled = true;
                    }
                }
                else
                {
                    if (IsDebugMode) LogPacket("主服", "客户端", playerIndex, args.Data.Span[2]);
                }
            }
        }

        public void OnGreetPlayer(GreetPlayerEventArgs args)
        {
            var who = args.Who;
            // 检查这个加入的玩家是不是从其他服务器代理过来的
            if (_sessionManager.GetForwardedPlayerInfo(who) != null)
            {
                // 是的，他是一个代理玩家。
                // 向他发送所有 NPC 的数据
                Main.npc.ForEach(n => NetMessage.SendData((int)PacketTypes.NpcUpdate, who, -1, null, n.whoAmI));

                // 触发一个自定义的钩子，通知其他可能的插件，这个玩家已成功切换
                MSCHooks.InvokePlayerFinishSwitch(TShock.Players[who], _sessionManager.GetSession(who));
            }

            // 向所有玩家广播正确的玩家活跃状态
            foreach (var player in TShock.Players)
            {
                if (player == null) continue;

                // 如果玩家正在一个跨服会话中，那么他在本服就是"不活跃"的
                bool isLocallyActive = _sessionManager.GetSession(player.Index) == null;

                foreach (var otherPlayer in TShock.Players)
                {
                    if (otherPlayer == null) continue;
                    // 将 player 的状态广播给 otherPlayer
                    otherPlayer.SendData(PacketTypes.PlayerActive, null, player.Index, isLocallyActive.GetHashCode());
                }
            }
        }

        private bool HandleConnectRequest(ReadOnlySpan<byte> data, int index)
        {
            using var ms = new MemoryStream(data.Slice(3).ToArray());
            using var reader = new BinaryReader(ms);
            var receivedString = reader.ReadString();

            if (receivedString.StartsWith("Terraria"))
            {
                // 普通客户端直连
                if (!_config.AllowDirectJoin)
                {
                    NetMessage.TrySendData((int)PacketTypes.Disconnect, index, -1, NetworkText.FromLiteral("本服务器不允许直接加入。"));
                    return true;
                }
                _sessionManager.SetPlayerTerrariaVersion(index, receivedString.Remove(0, 8));
                return false; // 允许正常处理
            }
            else
            {
                // MultiSCore服务器代理连接
                if (!_config.AllowOthorServerJoin)
                {
                    NetMessage.TrySendData((int)PacketTypes.Disconnect, index, -1, NetworkText.FromLiteral("本服务器不允许其他服务器的玩家加入。"));
                    return true;
                }

                // 密钥作为第一个字符串
                var key = receivedString;
                var sourceServerName = reader.ReadString();
                var playerIp = reader.ReadString();
                var pluginVersion = reader.ReadString();
                var terrariaVersion = reader.ReadString();

                if (key != _config.Key)
                {
                    TShock.Log.ConsoleInfo($"[MultiSCore] 拒绝了一个来自 {sourceServerName} 的连接请求，原因：密钥不匹配。");
                    NetMessage.TrySendData((int)PacketTypes.Disconnect, index, -1, NetworkText.FromLiteral("密钥不匹配。"));
                    return true;
                }

                var info = new ForwardedPlayerInfo
                {
                    SourceServerKey = key,
                    TerrariaVersion = terrariaVersion,
                    PluginVersion = Version.TryParse(pluginVersion, out var v) ? v : new Version()
                };
                _sessionManager.AddForwardedPlayer(index, info);

                // TODO: 触发 PlayerJoin hook
                // MSCHooks.InvokePlayerJoin(...);

                // 伪装成普通玩家连接，让TShock继续处理
                return false;
            }
        }

        private static bool HandleChatPacket(BossFramework.BNet.PacketReceivedEventArgs args, PlayerSession session)
        {
            var player = TShock.Players[args.PlayerIndex];
            var data = args.Data.Span;
            using var ms = new MemoryStream(data.ToArray());
            using var reader = new BinaryReader(ms);
            var packet = BossFramework.BNet.PacketHandler.Serializer.Deserialize(reader);
            if (packet is TrProtocol.Packets.Modules.NetTextModuleC2S netTextModuleC2S)
            {
                if (netTextModuleC2S.Command == "Say")
                {
                    var text = netTextModuleC2S.Text;
                    if (text.StartsWith("//") && !player.ContainsData("MiniWorld.CanEdit"))
                    {
                        player.SendErrorMessage($"[MiniWorld] 你没有权限在这个世界中使用 WorldEdit");
                        return true;
                    }
                    if (text.StartsWith(TShockAPI.Commands.Specifier) || text.StartsWith(TShockAPI.Commands.SilentSpecifier))
                    {
                        var cmdText = text.Split(' ')[0].ToLower();
                        var specifier = text.StartsWith(TShockAPI.Commands.Specifier) ? TShockAPI.Commands.Specifier : TShockAPI.Commands.SilentSpecifier;
                        var cmdName = cmdText[specifier.Length..].ToLower();

                        // 如果是msc命令或在全局命令列表中，则在主服务器上执行
                        if (cmdName == "msc" || session.TargetServer.GlobalCommand.Contains(cmdName))
                        {
                            //TShockAPI.Commands.HandleCommand(player, text);
                            return true; // 已处理，拦截数据包
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return true; // 由主服广播
                    }
                }
            }
            return false; // 不是需要拦截的聊天命令，允许转发
        }

        public static void LogPacket(string source, string destination, int playerIndex, byte packetId, string context = null)
        {
            if (!IsDebugMode) return;

            var packetType = (PacketTypes)packetId;
            var player = (playerIndex >= 0 && playerIndex < Main.maxPlayers) ? TShock.Players[playerIndex] : null;
            var playerName = player?.Name ?? $"索引{playerIndex}";

            string logMessage = string.IsNullOrEmpty(context)
                ? $"[MSC调试] {source} -> {destination}: {packetType}({packetId}) [玩家: {playerName}]"
                : $"[MSC调试] {source} -> {destination} ({context}): {packetType}({packetId}) [玩家: {playerName}]";

            TShock.Log.ConsoleInfo(logMessage);
        }
    }
}