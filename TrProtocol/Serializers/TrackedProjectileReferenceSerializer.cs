using System.IO;

namespace TrProtocol.Models
{
    public class TrackedProjectileReferenceSerializer : FieldSerializer<TrackedProjectileReference>
    {
        protected override TrackedProjectileReference _Read(BinaryBufferReader br)
        {
            var proj = new TrackedProjectileReference
            {
                ProjectileOwnerIndex = br.ReadInt16()
            };
            if (proj.ProjectileOwnerIndex == -1)
                return proj;
            proj.ProjectileIdentity = br.ReadInt16();
            proj.ProjectileType = br.ReadInt16();
            return proj;
        }

        protected override void _Write(BinaryWriter bw, TrackedProjectileReference t)
        {
            bw.Write(t.ProjectileOwnerIndex);
            if (t.ProjectileOwnerIndex == -1)
                return;
            bw.Write(t.ProjectileIdentity);
            bw.Write(t.ProjectileType);
        }
    }
}
