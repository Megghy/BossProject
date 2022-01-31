using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using OTAPI.Tile;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Tile_Entities;
using Terraria.ID;
using TShockAPI;

namespace WorldEdit
{
	public sealed class WorldSectionData
	{
		public IList<SignData> Signs;

		public IList<ChestData> Chests;

		public IList<DisplayItemData> ItemFrames;

        public IList<LogicSensorData> LogicSensors;

        public IList<PositionData> TrainingDummies;

		public IList<DisplayItemData> WeaponsRacks;

		public IList<PositionData> TeleportationPylons;

		public IList<DisplayItemsData> DisplayDolls;

		public IList<DisplayItemsData> HatRacks;

		public IList<DisplayItemData> FoodPlatters;

		public ITile[,] Tiles;

		public int Width;

		public int Height;

		public int X;

		public int Y;

		public WorldSectionData(int width, int height)
		{
			Width = width;
			Height = height;

			Signs = new List<SignData>();
			Chests = new List<ChestData>();
			ItemFrames = new List<DisplayItemData>();
            LogicSensors = new List<LogicSensorData>();
			TrainingDummies = new List<PositionData>();
			WeaponsRacks = new List<DisplayItemData>();
			TeleportationPylons = new List<PositionData>();
			DisplayDolls = new List<DisplayItemsData>();
			HatRacks = new List<DisplayItemsData>();
			FoodPlatters = new List<DisplayItemData>();
			Tiles = new ITile[width, height];
		}

