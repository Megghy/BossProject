using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MySql.Data.MySqlClient;
using Terraria;
using Terraria.ID;
using TShockAPI;
using TShockAPI.DB;

namespace AlternativeCommandExecution.SwitchCommand
{
	internal sealed class SwitchCmdManager
	{
		private readonly IDbConnection _database;

		public List<SwitchCmd> SwitchCmds { get; } = new List<SwitchCmd>();

		public SwitchCmdManager(IDbConnection db)
		{
			_database = db;

			var table = new SqlTable("SwitchCommands",
				new SqlColumn("X", MySqlDbType.Int32) { Unique = true },
				new SqlColumn("Y", MySqlDbType.Int32) { Unique = true },
				new SqlColumn("Command", MySqlDbType.Text),
				new SqlColumn("IgnorePermission", MySqlDbType.Int32),
				new SqlColumn("AllPlayerCdSecond", MySqlDbType.Int32),
				new SqlColumn("WorldId", MySqlDbType.Int32) { Unique = true }
			);

			var creator = new SqlTableCreator(db,
							db.GetSqlType() == SqlType.Sqlite
									? (IQueryBuilder)new SqliteQueryCreator()
									: new MysqlQueryCreator());

			creator.EnsureTableStructure(table);
		}

		public void UpdateSwitchCommands()
		{
			SwitchCmds.Clear();

			using (var reader = _database.QueryReader("SELECT * FROM SwitchCommands WHERE WorldId=@0", Main.worldID))
			{
				while (reader != null && reader.Read())
				{
					SwitchCmds.Add(SwitchCmd.FromReader(reader));
				}
			}

			TShock.Log.ConsoleInfo("共载入{0}个指令开关。", SwitchCmds.Count);
		}

		public void ClearNonexistents()
		{
			var total = 0;
			var list = (from sc in SwitchCmds
						let tile = Main.tile[sc.X, sc.Y]
						where tile == null ||
											tile.type != TileID.Switches &&
											tile.type != TileID.Lever &&
											tile.type != TileID.PressurePlates
						select sc).ToList();

			foreach (var sc in list)
			{
				Del(sc.X, sc.Y);
				total++;
			}

			TShock.Log.ConsoleInfo("移除了{0}个无效开关指令数据。", total);
		}

		public void Add(int x, int y, string command)
		{
			var ex = SwitchCmds.FirstOrDefault(sc => sc.X == x && sc.Y == y);

			if (ex != null)
			{
				ex.Command = command;
				Update(ex);
			}
			else
			{
				ex = new SwitchCmd
				{
					Command = command,
					X = x,
					Y = y,
					IgnorePermission = false,
					AllPlayerCdSecond = 0
				};
				Insert(ex);
				SwitchCmds.Add(ex);
			}
		}

		public void Del(int x, int y)
		{
			SwitchCmds.RemoveAll(sc => sc.X == x && sc.Y == y);

			try
			{
				_database.Query("DELETE FROM SwitchCommands WHERE X=@0 AND Y=@1 AND WorldId=@2;",
					x, y, Main.worldID);
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
		}

		public bool SetIgnoreStatus(int x, int y, bool ignore)
		{
			var sc = SwitchCmds.FirstOrDefault(s => s.X == x && s.Y == y);
			if (sc == null)
			{
				return false;
			}

			if (sc.IgnorePermission == ignore)
				return true;

			sc.IgnorePermission = ignore;
			Update(sc);

			return true;
		}

		public bool SetAllPlyCd(int x, int y, int allPlyCd)
		{
			var sc = SwitchCmds.FirstOrDefault(s => s.X == x && s.Y == y);
			if (sc == null)
			{
				return false;
			}

			if (sc.AllPlayerCdSecond == allPlyCd)
				return true;

			sc.AllPlayerCdSecond = allPlyCd;
			Update(sc);

			return true;
		}

		private void Insert(SwitchCmd cmd)
		{
			try
			{
				_database.Query("INSERT INTO SwitchCommands (X, Y, Command, IgnorePermission, AllPlayerCdSecond, WorldId) VALUES (@0, @1, @2, @3, @4, @5);",
					cmd.X, cmd.Y, cmd.Command, cmd.IgnorePermission ? 1 : 0, cmd.AllPlayerCdSecond, Main.worldID);
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
		}

		private void Update(SwitchCmd cmd)
		{
			try
			{
				_database.Query("UPDATE SwitchCommands SET Command=@0, IgnorePermission=@1, AllPlayerCdSecond=@2 WHERE X=@3 AND Y=@4 AND WorldId=@5",
					cmd.Command, cmd.IgnorePermission ? 1 : 0, cmd.AllPlayerCdSecond, cmd.X, cmd.Y, Main.worldID);
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
		}
	}
}
