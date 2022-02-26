namespace TrProtocol.Packets
{
    public struct SyncProjectileTrackers : IPacket, IPlayerSlot
    {
        public MessageID Type => MessageID.SyncProjectileTrackers;

        public byte PlayerSlot { get; set; }
        public TrackedProjectileReference PiggyBankTracker { get; set; }
        public TrackedProjectileReference VoidLensTracker { get; set; }
    }
}
