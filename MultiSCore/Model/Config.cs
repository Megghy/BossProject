using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TShockAPI;

namespace MultiSCore.Model
{
    /// <summary>
    /// 服务器信息，描述一个可供跳转的目标服务器。
    /// </summary>
    public class ServerInfo
    {
        /// <summary>
        /// 与目标服务器通信的密钥
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// 是否在/msc list中可见
        /// </summary>
        public bool Visible { get; set; }
        /// <summary>
        /// 加入此服务器所需的权限
        /// </summary>
        public string Permission { get; set; }
        public string IP { get; set; }
        public int Port { get; set; }
        /// <summary>
        /// 服务器名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 在目标服务器的出生点X坐标（-1为默认）
        /// </summary>
        public int SpawnX { get; set; } = -1;
        /// <summary>
        /// 在目标服务器的出生点Y坐标（-1为默认）
        /// </summary>
        public int SpawnY { get; set; } = -1;
        /// <summary>
        /// 返回主服务器时是否恢复玩家的库存
        /// </summary>
        public bool RememberHostInventory { get; set; }
        /// <summary>
        /// 可以在子服务器上执行并由主服务器处理的全局命令列表
        /// </summary>
        public List<string> GlobalCommand { get; set; } = new();
    }
    public class Config
    {
        [JsonIgnore]
        public JObject Language { get; set; }
        /// <summary>
        /// 语言文件名称 (e.g., "zh_cn.json")
        /// </summary>
        public string LanguageFileName { get; set; }
        /// <summary>
        /// 本服务器的密钥，用于服务器间验证
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// 本服务器的名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 是否允许其他MultiSCore服务器的玩家加入
        /// </summary>
        public bool AllowOthorServerJoin { get; set; }
        /// <summary>
        /// 是否允许普通客户端直接加入
        /// </summary>
        public bool AllowDirectJoin { get; set; }
        /// <summary>
        /// 玩家返回主服务器时是否回到上次离开的位置
        /// </summary>
        public bool RememberLastPoint { get; set; }
        /// <summary>
        /// 可用的子服务器列表
        /// </summary>
        public List<ServerInfo> Servers { get; set; }

        public void Write(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public static Config Read(string path)
        {
            if (!File.Exists(path))
                return new Config();
            return JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));
        }

        public static Config LoadFromFile(string configPath)
        {
            Config config = null;
            try
            {
                var directoryPath = Path.GetDirectoryName(configPath);
                if (!Directory.Exists(directoryPath) || !File.Exists(configPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    config = CreateDefaultConfig();
                    config.Write(configPath);

                    CreateLanguageFiles(directoryPath, config.LanguageFileName);
                    TShock.Log.ConsoleInfo("<MultiSCore> 未找到配置文件, 已创建新的配置文件。");
                }
                else
                {
                    config = Read(configPath);
                }

                var languagePath = Path.Combine(directoryPath, config.LanguageFileName);
                if (File.Exists(languagePath))
                {
                    config.Language = JObject.Parse(File.ReadAllText(languagePath));
                }
                else
                {
                    CreateLanguageFiles(directoryPath, config.LanguageFileName);
                    TShock.Log.ConsoleError($"<MultiSCore> 文件 {config.LanguageFileName} 不存在, 已重新创建, 默认使用中文语言包。");
                    config.Language = JObject.Parse(Encoding.UTF8.GetString(Properties.Resources.zh_cn));
                }

                if (config.Servers.Any(s => s.Key.StartsWith("Terraria")))
                {
                    TShock.Log.ConsoleInfo("[MultiSCore] [警告] 配置文件中的一个服务器密钥以'Terraria'开头. 这可能会导致与原版客户端的连接冲突, 建议修改.");
                }
                TShock.Log.ConsoleInfo("<MultiSCore> 配置加载成功。");
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError("<MultiSCore> 读取配置文件失败: " + ex.Message);
            }
            return config;
        }

        private static void CreateLanguageFiles(string directoryPath, string defaultLanguageFile)
        {
            File.WriteAllText(Path.Combine(directoryPath, "zh_cn.json"), Encoding.UTF8.GetString(Properties.Resources.zh_cn));
            File.WriteAllText(Path.Combine(directoryPath, "en_us.json"), Encoding.UTF8.GetString(Properties.Resources.en_us));
        }

        private static Config CreateDefaultConfig()
        {
            return new Config()
            {
                LanguageFileName = "zh_cn.json",
                AllowDirectJoin = true,
                AllowOthorServerJoin = false,
                Key = Guid.NewGuid().ToString(),
                Name = "host",
                RememberLastPoint = true,
                Servers = new()
                {
                    new() {
                        Key = "replace_with_your_server_key",
                        Visible = true,
                        Permission = "",
                        IP = "127.0.0.1",
                        Port = 7777,
                        Name = "example-server",
                        SpawnX = -1,
                        SpawnY = -1,
                        RememberHostInventory = true,
                        GlobalCommand = new() { "online", "who" }
                    }
                }
            };
        }
    }
}