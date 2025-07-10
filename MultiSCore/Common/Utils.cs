using System.Net;
using Microsoft.Xna.Framework;
using Terraria;
using TShockAPI;
using TShockAPI.Configuration;

namespace MultiSCore.Common
{
    /// <summary>
    /// 提供插件所需的各种辅助方法和扩展方法。
    /// </summary>
    public static class Utils
    {
        // 耦合代码，将在后续步骤中重构或移除
        // public static string GetKey(int index) ...
        // public static HostInfo GetForwordInfo(this TSPlayer plr) ...
        // public static bool IsForwordPlayer(this TSPlayer plr) => ...
        // public static bool IsInForword(this TSPlayer plr) => ...
        // public static void SendMessageToHostPlayer(string text) ...

        // 兼容不同TShock版本的配置值获取方法。
        internal static T GetConfigValue<T>(string name)
        {
            try
            {
                if (TShock.VersionNum < new Version(4, 5, 0, 0))
                    return GetConfigValue_440<T>(name);
                else
                    return GetConfigValue_450<T>(name);
            }
            catch (Exception ex)
            { TShock.Log.ConsoleError($"<MultiSCore> 获取配置值时出错: {ex.Message}"); return default; }
        }
        static T GetConfigValue_450<T>(string name) => (T)typeof(TShockSettings).GetField(name).GetValue(TShock.Config.Settings);
        static T GetConfigValue_440<T>(string name) => (T)AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "TShockAPI").GetType("TShockAPI.ConfigFile").GetField(name).GetValue(typeof(TShock).GetProperty("Config").GetValue(null));


        /// <summary>
        /// 尝试将IP地址或域名解析为字符串形式的IP地址。
        /// </summary>
        public static bool TryParseAddress(string address, out string ip)
        {
            ip = "";
            try
            {
                if (IPAddress.TryParse(address, out _))
                {
                    ip = address;
                    return true;
                }
                else
                {
                    IPHostEntry hostinfo = Dns.GetHostEntry(address);
                    if (hostinfo.AddressList.FirstOrDefault() is { } _ip)
                    {
                        ip = _ip.ToString();
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        internal static readonly string ServerPrefix = $"<[C/A8D9D0:MultiSCore]> ";

        /// <summary>
        /// 向玩家发送成功消息。
        /// </summary>
        public static void SendSuccessMsg(this TSPlayer tsp, object text, bool playsound = true)
        {
            tsp?.SendMessage(ServerPrefix + text, new Color(120, 194, 96));
            if (playsound) NetMessage.PlayNetSound(new NetMessage.NetSoundInfo(tsp.TPlayer.position, 122, -1, 0.62f), tsp.Index);
        }

        /// <summary>
        /// 向玩家发送参考消息。
        /// </summary>
        public static void SendInfoMsg(this TSPlayer tsp, object text)
        {
            tsp?.SendMessage(ServerPrefix + text, new Color(216, 212, 82));
        }

        /// <summary>
        /// 向玩家发送错误消息。
        /// </summary>
        public static void SendErrorMsg(this TSPlayer tsp, object text)
        {
            tsp?.SendMessage(ServerPrefix + text, new Color(195, 83, 83));
        }

        /// <summary>
        /// 向玩家发送通用消息。
        /// </summary>
        public static void SendMsg(this TSPlayer tsp, object text, Color color = default)
        {
            color = color == default ? new Color(212, 239, 245) : color;
            tsp?.SendMessage(ServerPrefix + text, color);
        }
    }
}