		public void ProcessTile(ITile tile, int x, int y)
		{
			Tiles[x, y] = new Tile();
			if (tile != null)
				Tiles[x, y].CopyFrom(tile);

			if (!tile.active())
			{
				return;
			}

			var actualX = x + X;
			var actualY = y + Y;

			switch (tile.type)
			{
				case TileID.Signs:
				case TileID.AnnouncementBox:
				case TileID.Tombstones:
					if (tile.frameX % 36 == 0 && tile.frameY == 0)
					{
						var id = Sign.ReadSign(actualX, actualY, false);
						if (id != -1)
						{
							Signs.Add(new SignData()
							{
								Text = Main.sign[id].text,
								X = x,
								Y = y
							});
						}
					}
					break;
				case TileID.ItemFrame:
					if (tile.frameX % 36 == 0 && tile.frameY == 0)
					{
						var id = TEItemFrame.Find(actualX, actualY);
						if (id != -1)
						{
							var frame = (TEItemFrame)TileEntity.ByID[id];
							ItemFrames.Add(new DisplayItemData()
							{
								Item = new NetItem(frame.item.netID, frame.item.stack, frame.item.prefix),
								X = x,
								Y = y
							});
						}
					}
					break;
				case TileID.Containers:
				case TileID.Dressers:
					if (tile.frameX % 36 == 0 && tile.frameY == 0)
					{
						var id = Chest.FindChest(actualX, actualY);
						if (id != -1)
						{
							var chest = Main.chest[id];
							if (chest.item != null)
							{
								var items = chest.item.Select(item => new NetItem(item.netID, item.stack, item.prefix)).ToArray();
								Chests.Add(new ChestData()
								{
									Items = items,
									X = x,
									Y = y
								});
							}
						}
					}
                    break;
                case TileID.LogicSensor:
                    {
                        var id = TELogicSensor.Find(actualX, actualY);
                        if (id != -1)
                        {
                            var sensor = (TELogicSensor)TileEntity.ByID[id];
                            LogicSensors.Add(new LogicSensorData()
							{
                                Type = sensor.logicCheck,
                                X = x,
                                Y = y
                            });
                        }
                        break;
                    }
                case TileID.TargetDummy:
                    if (tile.frameX % 36 == 0 && tile.frameY == 0)
                    {
                        var id = TETrainingDummy.Find(actualX, actualY);
                        if (id != -1)
                        {
                            TrainingDummies.Add(new PositionData()
							{
                                X = x,
                                Y = y
                            });
                        }
                    }
                    break;
				case TileID.WeaponsRack2:
					if (tile.frameY == 0 && tile.frameX % 54 == 0)
					{
						var id = TEWeaponsRack.Find(actualX, actualY);
						if (id != -1)
						{
							var rack = (TEWeaponsRack)TileEntity.ByID[id];
							WeaponsRacks.Add(new DisplayItemData()
							{
								Item = new NetItem(rack.item.netID, rack.item.stack, rack.item.prefix),
								X = x,
								Y = y,
							});
						}
					}
					break;
				case TileID.TeleportationPylon:
					if (tile.frameY == 0 && tile.frameX % 54 == 0)
					{
						var id = TETeleportationPylon.Find(actualX, actualY);
						if (id != -1)
						{
							TeleportationPylons.Add(new PositionData()
							{
								X = x,
								Y = y
							});
						}
					}
				break;
				case TileID.DisplayDoll:
					if (tile.frameY == 0 && tile.frameX % 36 == 0)
					{
						var id = TEDisplayDoll.Find(actualX, actualY);
						if (id != -1)
						{
							var doll = (TEDisplayDoll)TileEntity.ByID[id];
							DisplayDolls.Add(new DisplayItemsData()
							{
								Items = doll._items.Select(i => new NetItem(i.netID, i.stack, i.prefix)).ToArray(),
								Dyes = doll._dyes.Select(i => new NetItem(i.netID, i.stack, i.prefix)).ToArray(),
								X = x,
								Y = y
							});
						}
					}
					break;
				case TileID.HatRack:
					if (tile.frameY == 0 && tile.frameX % 54 == 0)
					{
						var id = TEHatRack.Find(actualX, actualY);
						if (id != -1)
						{
							var rack = (TEHatRack)TileEntity.ByID[id];
							HatRacks.Add(new DisplayItemsData()
							{
								Items = rack._items.Select(i => new NetItem(i.netID, i.stack, i.prefix)).ToArray(),
								Dyes = rack._dyes.Select(i => new NetItem(i.netID, i.stack, i.prefix)).ToArray(),
								X = x,
								Y = y
							});
						}
					}
					break;
				case TileID.FoodPlatter:
					{
						var id = TEFoodPlatter.Find(actualX, actualY);
						if (id != -1)
						{
							var platter = (TEFoodPlatter)TileEntity.ByID[id];
							FoodPlatters.Add(new DisplayItemData()
							{
								Item = new NetItem(platter.item.netID, platter.item.stack, platter.item.prefix),
								X = x,
								Y = y
							});
						}
					}
					break;
			}
		}

		private void WriteInner(BinaryWriter writer)
		{
			for (int i = 0; i < Width; i++)
				for (int j = 0; j < Height; j++)
					writer.Write(Tiles[i, j]);

			writer.Write(Signs.Count);
			foreach (var sign in Signs)
				sign.Write(writer);

			writer.Write(Chests.Count);
			foreach (var chest in Chests)
				chest.Write(writer);

			writer.Write(ItemFrames.Count);
			foreach (var itemFrame in ItemFrames)
				itemFrame.Write(writer);

            writer.Write(LogicSensors.Count);
            foreach (var logicSensor in LogicSensors)
				logicSensor.Write(writer);

            writer.Write(TrainingDummies.Count);
			foreach (var targetDummy in TrainingDummies)
				targetDummy.Write(writer);

			writer.Write(WeaponsRacks.Count);
			foreach (var weaponRack in WeaponsRacks)
				weaponRack.Write(writer);

			writer.Write(TeleportationPylons.Count);
			foreach (var teleportationPylon in TeleportationPylons)
				teleportationPylon.Write(writer);

			writer.Write(DisplayDolls.Count);
			foreach (var displayDoll in DisplayDolls)
				displayDoll.Write(writer);

			writer.Write(HatRacks.Count);
			foreach (var hatRack in HatRacks)
				hatRack.Write(writer);

			writer.Write(FoodPlatters.Count);
			foreach (var foodPlatter in FoodPlatters)
				foodPlatter.Write(writer);
		}

