using System.IO;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace WorldEdit.Commands
{
	public class Cut : WECommand
	{
		public Cut(int x, int y, int x2, int y2, TSPlayer plr)
			: base(x, y, x2, y2, plr)
		{
		}

		public override void Execute()
		{
			foreach (string fileName in Directory.EnumerateFiles("worldedit", string.Format("redo-{0}-{1}-*.dat", Main.worldID, plr.Account.ID)))
				File.Delete(fileName);

			if (WorldEdit.Database.GetSqlType() == SqlType.Mysql)
				WorldEdit.Database.Query("INSERT IGNORE INTO WorldEdit VALUES (@0, -1, -1)", plr.Account.ID);
			else
				WorldEdit.Database.Query("INSERT OR IGNORE INTO WorldEdit VALUES (@0, 0, 0)", plr.Account.ID);
			WorldEdit.Database.Query("UPDATE WorldEdit SET RedoLevel = -1 WHERE Account = @0", plr.Account.ID);
			WorldEdit.Database.Query("UPDATE WorldEdit SET UndoLevel = UndoLevel + 1 WHERE Account = @0", plr.Account.ID);

			int undoLevel = 0;
			using (var reader = WorldEdit.Database.QueryReader("SELECT UndoLevel FROM WorldEdit WHERE Account = @0", plr.Account.ID))
			{
				if (reader.Read())
					undoLevel = reader.Get<int>("UndoLevel");
			}
			
			string clipboard = Tools.GetClipboardPath(plr.Account.ID);

			string undoPath = Path.Combine("worldedit", string.Format("undo-{0}-{1}-{2}.dat", Main.worldID, plr.Account.ID, undoLevel));

			Tools.SaveWorldSection(x, y, x2, y2, undoPath);
            Tools.ClearObjects(x, y, x2, y2);

			for (int i = x; i <= x2; i++)
			{
				for (int j = y; j <= y2; j++)
				{ Main.tile[i, j] = new Tile(); }
			}

			if (File.Exists(clipboard)) File.Delete(clipboard);
			File.Copy(undoPath, clipboard);

			ResetSection();
			plr.SendSuccessMessage("Cut selection. ({0})", (x2 - x + 1) * (y2 - y + 1));
		}
	}
}
