#region Using
using System.IO.Compression;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Tile_Entities;
using Terraria.ID;
#endregion
namespace FakeProvider
{
    class SendSectionPacket
    {
        #region Send

        public static void Send(int Who, int IgnoreIndex,
                int X, int Y, int Width, int Height) =>
            Send(((Who == -1) ? FakeProviderPlugin.AllPlayers : new int[] { Who }),
                IgnoreIndex, X, Y, Width, Height);

        public static void Send(IEnumerable<int> Who, int IgnoreIndex,
            int X, int Y, int Width, int Height)
        {
            if (Who == null)
                return;

            List<RemoteClient> clients = new List<RemoteClient>();
            foreach (int i in Who)
            {
                if (i == IgnoreIndex)
                    continue;
                if ((i < 0) || (i >= Main.maxPlayers))
                    throw new ArgumentOutOfRangeException(nameof(Who));
                RemoteClient client = Netplay.Clients[i];
                // Not checking NetMessage.buffer[i].broadcast since section packet (because of section packet on connection)
                if (client?.IsConnected() == true)
                    clients.Add(client);
            }

            foreach (var group in FakeProviderAPI.GroupByPersonal(clients, X, Y, Width, Height))
            {
                FakeProviderPlugin.SendTo(group, Generate(group.Key, X, Y, Width, Height));
            }
        }

        #endregion
        #region Generate

        private static byte[] Generate(IEnumerable<TileProvider> providers, int X, int Y, int Width, int Height)
        {

            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.BaseStream.Position = 2L;
                bw.Write((byte)PacketTypes.TileSendSection);
                CompressTileBlock(providers, bw, X, Y, (short)Width, (short)Height);
                long position = bw.BaseStream.Position;
                bw.BaseStream.Position = 0L;
                bw.Write((short)position);
                bw.BaseStream.Position = position;
                data = ms.ToArray();
            }
            return data;
        }

        #endregion
        #region CompressTileBlock

