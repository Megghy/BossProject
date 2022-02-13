using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrProtocol.Models;

namespace TrProtocol.Serializers
{
    public class TileEntitySerializer : FieldSerializer<IProtocolTileEntity>
    {
        protected override IProtocolTileEntity _Read(BinaryReader br)
        {
            return IProtocolTileEntity.Read(br);
        }

        protected override void _Write(BinaryWriter bw, IProtocolTileEntity t)
        {
            IProtocolTileEntity.Write(bw, t);
        }
    }
}
