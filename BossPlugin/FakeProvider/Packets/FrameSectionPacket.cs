#region Using
using Terraria;
#endregion
namespace FakeProvider
{
    class FrameSectionPacket
    {
        #region Send

        public static void Send(int Who, int IgnoreIndex,
                short SX, short SY, short EX, short EY) =>
            Send(((Who == -1) ? FakeProviderPlugin.AllPlayers : new int[] { Who }),
                IgnoreIndex, SX, SY, EX, EY);

        public static void Send(IEnumerable<int> Who, int IgnoreIndex,
            short SX, short SY, short EX, short EY)
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

            FakeProviderPlugin.SendTo(clients, Generate(SX, SY, EX, EY));
        }

        #endregion
        #region Generate

        private static byte[] Generate(short SX, short SY, short EX, short EY)
        {

            byte[] data;
            using (MemoryStream ms = new())
            using (BinaryWriter bw = new(ms))
            {
                bw.BaseStream.Position = 2L;
                bw.Write((byte)PacketTypes.TileFrameSection);

                bw.Write(SX);
                bw.Write(SY);
                bw.Write(EX);
                bw.Write(EY);

                long position = bw.BaseStream.Position;
                bw.BaseStream.Position = 0L;
                bw.Write((short)position);
                bw.BaseStream.Position = position;
                data = ms.ToArray();
            }
            return data;
        }

        #endregion
    }
}
