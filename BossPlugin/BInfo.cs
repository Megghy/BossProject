using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;

namespace BossPlugin
{
    public static class BInfo
    {
        public static string FilePath => Path.Combine(Environment.CurrentDirectory, TShock.SavePath, "Boss");
    }
}
