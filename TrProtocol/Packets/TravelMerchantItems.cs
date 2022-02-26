namespace TrProtocol.Packets
{
    public struct TravelMerchantItems : IPacket
    {
        public MessageID Type => MessageID.TravelMerchantItems;
        [ArraySize(40)] public short[] ShopItems { get; set; }
    }
}