using TShockAPI;
namespace WorldEdit.Commands
{
    public class KillEmpty : WECommand
    {
        private readonly int _action;
        public KillEmpty(int x, int y, int x2, int y2, TSPlayer plr, byte action)
            : base(x, y, x2, y2, plr)
        {
            _action = action;
        }

        public override void Execute()
        {
            if (!CanUseCommand()) { return; }
            Tools.PrepareUndo(x, y, x2, y2, plr);

            #region Signs

            if ((_action == 255) || (_action == 0))
            {
                plr.SendSuccessMessage("Cleared {0} empty signs.",
                    Tools.ClearSigns(x, y, x2, y2, true));
            }

            #endregion
            #region Chests

            if ((_action == 255) || (_action == 1))
            {
                plr.SendSuccessMessage("Cleared {0} empty chests.",
                    Tools.ClearChests(x, y, x2, y2, true));
            }

            #endregion
            ResetSection();
        }
    }
}