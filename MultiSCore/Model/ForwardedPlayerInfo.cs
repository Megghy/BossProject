namespace MultiSCore.Model
{
    /// <summary>
    /// 存储通过代理进入此服务器的玩家的来源信息。
    /// </summary>
    public class ForwardedPlayerInfo
    {
        public string TerrariaVersion { get; set; }
        public Version PluginVersion { get; set; }
        public string SourceServerKey { get; set; }
    }
}