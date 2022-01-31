using OTAPI.Tile;
using Terraria;
using Terraria.ID;
using TShockAPI;

namespace WorldEdit.Commands
{
    public class Text : WECommand
    {
        string text;
        public Text(int x, int y, int x2, int y2, TSPlayer plr, string text)
            : base(x, y, x2, y2, plr)
        {
            this.text = text;
        }

        public override void Execute()
        {
            if (!CanUseCommand()) { return; }
            Tools.PrepareUndo(x, y, x2, y2, plr);

            WEPoint[,] frames = Tools.CreateStatueText(text, x2 - x + 1, y2 - y + 1);
            for (int i = 0; i < frames.GetLength(0); i++)
            {
                for (int j = 0; j < frames.GetLength(1); j++)
                {
                    if (!Tools.InMapBoundaries(i + x, j + y)
                        || (frames[i, j].X == 0 && frames[i, j].Y == 0))
                    { continue; }
                    ITile tile = Main.tile[i + x, j + y];
                    tile.active(true);
                    tile.frameX = frames[i, j].X;
                    tile.frameY = frames[i, j].Y;
                    tile.liquidType(0);
                    tile.liquid = 0;
                    tile.type = TileID.AlphabetStatues;
                }
            }

            ResetSection();
            plr.SendSuccessMessage("Set text.");
        }
    }
}