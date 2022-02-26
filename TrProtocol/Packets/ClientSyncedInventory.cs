namespace TrProtocol.Packets
{
    public struct ClientSyncedInventory : IPacket
    {
        public MessageID Type => MessageID.ClientSyncedInventory;
    }
}