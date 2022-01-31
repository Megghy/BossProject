using TShockAPI;

namespace WorldEdit.Commands
{
	public class Flip : WECommand
	{
		private readonly bool flipX;
		private readonly bool flipY;

		public Flip(TSPlayer plr, bool flipX, bool flipY)
			: base(0, 0, 0, 0, plr)
		{
			this.flipX = flipX;
			this.flipY = flipY;
		}

		public override void Execute()
		{
			string clipboardPath = Tools.GetClipboardPath(plr.Account.ID);

			var data = Tools.LoadWorldData(clipboardPath);

			int endX = flipX ? -1 : data.Width;
			int endY = flipY ? -1 : data.Height;
			int incX = flipX ? -1 : 1;
			int incY = flipY ? -1 : 1;

			using (var writer = WorldSectionData.WriteHeader(clipboardPath, 0, 0, data.Width, data.Height))
			{
				for (int i = flipX ? data.Width - 1 : 0; i != endX; i += incX)
				{
					for (int j = flipY ? data.Height - 1 : 0; j != endY; j += incY)
					{
						switch (data.Tiles[i, j].slope())
						{
							case 0:
								break;
							case 1:
								if (flipX && flipY)
									data.Tiles[i, j].slope(4);
								else if (flipX)
									data.Tiles[i, j].slope(2);
								else if (flipY)
									data.Tiles[i, j].slope(3);
								break;
							case 2:
								if (flipX && flipY)
									data.Tiles[i, j].slope(3);
								else if (flipX)
									data.Tiles[i, j].slope(1);
								else if (flipY)
									data.Tiles[i, j].slope(4);
								break;
							case 3:
								if (flipX && flipY)
									data.Tiles[i, j].slope(2);
								else if (flipX)
									data.Tiles[i, j].slope(4);
								else if (flipY)
									data.Tiles[i, j].slope(1);
								break;
							case 4:
								if (flipX && flipY)
									data.Tiles[i, j].slope(1);
								else if (flipX)
									data.Tiles[i, j].slope(3);
								else if (flipY)
									data.Tiles[i, j].slope(2);
								break;

						}

						writer.Write(data.Tiles[i, j]);
					}
				}
			}

			plr.SendSuccessMessage("Flipped clipboard.");
		}
	}
}