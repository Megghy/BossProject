using System.ComponentModel;

namespace TerrariaApi.Server
{
    public class NpcTransformationEventArgs : HandledEventArgs
    {
        public int NpcId
        {
            get;
            set;
        }
    }
}
