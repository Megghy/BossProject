using System.ComponentModel;

namespace TerrariaApi.Server
{
    public class ConnectEventArgs : HandledEventArgs
    {
        public string ConnectMessage { get; internal set; }
        public int Who
        {
            get;
            internal set;
        }
    }
}
