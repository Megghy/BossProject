using System.IO;
using TrProtocol;

namespace TShockAPI.Handlers.NetModules
{
    /// <summary>
    /// Rejects ambience new modules from clients
    /// </summary>
    public class AmbienceHandler : INetModuleHandler
    {
        /// <summary>
        /// No deserialization needed. This should never be received by the server
        /// </summary>
        /// <param name="data"></param>
        public void Deserialize(BinaryBufferReader data)
        {
        }

        /// <summary>
        /// This should never be received by the server
        /// </summary>
        /// <param name="player"></param>
        /// <param name="rejectPacket"></param>
        public void HandlePacket(TSPlayer player, out bool rejectPacket)
        {
            rejectPacket = true;
        }
    }
}
