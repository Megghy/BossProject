﻿using AlternativeCommandExecution.ShortCommand;
using Newtonsoft.Json;
using TShockAPI;

namespace AlternativeCommandExecution
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Configuration
    {
        [JsonProperty("指令分隔符")]
        public string CommandSpecifier = "\\";

        [JsonProperty("指令分隔符2")]
        public string CommandSpecifier2 = "、";

        [JsonProperty("简写指令")]
        public ShortCommandItem[] ShortCommands;

        public static Configuration Read()
        {
            if (!File.Exists(ConfigPath))
            {
                return new Configuration
                {
                    ShortCommands = new[]
                    {
                        new ShortCommandItem
                        {
                            CommandLines = new []
                            {
                                "spawnrate 0",
                                "maxspawns 0",
                                "butcher"
                            },
                            Names = new []
                            {
                                "zeronpc",
                                "灭怪"
                            }
                        },
                        new ShortCommandItem
                        {
                            CommandLines = new []
                            {
                                "clear npc 999999999",
                                "clear item 999999999",
                                "clear proj 999999999"
                            },
                            Names = new []
                            {
                                "saveserver",
                                "救服"
                            }
                        },
                        new ShortCommandItem
                        {
                            ParameterDescription = "{$Player}",
                            CommandLines = new []
                            {
                                "kill {Player}"
                            },
                            Names = new []
                            {
                                "zs",
                                "自杀"
                            }
                        }
                    }
                };
            }

            using (var fs = new FileStream(ConfigPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var sr = new StreamReader(fs))
                {
                    var cf = JsonConvert.DeserializeObject<Configuration>(sr.ReadToEnd());
                    return cf;
                }
            }
        }

        public void Write()
        {
            Directory.CreateDirectory(Directory.GetParent(ConfigPath).FullName);
            var str = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(ConfigPath, str);
        }

        private static readonly string ConfigPath = Path.Combine(TShock.SavePath, "ace.json");
    }
}
