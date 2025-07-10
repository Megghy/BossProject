using System.ComponentModel;
using Microsoft.Xna.Framework;

namespace TerrariaApi.Server
{
    public class ServerBroadcastEventArgs : HandledEventArgs
    {
        public Terraria.Localization.NetworkText Message { get; set; }
        public Color Color { get; set; }
    }
}
