namespace TrProtocol.Packets
{
    public struct KillProjectile : IPacket, IProjSlot, IPlayerSlot
    {
        public MessageID Type => MessageID.KillProjectile;
        public short ProjSlot { get; set; }
        public byte PlayerSlot { get; set; }
    }
}