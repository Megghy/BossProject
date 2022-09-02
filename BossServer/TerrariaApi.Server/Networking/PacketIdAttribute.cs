namespace ClientApi.Networking
{
    public class PacketIdAttribute : Attribute
    {
        public PacketId Id { get; set; }

        public PacketIdAttribute(PacketId id)
        {
            Id = id;
        }
    }
}
