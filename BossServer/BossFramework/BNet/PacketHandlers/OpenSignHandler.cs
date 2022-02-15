﻿using BossFramework.BInterfaces;
using BossFramework.BModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrProtocol.Packets;
using TShockAPI;

namespace BossFramework.BNet.PacketHandlers
{
    public class OpenSignHandler : PacketHandlerBase<RequestReadSign>
    {

        public override bool OnGetPacket(BPlayer plr, RequestReadSign packet)
        {
            var args = new GetDataHandlers.SignReadEventArgs()
            {
                Player = plr.TsPlayer,
                Data = null,
                X = packet.Position.X,
                Y = packet.Position.Y
            };
            GetDataHandlers.SignRead.Invoke(BPlugin.Instance, args);
            if (!args.Handled)
                BCore.SignRedirector.OnOpenSign(plr, packet);
            return true;
        }

        public override bool OnSendPacket(BPlayer plr, RequestReadSign packet)
        {
            return false;
        }
    }
}