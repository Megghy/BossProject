using System.IO;
using TShockAPI;

namespace WorldEdit.Commands
{
	public class Rotate : WECommand
	{
		private readonly int _degrees;

		public Rotate(TSPlayer plr, int degrees)
			: base(0, 0, 0, 0, plr)
		{
			_degrees = degrees;
		}

		public override void Execute()
		{
			var clipboardPath = Tools.GetClipboardPath(plr.Account.ID);

			var data = Tools.LoadWorldData(clipboardPath);

			BinaryWriter writer = null;

			switch ((_degrees / 90 % 4 + 4) % 4)
			{
				case 0:
					writer = WorldSectionData.WriteHeader(clipboardPath, 0, 0, data.Width, data.Height);
					for (var i = 0; i < data.Width; i++)
					{
						for (var j = 0; j < data.Height; j++)
						{
							writer.Write(data.Tiles[i, j]);
						}
					}
					break;
				case 1:
					writer = WorldSectionData.WriteHeader(clipboardPath, 0, 0, data.Height, data.Width);
					for (var j = data.Height - 1; j >= 0; j--)
					{
						for (var i = 0; i < data.Width; i++)
						{
							switch (data.Tiles[i, j].slope())
							{
								case 0:
									break;
								case 1:
									data.Tiles[i, j].slope(3);
									break;
								case 2:
									data.Tiles[i, j].slope(1);
									break;
								case 3:
									data.Tiles[i, j].slope(4);
									break;
								case 4:
									data.Tiles[i, j].slope(2);
									break;
							}

							writer.Write(data.Tiles[i, j]);
						}
					}
					break;
				case 2:
					writer = WorldSectionData.WriteHeader(clipboardPath, 0, 0, data.Width, data.Height);
					for (int i = data.Width - 1; i >= 0; i--)
					{
						for (int j = data.Height - 1; j >= 0; j--)
						{
							switch (data.Tiles[i, j].slope())
							{
								case 0:
									break;
								case 1:
									data.Tiles[i, j].slope(4);
									break;
								case 2:
									data.Tiles[i, j].slope(3);
									break;
								case 3:
									data.Tiles[i, j].slope(2);
									break;
								case 4:
									data.Tiles[i, j].slope(1);
									break;
							}

							writer.Write(data.Tiles[i, j]);
						}
					}
					break;
				case 3:
					writer = WorldSectionData.WriteHeader(clipboardPath, 0, 0, data.Height, data.Width);
					for (int j = 0; j < data.Height; j++)
					{
						for (int i = data.Width - 1; i >= 0; i--)
						{
							switch (data.Tiles[i, j].slope())
							{
								case 0:
									break;
								case 1:
									data.Tiles[i, j].slope(2);
									break;
								case 2:
									data.Tiles[i, j].slope(4);
									break;
								case 3:
									data.Tiles[i, j].slope(1);
									break;
								case 4:
									data.Tiles[i, j].slope(3);
									break;
							}

							writer.Write(data.Tiles[i, j]);
						}
					}
					break;
			}

			writer?.Close();

			plr.SendSuccessMessage("Rotated clipboard {0} degrees.", _degrees);
		}
	}
}