		public void Write(Stream stream)
		{
			using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
				WriteHeader(writer);
			using (var writer = new BinaryWriter(new BufferedStream(new GZipStream(stream,
					CompressionMode.Compress), Tools.BUFFER_SIZE)))
				WriteInner(writer);
		}

		public void Write(string filePath)
		{
			using (var writer = WriteHeader(filePath, X, Y, Width, Height))
				WriteInner(writer);
		}

		public void WriteHeader(BinaryWriter writer)
		{
			writer.Write(X);
			writer.Write(Y);
			writer.Write(Width);
			writer.Write(Height);
		}

		public static BinaryWriter WriteHeader(string filePath, int x, int y, int width, int height)
		{
			Stream stream = File.Open(filePath, FileMode.Create);
			using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
			{
				writer.Write(x);
				writer.Write(y);
				writer.Write(width);
				writer.Write(height);
			}
			return new BinaryWriter(
					new BufferedStream(
						new GZipStream(stream, CompressionMode.Compress), Tools.BUFFER_SIZE));
		}

		public struct DisplayItemData
		{
			public int X;

			public int Y;

			public NetItem Item;

			public void Write(BinaryWriter writer)
			{
				writer.Write(X);
				writer.Write(Y);
				writer.Write(Item);
			}

			public static DisplayItemData Read(BinaryReader reader) =>
				new DisplayItemData()
				{
					X = reader.ReadInt32(),
					Y = reader.ReadInt32(),
					Item = reader.ReadNetItem()
				};
		}

		public struct DisplayItemsData
		{
			public int X;

			public int Y;

			public NetItem[] Items;

			public NetItem[] Dyes;

			public void Write(BinaryWriter writer)
			{
				writer.Write(X);
				writer.Write(Y);
				writer.Write(Items);
				writer.Write(Dyes);
			}

			public static DisplayItemsData Read(BinaryReader reader) =>
				new DisplayItemsData()
				{
					X = reader.ReadInt32(),
					Y = reader.ReadInt32(),
					Items = reader.ReadNetItems(),
					Dyes = reader.ReadNetItems()
				};
		}

		public struct PositionData
		{
			public int X;

			public int Y;

			public void Write(BinaryWriter writer)
			{
				writer.Write(X);
				writer.Write(Y);
			}

			public static PositionData Read(BinaryReader reader) =>
				new PositionData()
				{
					X = reader.ReadInt32(),
					Y = reader.ReadInt32()
				};
		}

        public struct LogicSensorData
        {
            public int X;

            public int Y;

            public TELogicSensor.LogicCheckType Type;

			public void Write(BinaryWriter writer)
			{
				writer.Write(X);
				writer.Write(Y);
				writer.Write((int)Type);
			}

			public static LogicSensorData Read(BinaryReader reader) =>
				new LogicSensorData()
				{
					X = reader.ReadInt32(),
					Y = reader.ReadInt32(),
					Type = (TELogicSensor.LogicCheckType)reader.ReadInt32()
				};
        }

		public struct ChestData
		{
			public int X;

			public int Y;

			public NetItem[] Items;

			public void Write(BinaryWriter writer)
			{
				writer.Write(X);
				writer.Write(Y);
				writer.Write(Items);
			}

			public static ChestData Read(BinaryReader reader) =>
				new ChestData()
				{
					X = reader.ReadInt32(),
					Y = reader.ReadInt32(),
					Items = reader.ReadNetItems()
				};
		}

		public struct SignData
		{
			public int X;

			public int Y;

			public string Text;

			public void Write(BinaryWriter writer)
			{
				writer.Write(X);
				writer.Write(Y);
				writer.Write(Text);
			}

			public static SignData Read(BinaryReader reader) =>
				new SignData()
				{
					X = reader.ReadInt32(),
					Y = reader.ReadInt32(),
					Text = reader.ReadString()
				};
		}
	}
}
