#region Using
using Microsoft.Xna.Framework;
using System.IO.Compression;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using TrProtocol;
using TrProtocol.Models;
using TrProtocol.Models.TileEntities;
using TileEntity = Terraria.DataStructures.TileEntity;
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
                FakeProviderPlugin.SendTo(group, Generate(group.Key, X, Y, Width, Height));
        }

        #endregion
        //失败了 悲
        /*#region Generate

        private static byte[] Generate(IEnumerable<TileProvider> providers, int X, int Y, int Width, int Height)
        {
            var rec = new Rectangle(X, Y, Width, Height);
            var eneities = TileEntity.ByPosition.Where(e => rec.Contains(e.Key.X, e.Key.Y)).Select(e => Activator.CreateInstance(Constants.tileEntityDict[(TileEntityType)e.Value.type], new object[] { e.Value }) as IProtocolTileEntity).ToArray();
            var tiles = new List<ComplexTileData>();
            (var trTiles, var relative) = FakeProviderAPI.ApplyPersonal(providers, X, Y, Width, Height);

             int rx = relative ? -X : 0;
            int ry = relative ? -Y : 0;

            ITile lastTile = trTiles[X + rx, Y + ry];
            short tileCount = 0;
            for (int x = X; x < X + Width; x++)
            {
                for (int y = Y; y < Y + Height; y++)
                {
                    var tile = trTiles[x + rx, y + ry];
                    if (tile?.isTheSameAs(lastTile) == true && tileCount < short.MaxValue)
                        tileCount++;
                    else
                    {
                        tiles.Add(new()
                        {
                            Count = tileCount,
                            Flags1 = tile.bTileHeader,
                            Flags2 = tile.bTileHeader2,
                            Flags3 = tile.bTileHeader3,
                            FrameX = tile.frameX,
                            FrameY = tile.frameY,
                            Liquid = tile.liquid,
                            TileColor = tile.color(),
                            TileType = tile.type,
                            WallColor = tile.wallColor(),
                            WallType = tile.wall
                        });

                        tileCount = 0;
                    }
                    lastTile = tile;
                }
            }
            return new TileSection()
            {
                Data = new()
                {
                    StartX = X,
                    StartY = Y,
                    Width = (short)Width,
                    Height = (short)Height,
                    IsCompressed = true,
                    Tiles = tiles.ToArray(),
                    SignCount = 0,
                    ChestCount = 0,
                    TileEntityCount = (short)eneities.Length,
                    TileEntities = eneities
                }
            }.SerializePacket();
        }

        #endregion*/
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
        private static readonly Dictionary<TileEntityType, Type> tileEntityDict = new()
    {
        { TileEntityType.TETrainingDummy, typeof(TETrainingDummy) },
        { TileEntityType.TEItemFrame, typeof(TEItemFrame) },
        { TileEntityType.TELogicSensor, typeof(TELogicSensor) },
        { TileEntityType.TEDisplayDoll, typeof(TEDisplayDoll) },
        { TileEntityType.TEWeaponsRack, typeof(TEWeaponsRack) },
        { TileEntityType.TEHatRack, typeof(TEHatRack) },
        { TileEntityType.TEFoodPlatter, typeof(TEFoodPlatter) },
        { TileEntityType.TETeleportationPylon, typeof(TETeleportationPylon) }
    };
        private static void CompressTileBlock_Inner(IEnumerable<TileProvider> providers,
            BinaryWriter writer, int xStart, int yStart, int width, int height)
        {
            var rec = new Rectangle(xStart, yStart, width, height);
            var eneities = TileEntity.ByPosition.Where(e => rec.Contains(e.Key.X, e.Key.Y)).Select(e => BossFramework.BUtils.ToProtocalTileEntity(e.Value)).ToArray();
            short num4 = 0;
            int num5 = 0;
            int num6 = 0;
            byte b = 0;
            byte[] array4 = new byte[15];
            ITile tile = null;
            (var tiles, bool relative) = FakeProviderAPI.ApplyPersonal(providers, xStart, yStart, width, height);
            int dj = relative ? -xStart : 0;
            int di = relative ? -yStart : 0;
            for (int i = yStart; i < yStart + height; i++)
            {
                for (int j = xStart; j < xStart + width; j++)
                {
                    ITile tile2 = tiles[j + dj, i + di];
                    if (tile2.isTheSameAs(tile) && TileID.Sets.AllowsSaveCompressionBatching[(int)tile2.type])
                    {
                        num4 += 1;
                    }
                    else
                    {
                        if (tile != null)
                        {
                            if (num4 > 0)
                            {
                                array4[num5] = (byte)(num4 & 255);
                                num5++;
                                if (num4 > 255)
                                {
                                    b |= 128;
                                    array4[num5] = (byte)(((int)num4 & 65280) >> 8);
                                    num5++;
                                }
                                else
                                {
                                    b |= 64;
                                }
                            }
                            array4[num6] = b;
                            writer.Write(array4, num6, num5 - num6);
                            num4 = 0;
                        }
                        num5 = 3;
                        byte b3;
                        byte b2 = b = (b3 = 0);
                        if (tile2.active())
                        {
                            b |= 2;
                            array4[num5] = (byte)tile2.type;
                            num5++;
                            if (tile2.type > 255)
                            {
                                array4[num5] = (byte)(tile2.type >> 8);
                                num5++;
                                b |= 32;
                            }
                            if (Main.tileFrameImportant[(int)tile2.type])
                            {
                                array4[num5] = (byte)(tile2.frameX & 255);
                                num5++;
                                array4[num5] = (byte)(((int)tile2.frameX & 65280) >> 8);
                                num5++;
                                array4[num5] = (byte)(tile2.frameY & 255);
                                num5++;
                                array4[num5] = (byte)(((int)tile2.frameY & 65280) >> 8);
                                num5++;
                            }
                            if (tile2.color() != 0)
                            {
                                b3 |= 8;
                                array4[num5] = tile2.color();
                                num5++;
                            }
                        }
                        if (tile2.wall != 0)
                        {
                            b |= 4;
                            array4[num5] = (byte)tile2.wall;
                            num5++;
                            if (tile2.wallColor() != 0)
                            {
                                b3 |= 16;
                                array4[num5] = tile2.wallColor();
                                num5++;
                            }
                        }
                        if (tile2.liquid != 0)
                        {
                            if (tile2.lava())
                            {
                                b |= 16;
                            }
                            else if (tile2.honey())
                            {
                                b |= 24;
                            }
                            else
                            {
                                b |= 8;
                            }
                            array4[num5] = tile2.liquid;
                            num5++;
                        }
                        if (tile2.wire())
                        {
                            b2 |= 2;
                        }
                        if (tile2.wire2())
                        {
                            b2 |= 4;
                        }
                        if (tile2.wire3())
                        {
                            b2 |= 8;
                        }
                        int num31;
                        if (tile2.halfBrick())
                        {
                            num31 = 16;
                        }
                        else if (tile2.slope() != 0)
                        {
                            num31 = (int)(tile2.slope() + 1) << 4;
                        }
                        else
                        {
                            num31 = 0;
                        }
                        b2 |= (byte)num31;
                        if (tile2.actuator())
                        {
                            b3 |= 2;
                        }
                        if (tile2.inActive())
                        {
                            b3 |= 4;
                        }
                        if (tile2.wire4())
                        {
                            b3 |= 32;
                        }
                        if (tile2.wall > 255)
                        {
                            array4[num5] = (byte)(tile2.wall >> 8);
                            num5++;
                            b3 |= 64;
                        }
                        num6 = 2;
                        if (b3 != 0)
                        {
                            b2 |= 1;
                            array4[num6] = b3;
                            num6--;
                        }
                        if (b2 != 0)
                        {
                            b |= 1;
                            array4[num6] = b2;
                            num6--;
                        }
                        tile = tile2;
                    }
                }
            }
            if (num4 > 0)
            {
                array4[num5] = (byte)(num4 & 255);
                num5++;
                if (num4 > 255)
                {
                    b |= 128;
                    array4[num5] = (byte)(((int)num4 & 65280) >> 8);
                    num5++;
                }
                else
                {
                    b |= 64;
                }
            }
            array4[num6] = b;
            writer.Write(array4, num6, num5 - num6);
            writer.Write((short)0); //没有牌子
            writer.Write((short)0); //不发箱子
            writer.Write((short)eneities.Length);
            for (int m = 0; m < eneities.Length; m++)
            {
                TrProtocol.Models.TileEntity.Write(writer, eneities[m]);
            }
        }

        #endregion
    }
}
