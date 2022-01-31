using Terraria;
using TShockAPI;
using WorldEdit.Expressions;

namespace WorldEdit.Commands
{
    public class Paste : WECommand
	{
		private readonly int alignment;
		private readonly Expression expression;
        private readonly bool mode_MainBlocks;
        private readonly string path;
        private readonly bool prepareUndo;

        public Paste(int x, int y, TSPlayer plr, string path, int alignment, Expression expression, bool mode_MainBlocks, bool prepareUndo)
			: base(x, y, int.MaxValue, int.MaxValue, plr)
		{
			this.alignment = alignment;
			this.expression = expression;
            this.mode_MainBlocks = mode_MainBlocks;
            this.path = path;
            this.prepareUndo = prepareUndo;
        }

		public override void Execute()
		{
            WorldSectionData data = Tools.LoadWorldData(path);

			var width = data.Width - 1;
			var height = data.Height - 1;

			if ((alignment & 1) == 0)
				x2 = x + width;
			else
			{
				x2 = x;
				x -= width;
			}
			if ((alignment & 2) == 0)
				y2 = y + height;
			else
			{
				y2 = y;
				y -= height;
			}

            if (x < 0) { x = 0; }
            if (x2 < 0) { x2 = 0; }
            if (y < 0) { y = 0; }
            if (y2 < 0) { y2 = 0; }
            if (x >= Main.maxTilesX) { x = Main.maxTilesX - 1; }
            if (x2 >= Main.maxTilesX) { x2 = Main.maxTilesX - 1; }
            if (y >= Main.maxTilesY) { y = Main.maxTilesY - 1; }
            if (y2 >= Main.maxTilesY) { y2 = Main.maxTilesY - 1; }

            if (!CanUseCommand()) { return; }
            if (prepareUndo) { Tools.PrepareUndo(x, y, x2, y2, plr); }

			for (var i = x; i <= x2; i++)
			{
				for (var j = y; j <= y2; j++)
                {
                    var index1 = i - x;
                    var index2 = j - y;

                    if (i < 0 || j < 0 || i >= Main.maxTilesX || j >= Main.maxTilesY ||
						expression != null && !expression.Evaluate(mode_MainBlocks
                                                ? Main.tile[i, j]
                                                : data.Tiles[index1, index2]))
					{
						continue;
					}

					Main.tile[i, j] = data.Tiles[index1, index2];
				}
			}

            Tools.LoadWorldSection(data, x, y, false);
            ResetSection();
            plr.SendSuccessMessage("Pasted clipboard to selection.");
		}
	}
}