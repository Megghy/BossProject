namespace TrProtocol.Models;

[Serializer(typeof(PrimitiveFieldSerializer<MessageSource>))]
public enum MessageSource : byte
{
    Idle,
    Storage,
    ThrownAway,
    PickedUp,
    ChoppedTree,
    ChoppedGemTree,
    ChoppedCactus,
    Count
}