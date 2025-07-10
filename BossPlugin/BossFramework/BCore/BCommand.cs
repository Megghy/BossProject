using System.Reflection;
using BossFramework.BAttributes;
using BossFramework.BInterfaces;
using BossFramework.BModels;
using TerrariaApi.Server;
using TShockAPI;

namespace BossFramework.BCore
{
    public static class BCommand
    {
        #region 命令提示
        public const string InvalidInput = "无效的命令格式";
        #endregion
        public static string ScriptCmdPath => Path.Combine(ScriptManager.ScriptRootPath, "Cmds");
        internal static readonly List<BaseCommand> Cmds = new();
        private static readonly List<Command> _tsCmds = new();
        [AutoInit("注册Boss服命令")]
        public static void RegisteAllCommands()
        {
            if (!Directory.Exists(ScriptCmdPath))
                Directory.CreateDirectory(ScriptCmdPath);

            TShockAPI.Hooks.PlayerHooks.PlayerCommand += OnUseAnyCmd;

            try
            {
                var loaded = new List<Assembly>();
                ServerApi.Plugins.Select(p => p.Plugin.GetType().Assembly)
                .Where(a => a != null)
                .ForEach(a =>
                {
                    if (!loaded.Contains(a))
                    {
                        a.GetTypes()
                            .Where(t => t.BaseType == typeof(BaseCommand))
                            .ForEach(t =>
                            {
                                var tempCMD = (BaseCommand)Activator.CreateInstance(t)!;
                                tempCMD.Init();
                                tempCMD.RegisterAllSubCommands();
                                Cmds.Add(tempCMD);
                                RegisteToTS(tempCMD);

                                BLog.Info($"加载命令 [{string.Join(", ", tempCMD.Names)}]");
                            });

                        loaded.Add(a);
                    }
                });

                //加载脚本命令文件
                ScriptManager.LoadScripts<BaseCommand>(ScriptCmdPath)?
                    .ForEach(s =>
                    {
                        s.Init();
                        s.RegisterAllSubCommands();
                        Cmds.Add(s);
                        RegisteToTS(s);
                        BLog.Info($"-- 加载脚本命令 [{string.Join(", ", s.Names)}]");
                    });
                BLog.Success($"共加载 {Cmds.Count} 个命令");
            }
            catch (Exception ex) { BLog.Error($"注册命令失败\r\n{ex}"); }
        }
        /// <summary>
        /// 将命令添加到tshock
        /// </summary>
        /// <param name="cmd"></param>
        private static void RegisteToTS(BaseCommand cmd)
        {
            var tscmd = new Command(OnBCmd, cmd.Names)
            {
                HelpText = cmd.Description
            };
            Commands.ChatCommands.Add(tscmd);
            _tsCmds.Add(tscmd);
        }
        [Reloadable]
        public static void ReloadCmds()
        {
            _tsCmds.ForEach(c => Commands.ChatCommands.Remove(c));
            _tsCmds.Clear();
            Cmds.ForEach(c => c.Dispose());
            Cmds.Clear();
            RegisteAllCommands();
            BLog.Success("指令已重载");
        }
        private static void OnUseAnyCmd(TShockAPI.Hooks.PlayerCommandEventArgs args)
        {
            CommandPlaceholder.Placeholders.Where(p => p.Match(args.CommandText))
                .ForEach(p =>
                {
                    args.CommandText = p.Replace(new(args.Player.GetBPlayer()), args.CommandText);
                    if (args.Parameters.Count != 0)
                        for (int i = 0; i < args.Parameters.Count; i++)
                        {
                            if (p.Match(args.Parameters[i]))
                                args.Parameters[i] = p.Replace(new(args.Player.GetBPlayer()), args.Parameters[i]);
                        }
                });
        }
        /// <summary>
        /// 当ts玩家调用命令
        /// </summary>
        /// <param name="args"></param>
        private static void OnBCmd(CommandArgs args)
        {
            if (args.Player.Account is null)
            {
                args.Player.SendInfoMessage($"尚未登陆, 无法使用此命令");
                return;
            }
            var cmdName = args.Message.Contains(' ') ? args.Message.Split(' ')[0] : args.Message;

            if (Cmds.FirstOrDefault(c => c.Names.Any(c => c.ToLower() == cmdName.ToLower())) is { } cmd)
            {
                if (args.Parameters.Any())
                {
                    var subCmds = cmd.SubCommands.Where(s => s.Names?.Any(n => n.ToLower() == args.Parameters[0].ToLower()) ?? false).ToArray();
                    if (subCmds.Any())
                        subCmds.ForEach(s =>
                        {
                            ExcuteSubCmd(cmd, s, args, cmdName);
                        });
                    else if (cmd.SubCommands.FirstOrDefault(s => !s.Names?.Any() ?? true) is { } defaultSubCmd)
                        ExcuteSubCmd(cmd, defaultSubCmd, args, cmdName);
                    else
                        cmd.Help(new(args, cmdName));
                }
                else if (cmd.HasDefaultCommand)
                    cmd.Default(new(args, cmdName));
                else
                    cmd.Help(new(args, cmdName));
            }
        }
#pragma warning disable S3168 // "async" methods should not return "void"
        private static async void ExcuteSubCmd(BaseCommand baseCmd, SubCommandAttribute subCmd, CommandArgs args, string cmdName)
#pragma warning restore S3168 // "async" methods should not return "void"
        {
            try
            {
                if (!string.IsNullOrEmpty(subCmd.Permission) && !args.Player.HasPermission(subCmd.Permission))
                {
                    BLog.Info($"{args.Player.Name} 尝试使用命令 {args.Message}");
                    args.Player.SendInfoMessage("你没有权限使用此命令");
                }
                else
                {
                    var subArg = new SubCommandArgs(args, cmdName);
                    var isAwaitable = subCmd.Method?.ReturnType.GetMethod(nameof(Task.GetAwaiter)) != null;
                    var invokeArg = subCmd.Method?.GetParameters().Any() == true ? new object[] { subArg } : Array.Empty<object>();
                    if (isAwaitable)
                    {
                        if (subCmd.Method?.ReturnType.IsGenericType == true)
                        {
                            await (dynamic)(subCmd.Method.Invoke(baseCmd, invokeArg) ?? Task.Delay(1));
                        }
                        else
                        {
                            await (Task)(subCmd.Method?.Invoke(baseCmd, invokeArg) ?? Task.Delay(1));
                        }
                    }
                    else
                        subCmd.Method?.Invoke(baseCmd, invokeArg);
                }
            }
            catch (Exception ex)
            {
                BLog.Error($"命令执行出错{Environment.NewLine}{ex}");
            }
        }
    }
}
