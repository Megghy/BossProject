using System;
using TShockAPI;

namespace AlternativeCommandExecution.SwitchCommand
{
	internal sealed class SwitchCmdPlayerInfo
	{
		public const string Key = "switch-cmd";

		public bool WaitingSelection = false;

		public SelectSwitch Ss;

		public delegate void SelectSwitch(int x, int y);

		public static SwitchCmdPlayerInfo GetInfo(TSPlayer player)
		{
			if (player == null)
			{
				throw new ArgumentNullException(nameof(player));
			}

			var info = player.GetData<SwitchCmdPlayerInfo>(Key);
			if (info == null)
			{
				info = new SwitchCmdPlayerInfo();
				player.SetData(Key, info);
			}
			return info;
		}
	}
}
