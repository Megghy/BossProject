using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BossFramework.BHooks.HookHandlers
{
    public static class GameUpdateHandler
    {
        public static void OnGameUpdate(EventArgs args)
        {
            BInfo.GameTick++;
        }
    }
}
