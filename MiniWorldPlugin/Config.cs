using BossFramework.BInterfaces;

namespace MiniWorldPlugin
{
    public class Config : BaseConfig<Config>
    {
        protected override string FilePath => Path.Combine(TShockAPI.TShock.SavePath, "MiniWorld.json");

        public int MaxWorldsPerPlayer { get; set; } = 3;
        public int DefaultWorldPortStart { get; set; } = 27000;
        /// <summary>
        /// 工作节点的 RPC 服务器地址
        /// </summary>
        public string RpcUrl { get; set; } = "http://localhost:8080/rpc/";
    }
}