using BossFramework.DB;

namespace Scattering
{
    public class PlayerSpawn : DBStructBase<PlayerSpawn>
    {
        public long WorldId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }
}