        private static int CompressTileBlock(IEnumerable<TileProvider> providers,
            BinaryWriter writer, int xStart, int yStart, short width, short height)
        {
            if (xStart < 0)
            {
                width += (short)xStart;
                xStart = 0;
            }
            if (yStart < 0)
            {
                height += (short)yStart;
                yStart = 0;
            }
            if ((xStart + width) > Main.maxTilesX)
                width = (short)(Main.maxTilesX - xStart);
            if ((yStart + height) > Main.maxTilesY)
                height = (short)(Main.maxTilesY - yStart);
            if ((width == 0) || (height == 0))
                return 0; // WHAT???????????????????????

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
                {
                    binaryWriter.Write(xStart);
                    binaryWriter.Write(yStart);
                    binaryWriter.Write(width);
                    binaryWriter.Write(height);
                    //NetMessage.CompressTileBlock_Inner(binaryWriter, xStart, yStart, (int)width, (int)height);
                    CompressTileBlock_Inner(providers, binaryWriter, xStart, yStart, width, height);
                    memoryStream.Position = 0L;
                    MemoryStream memoryStream2 = new MemoryStream();
                    using (DeflateStream deflateStream = new DeflateStream(memoryStream2, CompressionMode.Compress, true))
                    {
                        memoryStream.CopyTo(deflateStream);
                        deflateStream.Flush();
                        deflateStream.Close();
                        deflateStream.Dispose();
                    }
                    bool flag = memoryStream.Length <= memoryStream2.Length;
                    if (flag)
                    {
                        memoryStream.Position = 0L;
                        writer.Write((byte)0);
                        writer.Write(memoryStream.GetBuffer());
                    }
                    else
                    {
                        memoryStream2.Position = 0L;
                        writer.Write((byte)1);
                        writer.Write(memoryStream2.GetBuffer());
                    }
                }
            }
            return 0;
        }

        #endregion
        #region CompressTileBlock_Inner


        public static short[] _compressChestList = new short[8000];

        public static short[] _compressSignList = new short[1000];

        public static short[] _compressEntities = new short[1000];
        private static void CompressTileBlock_Inner(IEnumerable<TileProvider> providers,
            BinaryWriter writer, int xStart, int yStart, int width, int height)
        {
            // Counter indices
            short chestCount = 0;
            short signCount = 0;
            short entityCount = 0;

            // Tracking variables
            int bufferPos = 0;
            int headerPos = 0;
            int repeatedTileCount = 0;
            byte flags = 0;
            byte[] buffer = new byte[16];
            ITile previousTile = null;

            // Process all tiles in the specified region
            for (int y = yStart; y < yStart + height; y++)
            {
                for (int x = xStart; x < xStart + width; x++)
                {
                    ITile currentTile = Terraria.Main.tile[x, y];

                    // Check if this tile can be batched with the previous one
                    if (currentTile.isTheSameAs(previousTile) && TileID.Sets.AllowsSaveCompressionBatching[currentTile.type])
                    {
                        repeatedTileCount++;
                        continue;
                    }

                    // Write out any accumulated repeated tiles
                    if (previousTile != null)
                    {
                        if (repeatedTileCount > 0)
                        {
                            // Write repeat count (using one or two bytes depending on size)
                            buffer[bufferPos] = (byte)(repeatedTileCount & 0xFF);
                            bufferPos++;

                            if (repeatedTileCount > 255)
                            {
                                flags |= 0x80; // Two-byte repeat flag
                                buffer[bufferPos] = (byte)((repeatedTileCount & 0xFF00) >> 8);
                                bufferPos++;
                            }
                            else
                            {
                                flags |= 0x40; // One-byte repeat flag
                            }
                        }

                        // Write the header byte with flags
                        buffer[headerPos] = flags;
                        writer.Write(buffer, headerPos, bufferPos - headerPos);
                        repeatedTileCount = 0;
                    }

                    // Reset for new tile
                    bufferPos = 4;
                    flags = 0;
                    byte importantFlag = 0;
                    byte headerFlag2 = 0;
                    byte headerFlag3 = 0;

                    // Process active tile properties
                    if (currentTile.active())
                    {
                        flags |= 2; // Active tile flag

                        // Write tile type
                        buffer[bufferPos++] = (byte)currentTile.type;

                        // Handle tile types that need extended format
                        if (currentTile.type > 255)
                        {
                            buffer[bufferPos++] = (byte)(currentTile.type >> 8);
                            flags |= 0x20; // Extended tile type flag
                        }

                        // Process special tiles
                        ProcessSpecialTiles(currentTile, x, y, ref chestCount, ref signCount, ref entityCount);

                        // Handle important frame data
                        if (Terraria.Main.tileFrameImportant[currentTile.type])
                        {
                            WriteFrameData(currentTile, buffer, ref bufferPos);
                        }

                        // Handle tile color
                        if (currentTile.color() != 0)
                        {
                            headerFlag2 |= 8;
                            buffer[bufferPos++] = currentTile.color();
                        }
                    }

                    // Process wall properties
                    if (currentTile.wall != 0)
                    {
                        flags |= 4; // Wall flag
                        buffer[bufferPos++] = (byte)currentTile.wall;

                        // Handle wall color
                        if (currentTile.wallColor() != 0)
                        {
                            headerFlag2 |= 0x10;
                            buffer[bufferPos++] = currentTile.wallColor();
                        }

                        // Extended wall ID
                        if (currentTile.wall > 255)
                        {
                            buffer[bufferPos++] = (byte)(currentTile.wall >> 8);
                            headerFlag2 |= 0x40;
                        }
                    }

                    // Process liquid properties
                    if (currentTile.liquid != 0)
                    {
                        if (!currentTile.shimmer())
                        {
                            flags |= (byte)(currentTile.lava() ? 0x10 :
                                    (currentTile.honey() ? 0x18 : 0x08));
                        }
                        else
                        {
                            headerFlag2 |= 0x80;
                            flags |= 0x08;
                        }
                        buffer[bufferPos++] = currentTile.liquid;
                    }

                    // Process wire properties
                    if (currentTile.wire()) importantFlag |= 2;
                    if (currentTile.wire2()) importantFlag |= 4;
                    if (currentTile.wire3()) importantFlag |= 8;
                    if (currentTile.wire4()) headerFlag2 |= 0x20;

                    // Process brick/slope properties
                    byte shapeValue = GetShapeValue(currentTile);
                    importantFlag |= shapeValue;

                    // Process additional tile flags
                    if (currentTile.actuator()) headerFlag2 |= 2;
                    if (currentTile.inActive()) headerFlag2 |= 4;

                    // Process invisibility and lighting flags
                    if (currentTile.invisibleBlock()) headerFlag3 |= 2;
                    if (currentTile.invisibleWall()) headerFlag3 |= 4;
                    if (currentTile.fullbrightBlock()) headerFlag3 |= 8;
                    if (currentTile.fullbrightWall()) headerFlag3 |= 0x10;

                    // Set up header positions in reverse order (header3, header2, header1)
                    headerPos = 3;

                    // Only include headers that are needed
                    if (headerFlag3 != 0)
                    {
                        headerFlag2 |= 1;
                        buffer[headerPos--] = headerFlag3;
                    }

                    if (headerFlag2 != 0)
                    {
                        importantFlag |= 1;
                        buffer[headerPos--] = headerFlag2;
                    }

                    if (importantFlag != 0)
                    {
                        flags |= 1;
                        buffer[headerPos--] = importantFlag;
                    }

                    previousTile = currentTile;
                }
            }

            // Handle any remaining repeated tiles
            if (repeatedTileCount > 0)
            {
                buffer[bufferPos++] = (byte)(repeatedTileCount & 0xFF);

                if (repeatedTileCount > 255)
                {
                    flags |= 0x80;
                    buffer[bufferPos++] = (byte)((repeatedTileCount & 0xFF00) >> 8);
                }
                else
                {
                    flags |= 0x40;
                }
            }

            // Write final buffer
            buffer[headerPos] = flags;
            writer.Write(buffer, headerPos, bufferPos - headerPos);

            // Write special tile entities
            WriteSpecialEntities(writer, chestCount, signCount, entityCount);
        }

        // Helper method to process special tiles (chests, signs, etc.)
        private static void ProcessSpecialTiles(ITile tile, int x, int y, ref short chestCount, ref short signCount, ref short entityCount)
        {
            // Basic chest detection
            if (TileID.Sets.BasicChest[tile.type] && tile.frameX % 36 == 0 && tile.frameY % 36 == 0)
            {
                short chestIndex = (short)Chest.FindChest(x, y);
                if (chestIndex != -1)
                {
                    _compressChestList[chestCount++] = chestIndex;
                }
            }

            // Special chest type (ID 88)
            if (tile.type == 88 && tile.frameX % 54 == 0 && tile.frameY % 36 == 0)
            {
                short chestIndex = (short)Chest.FindChest(x, y);
                if (chestIndex != -1)
                {
                    _compressChestList[chestCount++] = chestIndex;
                }
            }

            // Sign types (85, 55, 425, 573)
            int[] signTypes = { 85, 55, 425, 573 };
            if (Array.IndexOf(signTypes, tile.type) >= 0 && tile.frameX % 36 == 0 && tile.frameY % 36 == 0)
            {
                short signIndex = (short)Sign.ReadSign(x, y);
                if (signIndex != -1)
                {
                    _compressSignList[signCount++] = signIndex;
                }
            }

            // Process special entity tiles
            ProcessEntityTile(tile, x, y, ref entityCount);
        }

        // Helper method to process entity tiles
        private static void ProcessEntityTile(ITile tile, int x, int y, ref short entityCount)
        {
            int entityIndex = -1;

            // Training dummy
            if (tile.type == 378 && tile.frameX % 36 == 0 && tile.frameY == 0)
            {
                entityIndex = TETrainingDummy.Find(x, y);
            }
            // Item frame
            else if (tile.type == 395 && tile.frameX % 36 == 0 && tile.frameY == 0)
            {
                entityIndex = TEItemFrame.Find(x, y);
            }
            // Food platter
            else if (tile.type == 520 && tile.frameX % 18 == 0 && tile.frameY == 0)
            {
                entityIndex = TEFoodPlatter.Find(x, y);
            }
            // Weapons rack
            else if (tile.type == 471 && tile.frameX % 54 == 0 && tile.frameY == 0)
            {
                entityIndex = TEWeaponsRack.Find(x, y);
            }
            // Display doll
            else if (tile.type == 470 && tile.frameX % 36 == 0 && tile.frameY == 0)
            {
                entityIndex = TEDisplayDoll.Find(x, y);
            }
            // Hat rack
            else if (tile.type == 475 && tile.frameX % 54 == 0 && tile.frameY == 0)
            {
                entityIndex = TEHatRack.Find(x, y);
            }
            // Teleportation pylon
            else if (tile.type == 597 && tile.frameX % 54 == 0 && tile.frameY % 72 == 0)
            {
                entityIndex = TETeleportationPylon.Find(x, y);
            }

            if (entityIndex != -1)
            {
                _compressEntities[entityCount++] = (short)entityIndex;
            }
        }

        // Helper method to write frame data
        private static void WriteFrameData(ITile tile, byte[] buffer, ref int position)
        {
            // Write frameX
            buffer[position++] = (byte)(tile.frameX & 0xFF);
            buffer[position++] = (byte)((tile.frameX & 0xFF00) >> 8);

            // Write frameY
            buffer[position++] = (byte)(tile.frameY & 0xFF);
            buffer[position++] = (byte)((tile.frameY & 0xFF00) >> 8);
        }

        // Helper method to get shape value
        private static byte GetShapeValue(ITile tile)
        {
            if (tile.halfBrick())
            {
                return 16;
            }
            else if (tile.slope() != 0)
            {
                return (byte)((tile.slope() + 1) << 4);
            }

            return 0;
        }

        // Helper method to write special entities (chests, signs, etc.)
        private static void WriteSpecialEntities(BinaryWriter writer, short chestCount, short signCount, short entityCount)
        {
            // Write chests
            writer.Write(chestCount);
            for (int i = 0; i < chestCount; i++)
            {
                Chest chest = Terraria.Main.chest[_compressChestList[i]];
                writer.Write(_compressChestList[i]);
                writer.Write((short)chest.x);
                writer.Write((short)chest.y);
                writer.Write(chest.name);
            }

            // Write signs
            writer.Write(signCount);
            for (int i = 0; i < signCount; i++)
            {
                Sign sign = Terraria.Main.sign[_compressSignList[i]];
                writer.Write(_compressSignList[i]);
                writer.Write((short)sign.x);
                writer.Write((short)sign.y);
                writer.Write(sign.text);
            }

            // Write tile entities
            writer.Write(entityCount);
            for (int i = 0; i < entityCount; i++)
            {
                TileEntity.Write(writer, TileEntity.ByID[_compressEntities[i]]);
            }
        }

        #endregion
    }
}