using TShockAPI;
using TShockAPI.DB;

namespace AlternativeCommandExecution.SwitchCommand
{
	internal sealed class SwitchCmd
	{
		public string Command;

		public int X;

		public int Y;

		public bool IgnorePermission;

		public int AllPlayerCdSecond;

		private int _currentCd;

		public void Tick()
		{
			if (AllPlayerCdSecond <= 0)
				return;

			if (_currentCd > 0)
				_currentCd--;
		}

		public bool TryUse(TSPlayer player)
		{
			if (AllPlayerCdSecond <= 0)
				return true;

			if (_currentCd > 0 && !player.HasPermission("ace.sc.ignorecd"))
				return false;

			_currentCd = AllPlayerCdSecond;
			return true;
		}

		public static SwitchCmd FromReader(QueryResult reader)
		{
			return new SwitchCmd
			{
				Command = reader.Get<string>("Command"),
				X = reader.Get<int>("X"),
				Y = reader.Get<int>("Y"),
				IgnorePermission = reader.Get<int>("IgnorePermission") > 0,
				AllPlayerCdSecond = reader.Get<int>("AllPlayerCdSecond")
			};
		}
	}
}
