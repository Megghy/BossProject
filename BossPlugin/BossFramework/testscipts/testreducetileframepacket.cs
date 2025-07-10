using BossFramework.BInterfaces;
using Microsoft.Xna.Framework;

namespace BossFramework.testscipts
{
    public class reducetileframepacket : IScriptModule
    {
        public string Name => "testreducetileframepacket";
        public string Author => "Megghy";
        public string Version => "1.0.0";

        public void Dispose()
        {
            BossFramework.BNet.PacketHandler.DeregistePacketHandler(HandleTileFrameSection);
        }

        public void Initialize()
        {
            BossFramework.BNet.PacketHandler.RegisteGetPacketHandler(PacketTypes.TileSendSection, HandleTileFrameSection);
        }

        private void HandleTileFrameSection(BossFramework.BModels.BEventArgs.PacketEventArgs args)
        {
            if (args.Packet is TrProtocol.Packets.TileSection packet)
            {
                var rect = new Rectangle(packet.Data.StartX, packet.Data.StartY, packet.Data.StartX + packet.Data.Width, packet.Data.StartY + packet.Data.Height);
                var spawnRect = new Rectangle(Terraria.Main.spawnTileX - 100, Terraria.Main.spawnTileY - 100, 200, 200);
                if (spawnRect.Intersects(rect))
                {
                    args.Handled = true;
                }
            }
        }
    }
}
