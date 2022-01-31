using System.ComponentModel;

namespace TerrariaApi.Server
{
    public class NpcSpawnEventArgs : HandledEventArgs
    {
        public int NpcId
        {
            get; set;
        }
    }
}
