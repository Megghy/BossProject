using BossPlugin.BAttributes;
using BossPlugin.BCore;
using BossPlugin.BInterfaces;
using BossPlugin.BModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TShockAPI;

namespace BossPlugin.BModules
{
    public class BCommand
    {
        public static string ScriptCmdPath => Path.Combine(ScriptManager.ScriptRootPath, "Cmds");
        public static readonly List<BaseCommand> Cmds = new();
        private static readonly List<Command> _tsCmds = new();
        [AutoInit("注册Boss服命令")]
        public static void RegisteAllCommands()
        {
            if (!Directory.Exists(ScriptCmdPath))
                Directory.CreateDirectory(ScriptCmdPath);
            try
            {
                Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => t.BaseType == typeof(BaseCommand))
                    .ForEach(t =>
                    {
                        var tempCMD = (BaseCommand)Activator.CreateInstance(t);
                        tempCMD.RegisterAllSubCommands();
                        Cmds.Add(tempCMD);
                        RegisteToTS(tempCMD);
                    });

                //加载脚本命令文件
                ScriptManager.LoadScripts<BaseCommand>(ScriptCmdPath)?
                    .ForEach(s =>
                    {
                        s.RegisterAllSubCommands();
                        Cmds.Add(s);
                        RegisteToTS(s);
                    });
            }
            catch (Exception ex) { BLog.Error($"注册命令失败\r\n{ex}"); }
        }
        /// <summary>
        /// 将命令添加到tshock
        /// </summary>
        /// <param name="cmd"></param>
        private static void RegisteToTS(BaseCommand cmd)
        {
            var tscmd = new Command(OnCmd, cmd.Names);
            Commands.ChatCommands.Add(tscmd);
            _tsCmds.Add(tscmd);
        }
        [Reloadable]
        public static void ReloadCmds()
        {
            _tsCmds.ForEach(c => Commands.ChatCommands.Remove(c));
            _tsCmds.Clear();
            Cmds.Clear();
            RegisteAllCommands();
            BLog.Success("指令已重载");
        }
        /// <summary>
        /// 当ts玩家调用命令
        /// </summary>
        /// <param name="args"></param>
        private static void OnCmd(CommandArgs args)
        {
            var cmdName = args.Message.Contains(" ") ? args.Message.Split(' ')[0] : args.Message;
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
                        args.Player.SendInfoMessage($"无效的命令");
                }
            }
        }
        private static async void ExcuteSubCmd(BaseCommand baseCmd, SubCommandAttribute subCmd, CommandArgs args, string cmdName)
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
                    var isAwaitable = subCmd.Method.ReturnType.GetMethod(nameof(Task.GetAwaiter)) != null;
                    var invokeArg = subCmd.Method.GetParameters().Any() ? new object[] { subArg } : new object[] { };
                    if (isAwaitable)
                    {
                        if (subCmd.Method.ReturnType.IsGenericType)
                        {
                            await (dynamic)(subCmd.Method.Invoke(baseCmd, invokeArg) ?? Task.Delay(1));
                        }
                        else
                        {
                            await (Task)(subCmd.Method.Invoke(baseCmd, invokeArg) ?? Task.Delay(1));
                        }
                    }
                    else
                        subCmd.Method.Invoke(baseCmd, invokeArg);
                }
            }
            catch (Exception ex)
            {
                BLog.Error($"命令执行出错{Environment.NewLine}{ex}");
            }
        }
    }
}
