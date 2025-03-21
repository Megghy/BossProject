﻿using System.Linq;
using BossFramework.BAttributes;
using BossFramework.BInterfaces;
using BossFramework.BModels;
using BossFramework.DB;
using CSScriptLib;

namespace BossFramework.BCore.Cmds
{
    public class PlaceholderCmd : BaseCommand
    {
        public override string[] Names { get; } = new string[] { "placeholder", "ph" };
        [NeedPermission("boss.admin.placeholder.add")]
        public static void Add(SubCommandArgs args)
        {
            if (args.Count() > 1)
            {
                var name = args[0].ToLower();
                if (CommandPlaceholder.Placeholders.Exists(p => p.Name == name))
                {
                    args.Player.SendErrorMsg($"{name} 已存在.");
                    return;
                }
                var code = args.FullCommand[(args.SubCommandName.Length + args.CommandName.Length + args[0].Length + 3)..];
                try
                {
                    var info = new PlaceholderInfo()
                    {
                        Name = name,
                        EvalString = code,
                        ResultDelegate = CSScript.Evaluator.CreateDelegate<string>(@"string placeholder(BossFramework.BModels.BEventArgs.BaseEventArgs args){" + code + "}")
                    };
                    DBTools.Insert(info);
                    CommandPlaceholder.Placeholders.Add(info);
                    args.Player.SendSuccessMsg($"已添加占位符 {name}");
                }
                catch (Exception ex)
                {
                    args.Player.SendErrorMsg($"未能添加占位符.\r\n{ex.Message}");
                }
            }
            else
                args.Player.SendInfoMsg($"格式错误. /ph add <占位符名称> <返回代码>");
        }
        [NeedPermission("boss.admin.placeholder.add")]
        public static void Del(SubCommandArgs args)
        {
            if (args.Any())
            {
                var name = args[0].ToLower();
                if (CommandPlaceholder.Placeholders.FirstOrDefault(p => p.Name == name) is { } info)
                {
                    DBTools.Delete(info);
                    CommandPlaceholder.Placeholders.Remove(info);
                    args.Player.SendErrorMsg($"已移除占位符 {name}");
                }
                else
                    args.Player.SendErrorMsg($"未找到名为 {name} 的占位符");
            }
            else
                args.Player.SendInfoMsg($"格式错误. /ph del <占位符名称>");
        }
        [NeedPermission("boss.admin.placeholder.list")]
        public static void List(SubCommandArgs args)
        {
            args.Player.SendInfoMsg(string.Join("\r\n", CommandPlaceholder.Placeholders.Select(p => $"{{{{{p.Name}}}}} => {p.EvalString}")));
        }
    }
}

