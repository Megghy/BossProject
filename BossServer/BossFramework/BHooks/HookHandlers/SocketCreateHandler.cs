using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OTAPI.Hooks.Netplay;

namespace BossFramework.BHooks.HookHandlers
{
    public static class SocketCreateHandler
    {
        public static void OnSocketCreate(object o, CreateTcpListenerEventArgs args)
        {
            args.Result = new BNet.AsyncSocket();
        }
    }
}
