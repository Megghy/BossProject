using System.ComponentModel;

namespace TerrariaApi.Server
{
    public class WorldSaveEventArgs : HandledEventArgs
    {
        public bool ResetTime
        {
            get;
            internal set;
        }
    }
    public class WorldPostSaveEventArgs : HandledEventArgs
    {
        public bool ResetTime
        {
            get;
            internal set;
        }
    }
}
