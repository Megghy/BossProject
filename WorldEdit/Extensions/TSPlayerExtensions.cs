using TShockAPI;

namespace WorldEdit.Extensions
{
	public static class TSPlayerExtensions
	{
		public static PlayerInfo GetPlayerInfo(this TSPlayer tsplayer)
		{
			if (!tsplayer.ContainsData(PlayerInfo.Key))
				tsplayer.SetData(PlayerInfo.Key, new PlayerInfo());

			return tsplayer.GetData<PlayerInfo>(PlayerInfo.Key);
		}
	}
}
