using BossFramework.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scattering
{
    public class PlayerSpawn : DBStructBase<PlayerSpawn>
    {
        public long WorldId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }
}
