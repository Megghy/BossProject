using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct SpawnPlayer : IPacket, IPlayerSlot
    {
        public MessageID Type => MessageID.SpawnPlayer;
        public byte PlayerSlot { get; set; }
        public ShortPosition Position { get; set; }
        public int Timer { get; set; }
        public PlayerSpawnContext Context { get; set; }
    }
}
