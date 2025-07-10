using BossFramework.BAttributes;
using BossFramework.BInterfaces;
using BossFramework.BModels;
using CSScriptLib;

namespace BossFramework.BCore.Cmds
{
    public class BossCmd : BaseCommand
    {
        public override string[] Names { get; } = new[] { "boss" };

        [SubCommand("repl", Permission = "boss.admin.repl")]
        public static async void REPL(SubCommandArgs args)
        {
            var code = args.FullCommand.Remove(0, args.SubCommandName.Length + args.CommandName.Length + 2);
            try
            {
                var result = (await CompleteCode(code).REPL(new BEventArgs.BaseEventArgs(args.Player))).ToString();
                if (!string.IsNullOrEmpty(result))
                    args.SendMsg(result);
            }
            catch (Exception ex)
            {
                args.SendErrorMsg($"执行失败\r\n{ex.Message}");
                BLog.Warn($"REPL执行失败\r\n{ex}\r\n{code}");
            }
        }

        public static dynamic CompleteCode(string code)
        {
            var result = @"
using BossFramework;
using BossFramework.BAttributes;
using BossFramework.BInterfaces;
using BossFramework.BModels;
using BossFramework.BCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TerrariaApi.Server;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using TrProtocol;
                                        public class Script
                                        {
                                            public async Task<object> REPL(BossFramework.BModels.BEventArgs.BaseEventArgs args)
                                            {" + code + @"}
                                        }";
            return CSScript.Evaluator.LoadCode(result);
        }

        [SubCommand("ncw", Permission = "boss.admin.changecustomweaponmode")]
        public static void ncw(SubCommandArgs args)
        {
            if (args.Any())
            {
                if (BInfo.OnlinePlayers.FirstOrDefault(p => p.Name.IsSimilarWith(args[0])) is { } plr)
                {
                    bool enable = args.Count() > 1
                        ? args[1].ToLower() is "on" or "true" or "enable"
                        : !plr.IsCustomWeaponMode;
                    plr.ChangeCustomWeaponMode(enable);
                }
                else
                    args.Player.SendErrorMsg($"未找到名为 {args[0]} 的玩家");
            }
            else
                args.Player.SendErrorMsg($"格式错误. /boss ncw <玩家名> (on/off)");
        }
        [NeedPermission("boss.admin.reload")]
        public static void reload(SubCommandArgs args)
        {
            BHooks.HookHandlers.ReloadHandler.OnReload(new(args.TsPlayer));
        }
    }
}
