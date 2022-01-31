using ClientApi.Networking;
using System;
using System.IO;
using System.IO.Streams;

namespace TerrariaApi.Server.Networking.TerrariaPackets
{
    [PacketId(PacketId.Version)]
    public class VersionPacket : IPacket
    {
        public String Version { get; set; }

        public void Read(Stream s)
        {
            Version = s.ReadString();
        }

        public void Write(Stream s)
        {
            s.WriteString(Version);
        }
    }
}
