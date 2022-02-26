namespace TrProtocol.Packets
{
    public struct UpdateTowerShieldStrengths : IPacket
    {
        public MessageID Type => MessageID.UpdateTowerShieldStrengths;
        [ArraySize(4)] public ushort[] ShieldStrength { get; set; }
    }
}