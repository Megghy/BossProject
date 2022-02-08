using System.Data;
using Terraria;
using TShockAPI;
using TShockAPI.DB;
using MySqlConnector;

namespace TeleportRestriction
{
	internal class TrRegion
	{
		public int RegionId { get; set; }

		public bool AllowTpToRegion { get; set; }

		public bool AllowTpOut { get; set; }

		public bool AllowWarp { get; set; }

		public bool AllowInsideRoD { get; set; }

		public static TrRegion FromReader(QueryResult reader)
		{
			return new TrRegion
			{
				RegionId = reader.Get<int>("RegionId"),
				AllowTpToRegion = reader.Get<int>("TpToRegion") > 0,
				AllowTpOut = reader.Get<int>("TpOut") > 0,
				AllowWarp = reader.Get<int>("Warp") > 0,
				AllowInsideRoD = reader.Get<int>("InsideRoD") > 0
			};
		}
	}

	internal class TrRegionManager
	{
		private readonly IDbConnection _database;

		public List<TrRegion> TrRegions = new List<TrRegion>();

		public TrRegionManager(IDbConnection db)
		{
			_database = db;

			var table = new SqlTable("TpRestriction",
				new SqlColumn("Id", MySqlDbType.Int32) { Primary = true, AutoIncrement = true, NotNull = true },
				new SqlColumn("RegionId", MySqlDbType.Int32) { Unique = true },
				new SqlColumn("TpToRegion", MySqlDbType.Int32),
				new SqlColumn("TpOut", MySqlDbType.Int32),
				new SqlColumn("Warp", MySqlDbType.Int32),
				new SqlColumn("InsideRoD", MySqlDbType.Int32)
			);

			var creator = new SqlTableCreator(db,
											  db.GetSqlType() == SqlType.Sqlite
												  ? (IQueryBuilder)new SqliteQueryCreator()
												  : new MysqlQueryCreator());
			creator.EnsureTableStructure(table);
		}

		public void DeleteRtRegion(int iD)
        {
			try
			{
				_database.Query("DELETE FROM TpRestriction WHERE  REGIONID=@0;", iD);
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
		}

        public void LoadRegions()
		{
			try
			{
				using (
					var reader =
						_database.QueryReader(
							"SELECT `TpRestriction`.* FROM `TpRestriction`, `regions` WHERE `TpRestriction`.RegionId = `regions`.Id AND `regions`.WorldID = @0",
							Main.worldID.ToString())
					)
				{
					TrRegions.Clear();

					while (reader.Read())
					{
						TrRegions.Add(TrRegion.FromReader(reader));
					}
				}
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
		}

		public bool Add(int regionId, bool tpToRegion, bool tpOut, bool warp, bool insideRoD)
		{
			if (TrRegions.Any(t => t.RegionId == regionId))
				return false;

			var tr = new TrRegion
			{
				RegionId = regionId,
				AllowTpToRegion = tpToRegion,
				AllowTpOut = tpOut,
				AllowWarp = warp,
				AllowInsideRoD = insideRoD
			};
			TrRegions.Add(tr);

			try
			{
				_database.Query("INSERT INTO TpRestriction (RegionId, TpToRegion, TpOut, Warp, InsideRoD) VALUES (@0, @1, @2, @3, @4);",
					tr.RegionId, Convert.ToInt32(tr.AllowTpToRegion), Convert.ToInt32(tr.AllowTpOut), Convert.ToInt32(tr.AllowWarp), Convert.ToInt32(tr.AllowInsideRoD));
				return true;
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}

			return false;
		}

		public bool Remove(int regionId)
		{
			if (TrRegions.All(t => t.RegionId != regionId))
				return false;

			TrRegions.RemoveAll(t => t.RegionId == regionId);

			try
			{
				_database.Query("DELETE FROM TpRestriction WHERE RegionId = @0", regionId);
				return true;
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}

			return false;
		}

		public void Update(TrRegion region)
		{
			try
			{
				_database.Query("UPDATE TpRestriction SET TpToRegion=@0, TpOut=@1, Warp=@2, InsideRoD=@3 WHERE RegionId = @4",
					Convert.ToInt32(region.AllowTpToRegion), Convert.ToInt32(region.AllowTpOut), Convert.ToInt32(region.AllowWarp), Convert.ToInt32(region.AllowInsideRoD), region.RegionId);
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
		}

		public TrRegion CheckExist(Region region)
		{
			Add(region.ID, true, true, true, true);
			return TrRegions.Single(t => t.RegionId == region.ID);
		}

		public bool ShouldRes(TSPlayer player, TSPlayer target)
		{
			if (player.HasPermission(TeleportRestriction.BypassPermission))
				return false;

			if (TrRegions
				.Where(t => !t.AllowTpToRegion) // 不允许tp进入的区域
				.Select(t => TShock.Regions.GetRegionByID(t.RegionId))
				.Any(r => r.InArea(target.TileX, target.TileY)))
			{
				player.SendErrorMessage("目标玩家在禁止传送区域内.");
				return true;
			}

			if (TrRegions
				.Where(t => !t.AllowTpOut) // 不允许出去的区域
				.Select(t => TShock.Regions.GetRegionByID(t.RegionId))
				.Any(r => r.InArea(player.TileX, player.TileY)))
			{
				player.SendErrorMessage("你在禁止传送出的区域内.");
				return true;
			}

			return false;
		}

		public bool ShouldRes(int sourceX, int sourceY, int targetX, int targetY)
		{
			return TrRegions
					.Where(t => !t.AllowTpToRegion) // 不允许tp进入的区域
					.Select(t => TShock.Regions.GetRegionByID(t.RegionId))
					.Any(r => r?.InArea(targetX, targetY) == true) ||
				   TrRegions
					.Where(t => !t.AllowTpOut) // 不允许出去的区域
					.Select(t => TShock.Regions.GetRegionByID(t.RegionId))
					.Any(r => r?.InArea(sourceX, sourceY) == true);

		}

		public bool IsInsideRoD(TSPlayer player, int targetX, int targetY)
		{
			
			return TrRegions
					.Where(t => t.AllowInsideRoD)
					.Select(t => TShock.Regions.GetRegionByID(t.RegionId))
					.Any(r => r.InArea(targetX, targetY) && r.InArea(player.TileX, player.TileY));
		}
	}
}
