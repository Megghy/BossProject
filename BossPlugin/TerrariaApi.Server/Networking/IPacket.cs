using System.IO;

namespace ClientApi.Networking
{
    public interface IPacket
    {
        void Read(Stream s);
        void Write(Stream s);
    }
}
