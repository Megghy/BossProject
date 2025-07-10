using System.Collections.Concurrent;
using BossFramework.BAttributes;
using MiniWorldPlugin.Managers;
using MultiSCore.Hooks;
using MultiSCore.Model;
using MultiSCore.Services;

namespace MiniWorldPlugin.Hooks
{
    public static class MWHooks
    {
        private static readonly ConcurrentDictionary<int, bool> _permissionWarningCooldown = new();
        [AutoInit]
        public static void Initialize()
        {
            MSCHooks.PlayerFinishSwitch += OnPlayerFinishSwitch;
        }

        public static void Dispose()
        {
            MSCHooks.PlayerFinishSwitch -= OnPlayerFinishSwitch;
        }

        private static void OnPlayerFinishSwitch(object sender, MSCHooks.PlayerFinishSwitchEventArgs e)
        {
            var player = e.Player;
            var session = SessionManager.Instance.GetSession(player.Index);
            if (session == null)
                return;

            // 订阅数据包发送事件
            session.PacketSendingToServer += HandlePacketModification;

            // 订阅会话结束事件，用于清理
            session.Disposing += (s, args) =>
            {
                if (s is PlayerSession playerSession)
                {
                    playerSession.PacketSendingToServer -= HandlePacketModification;
                    WorldManager.Instance.RemovePlayerWorld(playerSession.Player);
                    _permissionWarningCooldown.TryRemove(playerSession.Player.Index, out _);
                }
            };
        }

        /// <summary>
        /// 处理玩家发送的修改世界的数据包（节流）
        /// </summary>
        private static async void HandlePacketModification(object sender, PacketEventArgs e)
        {
            if (sender is not PlayerSession session)
                return;

            var player = session.Player;

            // 检查是否是危险操作
            if (IsWorldModificationPacket(e.PacketType))
            {
                var world = WorldManager.Instance.GetPlayerWorld(player);
                if (world != null && player.Account?.ID != world.OwnerId && !world.AllowedEditors.Contains(player.Account?.ID ?? 0) && !player.HasPermission("mw.admin"))
                {
                    // 没有权限，拦截数据包
                    e.Cancel = true;

                    // 使用节流发送警告
                    if (_permissionWarningCooldown.TryAdd(player.Index, true))
                    {
                        player.SendErrorMessage("你没有权限修改这个世界。");
                        await Task.Delay(5000);
                        _permissionWarningCooldown.TryRemove(player.Index, out _);
                    }
                }
            }
        }

        private static bool IsWorldModificationPacket(PacketTypes type)
        {
            return type switch
            {
                PacketTypes.Tile => true,
                PacketTypes.TileSendSquare => true,
                PacketTypes.PaintTile => true,
                PacketTypes.PaintWall => true,
                PacketTypes.PlaceChest => true,
                PacketTypes.PlaceObject => true,
                PacketTypes.HitSwitch => true,
                PacketTypes.PlayerHurtV2 => true,
                PacketTypes.LiquidSet => true,
                PacketTypes.PlaceTileEntity => true,
                _ => false,
            };
        }
    }
}