using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrProtocol;

namespace BossPlugin.BModels
{
    public interface IEventArgs
    {
        public static bool Handled { get; set; } = false;
    }
    public static class EventArgs
    {
        public class PacketEventArgs : IEventArgs
        {
            public PacketEventArgs(BPlayer plr, Packet packet)
            {
                Player = plr;
                Packet = packet;
            }
            public BPlayer Player { get; private set; }
            public Packet Packet { get; set; }
            public bool Handled { get; set; } = false;
        }
    }
}
