using BossFramework.BInterfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Terraria;
using TShockAPI;

public class RandomCmd1 : IScriptModule
{
    public string Name => "RandomCmd";
    public string Author => "yuyu";
    public string Version => "0.1";
    public static Configuration Config;

    private static List<Command> cmds = new List<Command>()
        {
            new Command("rc.use", Use, "rc", "randomcmd")
            {
                AllowServer = false,
                HelpText = "随机指令.使用方法/rc 表名"
            },
            new Command("rc.table", Table, "rctable")
            {
                AllowServer = true,
                HelpText = "制作随机指令表.使用方法/rc 表名 指令集(用;隔开，整个指令集用半角\"包起来)"
            },
            new Command("rc.del", Del, "rcdeltable")
            {
                AllowServer = true,
                HelpText = "删除随机指令表.使用方法/rcdeltable 表名"
            },
            new Command("rc.show", Show, "rcshow")
            {
                AllowServer = true,
                HelpText = "获取随机指令表内容.使用方法/rcshow 表名"
            },
        };

    public static void Use(CommandArgs args)
    {
        if (args.Parameters.Count != 1)
        {
            args.Player.SendErrorMessage("参数数量不对");
            return;
        }

        var tableName = args.Parameters[0];
        if (Config.Tables.TryGetValue(tableName, out var cmds))
        {
            HandleCommandIgnorePermission(args.Player, cmds[(new Random().Next(cmds.Count))]);
        }
    }

    public static void Table(CommandArgs args)
    {
        if (args.Parameters.Count != 2)
        {
            args.Player.SendErrorMessage("参数数量不对");
            return;
        }

        var tableName = args.Parameters[0];
        var cmds = args.Parameters[1].Split(';');

        if (Config.Tables.ContainsKey(tableName))
        {
            Config.Tables[tableName] = cmds.ToList();
        }
        else
        {
            Config.Tables.Add(tableName, cmds.ToList());
        }

        args.Player.SendInfoMessage("已创建表");

        Config.Write();
    }

    public static void Show(CommandArgs args)
    {
        if (args.Parameters.Count != 1)
        {
            args.Player.SendErrorMessage("参数数量不对");
            return;
        }

        var tableName = args.Parameters[0];
        if (Config.Tables.ContainsKey(tableName))
        {
            var sb = new StringBuilder();
            foreach (var c in Config.Tables[tableName])
            {
                sb.Append(c);
                sb.Append("\n");
            }

            args.Player.SendInfoMessage(sb.ToString());
        }
    }

    public static void Del(CommandArgs args)
    {
        if (args.Parameters.Count != 1)
        {
            args.Player.SendErrorMessage("参数数量不对");
            return;
        }

        var tableName = args.Parameters[0];
        if (Config.Tables.ContainsKey(tableName))
        {
            Config.Tables.Remove(tableName);
            args.Player.SendInfoMessage("已删除表");
        }

        Config.Write();
    }

    public void Initialize()
    {
        Config = Configuration.Read(Configuration.ConfigPath);

        foreach (var c in cmds)
        {
            Commands.ChatCommands.Add(c);
        }
    }

    public void Dispose()
    {
        foreach (var c in cmds)
        {
            Commands.ChatCommands.Remove(c);
        }
    }

    public static bool HandleCommandIgnorePermission(TSPlayer player, string text)
    {
        if (!Internal_ParseCmd(text, out var cmdText, out var cmdName, out var args, out var silent))
        {
            player.SendErrorMessage("指令无效；键入 {0}help 以获取可用指令。", Commands.Specifier);
            return false;
        }

        return Internal_HandleCommandIgnorePermission(player, cmdText, cmdName, args, silent);
    }

    private static bool Internal_ParseCmd(string text, out string cmdText, out string cmdName, out List<string> args, out bool silent)
    {
		if(text.StartsWith('/') || text.StartsWith('.'))
			cmdText = text.Remove(0, 1);
		else 
			cmdText = text;
        var cmdPrefix = text[0].ToString();
        silent = cmdPrefix == Commands.SilentSpecifier;

        var index = -1;
        for (var i = 0; i < cmdText.Length; i++)
        {
            if (Commands.IsWhiteSpace(cmdText[i]))
            {
                index = i;
                break;
            }
        }
        if (index == 0) // Space after the command specifier should not be supported
        {
            args = null;
            cmdName = null;
            return false;
        }
        cmdName = index < 0 ? cmdText.ToLower() : cmdText.Substring(0, index).ToLower();

        args = index < 0 ?
            new List<string>() :
            Commands.ParseParameters(cmdText.Substring(index));
        return true;
    }

    private static bool Internal_HandleCommandIgnorePermission(TSPlayer player, string cmdText, string cmdName, List<string> args, bool silent)
    {
        var cmds = Commands.ChatCommands.FindAll(x => x.HasAlias(cmdName));

        if (cmds.Count == 0)
        {
            if (player.AwaitingResponse.ContainsKey(cmdName))
            {
                Action<CommandArgs> call = player.AwaitingResponse[cmdName];
                player.AwaitingResponse.Remove(cmdName);
                call(new CommandArgs(cmdText, player, args));
                return true;
            }
            player.SendErrorMessage("键入的指令无效；使用 {0}help 查看有效指令。", Commands.Specifier);
            return true;
        }
        foreach (var cmd in cmds)
        {
            cmd.CommandDelegate?.Invoke(new CommandArgs(cmdText, silent, player, args));
        }
        return true;
    }
    public sealed class Configuration
    {
        public static readonly string ConfigPath = Path.Combine(TShock.SavePath, "RandomCmd.json");

        public Dictionary<string, List<string>> Tables = new Dictionary<string, List<string>>();

        public static Configuration Read(string path)
        {
            if (!File.Exists(path))
                return new Configuration();
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
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
            var path = ConfigPath;
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                var str = JsonConvert.SerializeObject(this, Formatting.Indented);
                using (var sw = new StreamWriter(fs))
                {
                    sw.Write(str);
                }
            }
        }
    }
}