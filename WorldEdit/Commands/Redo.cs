using TShockAPI;

namespace WorldEdit.Commands
{
	public class Redo : WECommand
	{
		private int accountID;
		private int steps;

		public Redo(TSPlayer plr, int accountID, int steps)
			: base(0, 0, 0, 0, plr)
		{
			this.accountID = accountID;
			this.steps = steps;
		}

		public override void Execute()
        {
            if (WorldEdit.Config.DisableUndoSystemForUnrealPlayers
                && (!plr.RealPlayer || (accountID == 0)))
            {
                plr.SendErrorMessage("Undo system is disabled for unreal players.");
                return;
            }

            int i = -1;
			while (++i < steps && Tools.Redo(accountID)) ;
			if (i == 0)
				plr.SendErrorMessage("Failed to redo any actions.");
			else
				plr.SendSuccessMessage("Redid {0}'s last {1}action{2}.", ((accountID == 0) ? "ServerConsole" : TShock.UserAccounts.GetUserAccountByID(accountID).Name), i == 1 ? "" : i + " ", i == 1 ? "" : "s");
		}
	}
}
