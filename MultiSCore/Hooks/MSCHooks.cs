using System.ComponentModel;
using MultiSCore.Model;
using TShockAPI;

namespace MultiSCore.Hooks
{
    /// <summary>
    /// 提供MultiSCore插件的自定义事件钩子，以实现扩展性。
    /// </summary>
    public static class MSCHooks
    {
        #region Event Args

        /// <summary>
        /// 玩家通过MultiSCore加入服务器事件的参数。
        /// </summary>
        public class PlayerJoinEventArgs : HandledEventArgs
        {
            public int PlayerIndex { get; }
            public string ServerName { get; }
            public string ServerKey { get; }
            public string PlayerIP { get; }
            public Version PluginVersion { get; }
            public string TerrariaVersion { get; }

            public PlayerJoinEventArgs(int index, string name, string key, string ip, string version, string trVersion)
            {
                PlayerIndex = index;
                ServerName = name;
                ServerKey = key;
                PlayerIP = ip;
                Version.TryParse(version, out var v);
                PluginVersion = v;
                TerrariaVersion = trVersion;
            }
        }

        /// <summary>
        /// 收到自定义数据包事件的参数。
        /// </summary>
        public class RecieveCustomDataEventArgs : HandledEventArgs
        {
            public int PlayerIndex { get; }
            public TSPlayer Player => TShock.Players[PlayerIndex];
            public CustomPacketType PacketType { get; }
            public BinaryReader Reader { get; }

            public RecieveCustomDataEventArgs(int index, CustomPacketType type, BinaryReader reader)
            {
                PlayerIndex = index;
                PacketType = type;
                Reader = reader;
            }
        }

        /// <summary>
        /// 玩家准备切换服务器之前的事件参数。可用于阻止切换。
        /// </summary>
        public class PlayerReadyToSwitchEventArgs : HandledEventArgs
        {
            public TSPlayer Player { get; }
            public PlayerReadyToSwitchEventArgs(TSPlayer plr)
            {
                Player = plr;
            }
        }

        /// <summary>
        /// 玩家成功完成服务器切换之后的事件参数。
        /// </summary>
        public class PlayerFinishSwitchEventArgs : HandledEventArgs
        {
            public TSPlayer Player { get; }
            public PlayerSession Session { get; }
            public PlayerFinishSwitchEventArgs(TSPlayer player, PlayerSession session)
            {
                Player = player;
                Session = session;
            }
        }

        #endregion

        #region Events & Invokers

        public static event EventHandler<PlayerJoinEventArgs> PlayerJoin;
        public static event EventHandler<RecieveCustomDataEventArgs> RecieveCustomData;
        public static event EventHandler<PlayerReadyToSwitchEventArgs> PlayerReadyToSwitch;
        public static event EventHandler<PlayerFinishSwitchEventArgs> PlayerFinishSwitch;

        internal static bool InvokePlayerJoin(int index, string name, string key, string ip, string version, string trVersion)
        {
            var args = new PlayerJoinEventArgs(index, name, key, ip, version, trVersion);
            PlayerJoin?.Invoke(null, args);
            return args.Handled;
        }

        internal static bool InvokeRecieveCustomData(int index, CustomPacketType type, BinaryReader reader)
        {
            var args = new RecieveCustomDataEventArgs(index, type, reader);
            var initialPosition = reader.BaseStream.Position;
            RecieveCustomData?.Invoke(null, args);
            reader.BaseStream.Position = initialPosition; // 确保事件处理器不影响流位置
            return args.Handled;
        }

        internal static bool InvokePlayerReadyToSwitch(TSPlayer plr)
        {
            var args = new PlayerReadyToSwitchEventArgs(plr);
            PlayerReadyToSwitch?.Invoke(null, args);
            return args.Handled;
        }

        internal static bool InvokePlayerFinishSwitch(TSPlayer player, PlayerSession session)
        {
            var args = new PlayerFinishSwitchEventArgs(player, session);
            PlayerFinishSwitch?.Invoke(null, args);
            return args.Handled;
        }

        #endregion
    }
}