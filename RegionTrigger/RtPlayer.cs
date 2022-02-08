using TShockAPI;

namespace RegionTrigger
{
    internal class RtPlayer
    {
        public const string Rtdataname = "rtply";

        public int MsgCd = 0;

        public bool? ForcePvP = null;

        public bool CanTogglePvP = true;

        public RtRegion CurrentRegion;

        public static RtPlayer GetPlayerInfo(TSPlayer player)
        {
            var info = player.GetData<RtPlayer>(Rtdataname);
            if (info == null)
            {
                info = new RtPlayer();
                player.SetData(Rtdataname, info);
            }
            return info;
        }
    }
}
