using TShockAPI;

namespace Permabuffs
{
    public class PlayerInfo
    {
        public const string InfoKey = "bf-info";

        private readonly TSPlayer _player;
        public bool BypassChange { get; internal set; }

        private PlayerInfo(TSPlayer player)
        {
            _player = player;
        }

        public static PlayerInfo GetPlayerInfo(TSPlayer player)
        {
            var info = player.GetData<PlayerInfo>(InfoKey);
            if (info == null)
            {
                info = new PlayerInfo(player);
                player.SetData(InfoKey, info);
            }
            return info;
        }
    }
}
