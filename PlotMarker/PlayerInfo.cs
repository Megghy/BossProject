using System;
using Terraria;
using TShockAPI;

namespace PlotMarker
{
	internal delegate void GetPoint(int tileX, int tileY, TSPlayer receiver);
	internal class PlayerInfo
	{
		private const string Key = "pm_info_key";

		public static PlayerInfo GetInfo(TSPlayer player)
		{
			if (player == null)
				return null;

			var info = player.GetData<PlayerInfo>(Key);
			if (info == null)
			{
				info = new PlayerInfo();
				player.SetData(Key, info);
			}
			return info;
		}

		private int _x = -1;
		private int _x2 = -1;
		private int _y = -1;
		private int _y2 = -1;

		public int X
		{
			get => _x;
			set => _x = Math.Max(0, value);
		}

		public int X2
		{
			get => _x2;
			set => _x2 = Math.Min(value, Main.maxTilesX - 1);
		}

		public int Y
		{
			get => _y;
			set => _y = Math.Max(0, value);
		}

		public int Y2
		{
			get => _y2;
			set => _y2 = Math.Min(value, Main.maxTilesY - 1);
		}

		/// <summary>
		/// 玩家选取点坐标的状态.
		/// 1/2: 选两点, 3: 选区域, 4: 选点确定自己属地/更改状态, 5: 触发事件
		/// </summary>
		public PointStatus Status = PointStatus.None;

		public GetPoint OnGetPoint;

		/// <summary>
		/// Permission to build message cool down.
		/// </summary>
		public long BPm = 1;

		public enum PointStatus : byte
		{
			None = 0,
			Point1,
			Point2,
			Delegate
		}
	}
}
