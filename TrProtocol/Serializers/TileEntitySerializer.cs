using System.IO;
using TrProtocol.Models;

namespace TrProtocol.Serializers
{
    public class TileEntitySerializer : FieldSerializer<IProtocolTileEntity>
    {
        protected override IProtocolTileEntity _Read(BinaryBufferReader br)
        {
            return IProtocolTileEntity.Read(br);
        }

        protected override void _Write(BinaryWriter bw, IProtocolTileEntity t)
        {
            IProtocolTileEntity.Write(bw, t);
        }
    }
}
