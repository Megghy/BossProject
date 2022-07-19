#region Using
using BossFramework;
using Terraria;
using TrProtocol.Models;
using TrProtocol.Packets;
using BitsByte = TrProtocol.Models.BitsByte;
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
            int X, int Y, int Width, int Height, int tileChangeType)
        {
            var data = new TileSquare()
            {
                Data = new()
                {
                    ChangeType = (TileChangeType)tileChangeType,
                    Height = (byte)Height,
                    Width = (byte)Width,
                    TilePosX = (short)X,
                    TilePosY = (short)Y,
                    Tiles = new SimpleTileData[Width, Height]
                }
            };
            (var trTiles, var relative) = FakeProviderAPI.ApplyPersonal(providers, X, Y, Width, Height);

            int rx = relative ? -X : 0;
            int ry = relative ? -Y : 0;

            for (int x = X; x < X + Width; x++)
            {
                for (int y = Y; y < Y + Height; y++)
                {
                    var tile = trTiles[x + rx, y + ry];
                    BitsByte bb1 = 0;
                    BitsByte bb2 = 0;

                    bb1[0] = tile.active();
                    bb1[2] = (tile.wall > 0);
                    bb1[3] = (tile.liquid > 0);
                    bb1[4] = tile.wire();
                    bb1[5] = tile.halfBrick();
                    bb1[6] = tile.actuator();
                    bb1[7] = tile.inActive();

                    bb2[0] = tile.wire2();
                    bb2[1] = tile.wire3();
                    bb2[2] = tile.active();
                    bb2[3] = tile.wall > 0;
                    bb2 += (byte)(tile.slope() << 4);
                    bb2[7] = tile.wire4();
                    data.Data.Tiles[x - X, y - Y] = (new()
                    {
                        Flags1 = bb1,
                        Flags2 = bb2,
                        FrameX = tile.frameX,
                        FrameY = tile.frameY,
                        Liquid = tile.liquid,
                        LiquidType = tile.liquid,
                        TileColor = tile.color(),
                        TileType = tile.type,
                        WallColor = tile.wallColor(),
                        WallType = tile.wall,
                    });
                }
            }
            return data.SerializePacket();
        }

        #endregion
    }
}
