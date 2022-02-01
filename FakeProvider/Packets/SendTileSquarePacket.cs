#region Using
using Terraria;
#endregion
namespace FakeProvider
{
    class SendTileSquarePacket
    {
        #region Send

        public static void Send(int Who, int IgnoreIndex,
                int Width, int Height, int X, int Y, int TileChangeType = 0) =>
            Send(((Who == -1) ? FakeProviderPlugin.AllPlayers : new int[] { Who }),
                IgnoreIndex, Width, Height, X, Y, TileChangeType);

        public static void Send(IEnumerable<int> Who, int IgnoreIndex,
            int Width, int Height, int X, int Y, int TileChangeType = 0)
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
                if (NetMessage.buffer[i].broadcast && client?.IsConnected() == true && client.SectionRange(Math.Max(Width, Height), X, Y))
                    clients.Add(client);
            }

            foreach (var group in FakeProviderAPI.GroupByPersonal(clients, X, Y, Width, Height))
                FakeProviderPlugin.SendTo(group, Generate(group.Key, X, Y, Width, Height, TileChangeType));
        }

        #endregion
        #region Generate

        private static byte[] Generate(IEnumerable<TileProvider> providers,
            int X, int Y, int Width, int Height, int TileChangeType)
        {

            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.BaseStream.Position = 2L;
                bw.Write((byte)PacketTypes.TileSendSquare);
                WriteTiles(providers, bw, X, Y, Width, Height, TileChangeType);
                long position = bw.BaseStream.Position;
                bw.BaseStream.Position = 0L;
                bw.Write((short)position);
                bw.BaseStream.Position = position;
                data = ms.ToArray();
            }
            return data;
        }

        #endregion
        #region WriteTiles

        /// <param name="number">Size</param>
        /// <param name="number2">X</param>
        /// <param name="number3">Y</param>
        /// <param name="number5">TileChangeType</param>
        private static void WriteTiles(IEnumerable<TileProvider> providers,
            BinaryWriter binaryWriter, int number, int number2, int number3, int number4, int number5 = 0)
        {
            int sx = number;
            int sy = (int)number2;
            int width = (int)number3;
            if (width < 0)
            {
                width = 0;
            }
            int height = (int)number4;
            if (height < 0)
            {
                height = 0;
            }
            if (sx < width)
            {
                sx = width;
            }
            if (sx >= Main.maxTilesX + width)
            {
                sx = Main.maxTilesX - width - 1;
            }
            if (sy < height)
            {
                sy = height;
            }
            if (sy >= Main.maxTilesY + height)
            {
                sy = Main.maxTilesY - height - 1;
            }
            binaryWriter.Write((short)sx);
            binaryWriter.Write((short)sy);
            binaryWriter.Write((byte)width);
            binaryWriter.Write((byte)height);
            binaryWriter.Write((byte)number5);
            (ModFramework.ICollection<ITile> tiles, bool relative) = FakeProviderAPI.ApplyPersonal(providers, sx, sy, width, height);
            int _sx = (relative ? 0 : sx);
            int _sy = (relative ? 0 : sy);
            for (int x = _sx; x < _sx + width; x++)
            {
                for (int y = _sy; y < _sy + height; y++)
                {
                    BitsByte bb17 = 0;
                    BitsByte bb18 = 0;
                    byte b = 0;
                    byte b2 = 0;
                    ITile tile = tiles[x, y];
                    bb17[0] = tile.active();
                    bb17[2] = (tile.wall > 0);
                    bb17[3] = (tile.liquid > 0 && Main.netMode == 2);
                    bb17[4] = tile.wire();
                    bb17[5] = tile.halfBrick();
                    bb17[6] = tile.actuator();
                    bb17[7] = tile.inActive();
                    bb18[0] = tile.wire2();
                    bb18[1] = tile.wire3();
                    if (tile.active())// && tile.color() > 0) // Allow clearing paint
                    {
                        bb18[2] = true;
                        b = tile.color();
                    }
                    if (tile.wall > 0)// && tile.wallColor() > 0) // Allow clearing paint
                    {
                        bb18[3] = true;
                        b2 = tile.wallColor();
                    }
                    bb18 += (byte)(tile.slope() << 4);
                    bb18[7] = tile.wire4();
                    binaryWriter.Write(bb17);
                    binaryWriter.Write(bb18);
                    //if (b > 0) // Allow clearing paint
                    if (tile.active()) // Allow clearing paint
                    {
                        binaryWriter.Write(b);
                    }
                    //if (b2 > 0) // Allow clearing paint
                    if (tile.wall > 0) // Allow clearing paint
                    {
                        binaryWriter.Write(b2);
                    }
                    if (tile.active())
                    {
                        binaryWriter.Write(tile.type);
                        if (Main.tileFrameImportant[(int)tile.type])
                        {
                            binaryWriter.Write(tile.frameX);
                            binaryWriter.Write(tile.frameY);
                        }
                    }
                    if (tile.wall > 0)
                    {
                        binaryWriter.Write(tile.wall);
                    }
                    if (tile.liquid > 0 && Main.netMode == 2)
                    {
                        binaryWriter.Write(tile.liquid);
                        binaryWriter.Write(tile.liquidType());
                    }
                }
            }
        }

        #endregion
    }
}
