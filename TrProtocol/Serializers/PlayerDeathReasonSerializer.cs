using System.IO;
using Terraria.DataStructures;

namespace TrProtocol.Models
{
    public class PlayerDeathReasonSerializer : FieldSerializer<PlayerDeathReason>
    {
        protected override PlayerDeathReason _Read(BinaryReader br)
        {
            return PlayerDeathReason.FromReader(br);
        }

        protected override void _Write(BinaryWriter bw, PlayerDeathReason t)
        {
            t.WriteSelfTo(bw);
        }
    }
}