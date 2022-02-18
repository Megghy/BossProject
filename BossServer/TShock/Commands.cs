/*
TShock, a server mod for Terraria
Copyright (C) 2011-2019 Pryaxis & TShock Contributors

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.Localization;
using TShockAPI.DB;
using TShockAPI.Hooks;
using TShockAPI.Localization;

namespace TShockAPI
{
    public delegate void CommandDelegate(CommandArgs args);

    public class CommandArgs : EventArgs
    {
        public string Message { get; private set; }
        public TSPlayer Player { get; private set; }
        public bool Silent { get; private set; }

        /// <summary>
        /// Parameters passed to the argument. Does not include the command name.
        /// IE '/kick "jerk face"' will only have 1 argument
        /// </summary>
        public List<string> Parameters { get; private set; }

        public Player TPlayer
        {
            get { return Player.TPlayer; }
        }

        public CommandArgs(string message, TSPlayer ply, List<string> args)
        {
            Message = message;
            Player = ply;
            Parameters = args;
            Silent = false;
        }

        public CommandArgs(string message, bool silent, TSPlayer ply, List<string> args)
        {
            Message = message;
            Player = ply;
            Parameters = args;
            Silent = silent;
        }
    }

    public class Command
    {
        /// <summary>
        /// Gets or sets whether to allow non-players to use this command.
        /// </summary>
        public bool AllowServer { get; set; }
        /// <summary>
        /// Gets or sets whether to do logging of this command.
        /// </summary>
        public bool DoLog { get; set; }
        /// <summary>
        /// Gets or sets the help text of this command.
        /// </summary>
        public string HelpText { get; set; }
        /// <summary>
        /// Gets or sets an extended description of this command.
        /// </summary>
        public string[] HelpDesc { get; set; }
        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        public string Name { get { return Names[0]; } }
        /// <summary>
        /// Gets the names of the command.
        /// </summary>
        public List<string> Names { get; protected set; }
        /// <summary>
        /// Gets the permissions of the command.
        /// </summary>
        public List<string> Permissions { get; protected set; }

        private CommandDelegate commandDelegate;
        public CommandDelegate CommandDelegate
        {
            get { return commandDelegate; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();

                commandDelegate = value;
            }
        }

        public Command(List<string> permissions, CommandDelegate cmd, params string[] names)
            : this(cmd, names)
        {
            Permissions = permissions;
        }

        public Command(string permissions, CommandDelegate cmd, params string[] names)
            : this(cmd, names)
        {
            Permissions = new List<string> { permissions };
        }

        public Command(CommandDelegate cmd, params string[] names)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");
            if (names == null || names.Length < 1)
                throw new ArgumentException("names");

            AllowServer = true;
            CommandDelegate = cmd;
            DoLog = true;
            HelpText = "No help available.";
            HelpDesc = null;
            Names = new List<string>(names);
            Permissions = new List<string>();
        }

        public bool Run(string msg, bool silent, TSPlayer ply, List<string> parms)
        {
            if (!CanRun(ply))
                return false;

            try
            {
                CommandDelegate(new CommandArgs(msg, silent, ply, parms));
            }
            catch (Exception e)
            {
                ply.SendErrorMessage("指令执行失败，请检查日志以获取更多详细信息");
                TShock.Log.Error(e.ToString());
            }

            return true;
        }

        public bool Run(string msg, TSPlayer ply, List<string> parms)
        {
            return Run(msg, false, ply, parms);
        }

        public bool HasAlias(string name)
        {
            return Names.Contains(name);
        }

        public bool CanRun(TSPlayer ply)
        {
            if (Permissions == null || Permissions.Count < 1)
                return true;
            foreach (var Permission in Permissions)
            {
                if (ply.HasPermission(Permission))
                    return true;
            }
            return false;
        }
    }

    public static class Commands
    {
        public static List<Command> ChatCommands = new List<Command>();
        public static ReadOnlyCollection<Command> TShockCommands = new ReadOnlyCollection<Command>(new List<Command>());

        /// <summary>
        /// The command specifier, defaults to "/"
        /// </summary>
        public static string Specifier
        {
            get { return string.IsNullOrWhiteSpace(TShock.Config.Settings.CommandSpecifier) ? "/" : TShock.Config.Settings.CommandSpecifier; }
        }

        /// <summary>
        /// The silent command specifier, defaults to "."
        /// </summary>
        public static string SilentSpecifier
        {
            get { return string.IsNullOrWhiteSpace(TShock.Config.Settings.CommandSilentSpecifier) ? "." : TShock.Config.Settings.CommandSilentSpecifier; }
        }

        private delegate void AddChatCommand(string permission, CommandDelegate command, params string[] names);

        public static void InitCommands()
        {
            List<Command> tshockCommands = new List<Command>(100);
            Action<Command> add = (cmd) =>
            {
                tshockCommands.Add(cmd);
                ChatCommands.Add(cmd);
            };

            add(new Command(SetupToken, "setup(授权超级管理员)", "setup")
            {
                AllowServer = false,
                HelpText = "首次登录时用于授权超级管理员"
            });
            add(new Command(Permissions.user, ManageUsers, "user(用户)", "user")
            {
                DoLog = false,
                HelpText = "管理用户帐户"
            });

            #region Account Commands
            add(new Command(Permissions.canlogin, AttemptLogin, "login(登录)", "login")
            {
                AllowServer = false,
                DoLog = false,
                HelpText = "登录帐户"
            });
            add(new Command(Permissions.canlogout, Logout, "logout(登出)", "logout")
            {
                AllowServer = false,
                DoLog = false,
                HelpText = "登出账户"
            });
            add(new Command(Permissions.canchangepassword, PasswordUser, "password(修改密码)", "password")
            {
                AllowServer = false,
                DoLog = false,
                HelpText = "更改帐户密码"
            });
            add(new Command(Permissions.canregister, RegisterUser, "register(注册)", "register")
            {
                AllowServer = false,
                DoLog = false,
                HelpText = "注册新帐户"
            });
            add(new Command(Permissions.checkaccountinfo, ViewAccountInfo, "accountinfo(账户信息)", "accountinfo", "ai")
            {
                HelpText = "查看用户的账户信息"
            });
            #endregion
            #region Admin Commands
            add(new Command(Permissions.ban, Ban, "ban(封禁)", "ban")
            {
                HelpText = "管理用户封禁"
            });
            add(new Command(Permissions.broadcast, Broadcast, "broadcast(广播)", "broadcast", "bc", "say")
            {
                HelpText = "向服务器上的所有用户广播消息"
            });
            add(new Command(Permissions.logs, DisplayLogs, "displaylogs(日志设置)", "displaylogs")
            {
                HelpText = "切换是否接收服务器日志"
            });
            add(new Command(Permissions.managegroup, Group, "group(用户组)", "group")
            {
                HelpText = "管理用户组"
            });
            add(new Command(Permissions.manageitem, ItemBan, "itemban(禁用物品)", "itemban")
            {
                HelpText = "管理物品禁令."
            });
            add(new Command(Permissions.manageprojectile, ProjectileBan, "projban(投射物)", "projban")
            {
                HelpText = "管理投射物禁令"
            });
            add(new Command(Permissions.managetile, TileBan, "tileban(禁用物块)", "tileban")
            {
                HelpText = "管理物块禁令"
            });
            add(new Command(Permissions.manageregion, Region, "region(区域)", "region")
            {
                HelpText = "管理区域"
            });
            add(new Command(Permissions.kick, Kick, "kick(驱逐)", "kick")
            {
                HelpText = "驱逐用户"
            });
            add(new Command(Permissions.mute, Mute, "mute(禁言)", "mute", "unmute")
            {
                HelpText = "禁言用户"
            });
            add(new Command(Permissions.savessc, OverrideSSC, "overridessc(覆盖服务端存档)", "overridessc", "ossc")
            {
                HelpText = "临时在服务端覆盖用户存档"
            });
            add(new Command(Permissions.savessc, SaveSSC, "savessc(保存服务端存档)", "savessc")
            {
                HelpText = "保存服务端存档"
            });
            add(new Command(Permissions.uploaddata, UploadJoinData, "uploadssc(上传服务端存档)", "uploadssc")
            {
                HelpText = "加入游戏时上传存档数据作为服务端数据"
            });
            add(new Command(Permissions.settempgroup, TempGroup, "tempgroup(临时用户组)", "tempgroup")
            {
                HelpText = "暂时更改用户组"
            });
            add(new Command(Permissions.su, SubstituteUser, "su(临时超管)", "su")
            {
                HelpText = "暂时提升为超级管理员"
            });
            add(new Command(Permissions.su, SubstituteUserDo, "sudo(以超管身份运行)", "sudo")
            {
                HelpText = "以超级管理员身份执行命令"
            });
            add(new Command(Permissions.userinfo, GrabUserUserInfo, "userinfo(用户信息)", "userinfo", "ui")
            {
                HelpText = "显示用户信息"
            });
            #endregion
            #region Annoy Commands
            add(new Command(Permissions.annoy, Annoy, "annoy(骚扰)", "annoy")
            {
                HelpText = "骚扰用户一段时间"
            });
            add(new Command(Permissions.annoy, Rocket, "rocket(上天)", "rocket")
            {
                HelpText = "让用户飞上天"
            });
            add(new Command(Permissions.annoy, FireWork, "firework(烟火)", "firework")
            {
                HelpText = "向用户发射烟火"
            });
            #endregion
            #region Configuration Commands
            add(new Command(Permissions.maintenance, CheckUpdates, "checkupdates(检查更新)", "checkupdates")
            {
                HelpText = "检查TShock更新"
            });
            add(new Command(Permissions.maintenance, Off, "off(关服)", "off", "exit", "stop")
            {
                HelpText = "关闭服务器且保存数据"
            });
            add(new Command(Permissions.maintenance, OffNoSave, "off-nosave(不保存关服)", "off-nosave", "exit-nosave", "stop-nosave")
            {
                HelpText = "关闭服务器而不保存数据"
            });
            add(new Command(Permissions.cfgreload, Reload, "reload(重载配置)", "reload")
            {
                HelpText = "重新加载服务器配置"
            });
            add(new Command(Permissions.cfgpassword, ServerPassword, "serverpassword(服务密码)", "serverpassword")
            {
                HelpText = "更改服务器登入密码"
            });
            add(new Command(Permissions.maintenance, GetVersion, "version(版本)", "version")
            {
                HelpText = "查看TShock版本"
            });
            add(new Command(Permissions.whitelist, Whitelist, "whitelist(白名单)", "whitelist")
            {
                HelpText = "管理服务器白名单"
            });
            #endregion
            #region Item Commands
            add(new Command(Permissions.give, Give, "give(给予用户物品)", "give", "g")
            {
                HelpText = "给予另一个用户物品"
            });
            add(new Command(Permissions.item, Item, "item(给予自己物品)", "item", "i")
            {
                AllowServer = false,
                HelpText = "给予自己物品"
            });
            #endregion
            #region NPC Commands
            add(new Command(Permissions.butcher, Butcher, "butcher(屠杀)", "butcher")
            {
                HelpText = "屠杀NPCs"
            });
            add(new Command(Permissions.renamenpc, RenameNPC, "renamenpc(重命名NPC)", "renamenpc")
            {
                HelpText = "重命名NPC"
            });
            add(new Command(Permissions.maxspawns, MaxSpawns, "maxspawns")
            {
                HelpText = "设置NPC的最大生成数量"
            });
            add(new Command(Permissions.spawnboss, SpawnBoss, "spawnboss(召唤BOSS)", "spawnboss", "sb")
            {
                AllowServer = false,
                HelpText = "召唤一定数量的BOSS"
            });
            add(new Command(Permissions.spawnmob, SpawnMob, "spawnmob(召唤NPC)", "spawnmob", "sm")
            {
                AllowServer = false,
                HelpText = "在你周围召唤一定数量的NPC"
            });
            add(new Command(Permissions.spawnrate, SpawnRate, "spawnrate(刷NPC率)", "spawnrate")
            {
                HelpText = "设置NPC生成速率"
            });
            add(new Command(Permissions.clearangler, ClearAnglerQuests, "clearangler(重置钓鱼任务)", "clearangler")
            {
                HelpText = "重置当天钓鱼任务"
            });
            #endregion
            #region TP Commands
            add(new Command(Permissions.home, Home, "home(你的出生点)", "home")
            {
                AllowServer = false,
                HelpText = "传送到你的出生点"
            });
            add(new Command(Permissions.spawn, Spawn, "spawn(世界出生点)", "spawn")
            {
                AllowServer = false,
                HelpText = "传送到世界出生点"
            });
            add(new Command(Permissions.tp, TP, "tp(传送用户)", "tp")
            {
                AllowServer = false,
                HelpText = "将一个用户传送到另一个用户"
            });
            add(new Command(Permissions.tpothers, TPHere, "tphere(传送用户到附近)", "tphere")
            {
                AllowServer = false,
                HelpText = "将用户传送到自己的位置"
            });
            add(new Command(Permissions.tpnpc, TPNpc, "tpnpc(传自己到NPC)", "tpnpc")
            {
                AllowServer = false,
                HelpText = "传送到其他的NPC位置"
            });
            add(new Command(Permissions.tppos, TPPos, "tppos(传送到坐标)", "tppos")
            {
                AllowServer = false,
                HelpText = "传送到坐标点"
            });
            add(new Command(Permissions.getpos, GetPos, "pos(获取坐标)", "pos")
            {
                AllowServer = false,
                HelpText = "获取指定用户的坐标"
            });
            add(new Command(Permissions.tpallow, TPAllow, "tpallow(传送保护)", "tpallow")
            {
                AllowServer = false,
                HelpText = "切换是否允许其他人传送自己"
            });
            add(new Command(Permissions.toggleexpert, ChangeWorldMode, "worldmode(世界模式)", "worldmode", "gamemode")
            {
                HelpText = "更改世界模式"
            });
            add(new Command(Permissions.antibuild, ToggleAntiBuild, "antibuild(切换建筑保护)", "antibuild")
            {
                HelpText = "切换世界建筑保护状态"
            });
            add(new Command(Permissions.grow, Grow, "grow(植物生长)", "grow")
            {
                AllowServer = false,
                HelpText = "在附近种植植物"
            });
            add(new Command(Permissions.halloween, ForceHalloween, "forcehalloween(万圣节)", "forcehalloween")
            {
                HelpText = "切换万圣节模式"
            });
            add(new Command(Permissions.xmas, ForceXmas, "forcexmas(圣诞节)", "forcexmas")
            {
                HelpText = "切换圣诞节模式"
            });
            add(new Command(Permissions.manageevents, ManageWorldEvent, "worldevent(世界事件)", "worldevent")
            {
                HelpText = "启用开始和停止各种世界事件"
            });
            add(new Command(Permissions.hardmode, Hardmode, "hardmode(困难模式)", "hardmode")
            {
                HelpText = "切换到困难模式"
            });
            add(new Command(Permissions.editspawn, ProtectSpawn, "protectspawn(切换出生点保护)", "protectspawn")
            {
                HelpText = "切换出生点保护"
            });
            add(new Command(Permissions.worldsave, Save, "save(保存)", "save")
            {
                HelpText = "保存地图"
            });
            add(new Command(Permissions.worldspawn, SetSpawn, "setspawn(设置世界出生点)", "setspawn")
            {
                AllowServer = false,
                HelpText = "将世界的生成点设置为你的位置"
            });
            add(new Command(Permissions.dungeonposition, SetDungeon, "setdungeon(设置地牢位置)", "setdungeon")
            {
                AllowServer = false,
                HelpText = "将地牢的位置设置为你的位置"
            });
            add(new Command(Permissions.worldsettle, Settle, "settle(平衡液体)", "settle")
            {
                HelpText = "强制液体平衡"
            });
            add(new Command(Permissions.time, Time, "time(时间)", "time")
            {
                HelpText = "设置世界时间"
            });
            add(new Command(Permissions.wind, Wind, "wind(风速)", "wind")
            {
                HelpText = "改变风速"
            });
            add(new Command(Permissions.worldinfo, WorldInfo, "worldinfo(世界信息)", "worldinfo")
            {
                HelpText = "显示有关当前世界的信息"
            });
            #endregion
            #region Other Commands
            add(new Command(Permissions.buff, Buff, "buff(给予自己增益)", "buff")
            {
                AllowServer = false,
                HelpText = "给自己一个增益或debuff一段时间。用-1表示时间将会设置为415天。"
            });
            add(new Command(Permissions.clear, Clear, "clear(清除掉落物)", "clear")
            {
                HelpText = "清除物品掉落"
            });
            add(new Command(Permissions.buffplayer, GBuff, "gbuff(给予其他用户增益)", "gbuff", "buffplayer")
            {
                HelpText = "给予其他用户增益。用-1表示时间将会设置为415天。"
            });
            add(new Command(Permissions.godmode, ToggleGodMode, "godmode(上帝模式)", "godmode")
            {
                HelpText = "切换上帝模式"
            });
            add(new Command(Permissions.heal, Heal, "heal(治愈)", "heal")
            {
                HelpText = "治疗用户(HP和MP)"
            });
            add(new Command(Permissions.kill, Kill, "kill(杀死)", "kill")
            {
                HelpText = "杀死一个用户"
            });
            add(new Command(Permissions.cantalkinthird, ThirdPerson, "me(我)", "me")
            {
                HelpText = "向所有人发送操作消息"
            });
            add(new Command(Permissions.canpartychat, PartyChat, "party(团队消息)", "party", "p")
            {
                AllowServer = false,
                HelpText = "向团队中的每个人发送消息"
            });
            add(new Command(Permissions.whisper, Reply, "reply(回复PM)", "reply", "r")
            {
                HelpText = "回复发送给你的PM"
            });
            add(new Command("tshock.rest.manage", ManageRest, "rest(REST设置)", "rest")
            {
                HelpText = "管理REST API"
            });
            add(new Command(Permissions.slap, Slap, "slap(击打)", "slap")
            {
                HelpText = "击打用户，造成伤害"
            });
            add(new Command(Permissions.serverinfo, ServerInfo, "serverinfo(服务器信息)", "serverinfo")
            {
                HelpText = "显示服务器信息"
            });
            add(new Command(Permissions.warp, Warp, "warp(传送点)", "warp")
            {
                HelpText = "将你传送到坐标或管理传送坐标"
            });
            add(new Command(Permissions.whisper, Whisper, "whisper(发送PM)", "whisper", "w", "tell")
            {
                HelpText = "将PM发送给用户"
            });
            add(new Command(Permissions.createdumps, CreateDumps, "dump-reference-data(创建参考数据)", "dump-reference-data")
            {
                HelpText = "在服务器文件夹中为Terraria数据类型和TShock权限系统创建参考表"
            });
            add(new Command(Permissions.synclocalarea, SyncLocalArea, "sync(同步)", "sync")
            {
                HelpText = "将所有物块从服务器发送到用户，以使客户端与实际世界状态重新同步"
            });
            add(new Command(Permissions.respawn, Respawn, "respawn")
            {
                HelpText = "重生自己或其他玩家"
            });
            #endregion

            add(new Command(Aliases, "aliases(命令别名)", "aliases")
            {
                HelpText = "显示命令的别名"
            });
            add(new Command(Help, "help(帮助)", "help")
            {
                HelpText = "列出命令或提供帮助"
            });
            add(new Command(Motd, "motd(公告)", "motd")
            {
                HelpText = "显示公告"
            });
            add(new Command(ListConnectedPlayers, "playing(用户)", "playing", "online", "who")
            {
                HelpText = "显示当前连接的用户"
            });
            add(new Command(Rules, "rules(规则)", "rules")
            {
                HelpText = "显示服务器的规则"
            });

            TShockCommands = new ReadOnlyCollection<Command>(tshockCommands);
        }

        public static bool HandleCommand(TSPlayer player, string text)
        {
            string cmdText = text.Remove(0, 1);
            string cmdPrefix = text[0].ToString();
            bool silent = false;

            if (cmdPrefix == SilentSpecifier)
                silent = true;

            int index = -1;
            for (int i = 0; i < cmdText.Length; i++)
            {
                if (IsWhiteSpace(cmdText[i]))
                {
                    index = i;
                    break;
                }
            }
            string cmdName;
            if (index == 0) // Space after the command specifier should not be supported
            {
                player.SendErrorMessage("输入的命令无效，输入{0}help以获取有效命令列表", Specifier);
                return true;
            }
            else if (index < 0)
                cmdName = cmdText.ToLower();
            else
                cmdName = cmdText.Substring(0, index).ToLower();

            List<string> args;
            if (index < 0)
                args = new List<string>();
            else
                args = ParseParameters(cmdText.Substring(index));

            IEnumerable<Command> cmds = ChatCommands.FindAll(c => c.HasAlias(cmdName));

            if (PlayerHooks.OnPlayerCommand(player, cmdName, ref cmdText, args, ref cmds, cmdPrefix))
                return true;

            if (cmds.Any())
            {
                if (player.AwaitingResponse.ContainsKey(cmdName))
                {
                    Action<CommandArgs> call = player.AwaitingResponse[cmdName];
                    player.AwaitingResponse.Remove(cmdName);
                    call(new CommandArgs(cmdText, player, args));
                    return true;
                }
            }
            else
            {
                player.SendErrorMessage("输入的命令无效，输入{0}help以获取有效命令列表", Specifier);
                return true;
            }
            foreach (Command cmd in cmds)
            {
                if (!cmd.CanRun(player))
                {
                    TShock.Utils.SendLogs(string.Format("{0} 试图执行 {1}{2}.", player.Name, Specifier, cmdText), Color.PaleVioletRed, player);
                    player.SendErrorMessage("你无权访问此命令");
                    if (player.HasPermission(Permissions.su))
                    {
                        player.SendInfoMessage("你可以使用 '{0}sudo {0}{1}' 覆盖此检查", Specifier, cmdText);
                    }
                }
                else if (!cmd.AllowServer && !player.RealPlayer)
                {
                    player.SendErrorMessage("你必须在游戏中使用此命令");
                }
                else
                {
                    if (cmd.DoLog)
                        TShock.Utils.SendLogs(string.Format("{0} 执行了: {1}{2}.", player.Name, silent ? SilentSpecifier : Specifier, cmdText), Color.PaleVioletRed, player);
                    cmd.Run(cmdText, silent, player, args);
                }
            }
            return true;
        }

        /// <summary>
        /// Parses a string of parameters into a list. Handles quotes.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static List<String> ParseParameters(string str)
        {
            var ret = new List<string>();
            var sb = new StringBuilder();
            bool instr = false;
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];

                if (c == '\\' && ++i < str.Length)
                {
                    if (str[i] != '"' && str[i] != ' ' && str[i] != '\\')
                        sb.Append('\\');
                    sb.Append(str[i]);
                }
                else if (c == '"')
                {
                    instr = !instr;
                    if (!instr)
                    {
                        ret.Add(sb.ToString());
                        sb.Clear();
                    }
                    else if (sb.Length > 0)
                    {
                        ret.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                else if (IsWhiteSpace(c) && !instr)
                {
                    if (sb.Length > 0)
                    {
                        ret.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                else
                    sb.Append(c);
            }
            if (sb.Length > 0)
                ret.Add(sb.ToString());

            return ret;
        }

        private static bool IsWhiteSpace(char c)
        {
            return c == ' ' || c == '\t' || c == '\n';
        }

        #region Account commands

        private static void AttemptLogin(CommandArgs args)
        {
            if (args.Player.LoginAttempts > TShock.Config.Settings.MaximumLoginAttempts && (TShock.Config.Settings.MaximumLoginAttempts != -1))
            {
                TShock.Log.Warn(String.Format("{0} ({1}) had {2} or more invalid login attempts and was kicked automatically.",
                    args.Player.IP, args.Player.Name, TShock.Config.Settings.MaximumLoginAttempts));
                args.Player.Kick("过多的无效登录");
                return;
            }

            if (args.Player.IsLoggedIn)
            {
                args.Player.SendErrorMessage("你已经登录，无需再次登录");
                return;
            }

            UserAccount account = TShock.UserAccounts.GetUserAccountByName(args.Player.Name);
            string password = "";
            bool usingUUID = false;
            if (args.Parameters.Count == 0 && !TShock.Config.Settings.DisableUUIDLogin)
            {
                if (PlayerHooks.OnPlayerPreLogin(args.Player, args.Player.Name, ""))
                    return;
                usingUUID = true;
            }
            else if (args.Parameters.Count == 1)
            {
                if (PlayerHooks.OnPlayerPreLogin(args.Player, args.Player.Name, args.Parameters[0]))
                    return;
                password = args.Parameters[0];
            }
            else if (args.Parameters.Count == 2 && TShock.Config.Settings.AllowLoginAnyUsername)
            {
                if (String.IsNullOrEmpty(args.Parameters[0]))
                {
                    args.Player.SendErrorMessage("Bad login attempt.");
                    return;
                }

                if (PlayerHooks.OnPlayerPreLogin(args.Player, args.Parameters[0], args.Parameters[1]))
                    return;

                account = TShock.UserAccounts.GetUserAccountByName(args.Parameters[0]);
                password = args.Parameters[1];
            }
            else
            {
                if (!TShock.Config.Settings.DisableUUIDLogin)
                    args.Player.SendMessage($"{Specifier}login - 使用你的UUID和人物名进行登录.", Color.White);

                if (TShock.Config.Settings.AllowLoginAnyUsername)
                    args.Player.SendMessage($"{Specifier}login {"username".Color(Utils.GreenHighlight)} {"password".Color(Utils.BoldHighlight)} - 使用用户名和密码登录", Color.White);
                else
                    args.Player.SendMessage($"{Specifier}login {"password".Color(Utils.BoldHighlight)} - 使用角色名和密码登录", Color.White);

                args.Player.SendWarningMessage("如果你忘记了密码，将无法恢复");
                return;
            }
            try
            {
                if (account == null)
                {
                    args.Player.SendErrorMessage("A user account by that name does not exist.");
                }
                else if (account.VerifyPassword(password) ||
                        (usingUUID && account.UUID == args.Player.UUID && !TShock.Config.Settings.DisableUUIDLogin &&
                        !String.IsNullOrWhiteSpace(args.Player.UUID)))
                {
                    var group = TShock.Groups.GetGroupByName(account.Group);

                    if (!TShock.Groups.AssertGroupValid(args.Player, group, false))
                    {
                        args.Player.SendErrorMessage("Login attempt failed - see the message above.");
                        return;
                    }

                    args.Player.PlayerData = TShock.CharacterDB.GetPlayerData(args.Player, account.ID);

                    args.Player.Group = group;
                    args.Player.tempGroup = null;
                    args.Player.Account = account;
                    args.Player.IsLoggedIn = true;
                    args.Player.IsDisabledForSSC = false;

                    if (Main.ServerSideCharacter)
                    {
                        if (args.Player.HasPermission(Permissions.bypassssc))
                        {
                            args.Player.PlayerData.CopyCharacter(args.Player);
                            TShock.CharacterDB.InsertPlayerData(args.Player);
                        }
                        args.Player.PlayerData.RestoreCharacter(args.Player);
                    }
                    args.Player.LoginFailsBySsi = false;

                    if (args.Player.HasPermission(Permissions.ignorestackhackdetection))
                        args.Player.IsDisabledForStackDetection = false;

                    if (args.Player.HasPermission(Permissions.usebanneditem))
                        args.Player.IsDisabledForBannedWearable = false;

                    args.Player.SendSuccessMessage("Authenticated as " + account.Name + " successfully.");

                    TShock.Log.ConsoleInfo(args.Player.Name + " 成功认证账户: " + account.Name + ".");
                    if ((args.Player.LoginHarassed) && (TShock.Config.Settings.RememberLeavePos))
                    {
                        if (TShock.RememberedPos.GetLeavePos(args.Player.Name, args.Player.IP) != Vector2.Zero)
                        {
                            Vector2 pos = TShock.RememberedPos.GetLeavePos(args.Player.Name, args.Player.IP);
                            args.Player.Teleport((int)pos.X * 16, (int)pos.Y * 16);
                        }
                        args.Player.LoginHarassed = false;

                    }
                    TShock.UserAccounts.SetUserAccountUUID(account, args.Player.UUID);

                    Hooks.PlayerHooks.OnPlayerPostLogin(args.Player);
                }
                else
                {
                    if (usingUUID && !TShock.Config.Settings.DisableUUIDLogin)
                    {
                        args.Player.SendErrorMessage("UUID 不匹配!");
                    }
                    else
                    {
                        args.Player.SendErrorMessage("无效的密码!");
                    }
                    TShock.Log.Warn(args.Player.IP + " 未能成功认证: " + account.Name + ".");
                    args.Player.LoginAttempts++;
                }
            }
            catch (Exception ex)
            {
                args.Player.SendErrorMessage("处理请求时出错");
                TShock.Log.Error(ex.ToString());
            }
        }

        private static void Logout(CommandArgs args)
        {
            if (!args.Player.IsLoggedIn)
            {
                args.Player.SendErrorMessage("你还没有登录！");
                return;
            }

            args.Player.Logout();
            args.Player.SendSuccessMessage("你已成功登出帐户！");
            if (Main.ServerSideCharacter)
            {
                args.Player.SendWarningMessage("服务器端记录已启用，你需要登录才能使用。");
            }
        }

        private static void PasswordUser(CommandArgs args)
        {
            try
            {
                if (args.Player.IsLoggedIn && args.Parameters.Count == 2)
                {
                    string password = args.Parameters[0];
                    if (args.Player.Account.VerifyPassword(password))
                    {
                        try
                        {
                            args.Player.SendSuccessMessage("你更改了密码!");
                            TShock.UserAccounts.SetUserAccountPassword(args.Player.Account, args.Parameters[1]); // SetUserPassword will hash it for you.
                            TShock.Log.ConsoleInfo(args.Player.IP + " named " + args.Player.Name + " changed the password of account " +
                                                   args.Player.Account.Name + ".");
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            args.Player.SendErrorMessage("密码必须大于或等于 " + TShock.Config.Settings.MinimumPasswordLength + " 字符");
                        }
                    }
                    else
                    {
                        args.Player.SendErrorMessage("修改密码失败!");
                        TShock.Log.ConsoleError(args.Player.IP + " named " + args.Player.Name + " failed to change password for account: " +
                                                args.Player.Account.Name + ".");
                    }
                }
                else
                {
                    args.Player.SendErrorMessage("未登录或语法无效！正确的语法: {0}password <旧密码> <新密码>", Specifier);
                }
            }
            catch (UserAccountManagerException ex)
            {
                args.Player.SendErrorMessage("抱歉，发生错误: " + ex.Message + ".");
                TShock.Log.ConsoleError("PasswordUser returned an error: " + ex);
            }
        }

        private static void RegisterUser(CommandArgs args)
        {
            try
            {
                var account = new UserAccount();
                string echoPassword = "";
                if (args.Parameters.Count == 1)
                {
                    account.Name = args.Player.Name;
                    echoPassword = args.Parameters[0];
                    try
                    {
                        account.CreateBCryptHash(args.Parameters[0]);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        args.Player.SendErrorMessage("密码必须大于或等于 " + TShock.Config.Settings.MinimumPasswordLength + " 字符");
                        return;
                    }
                }
                else if (args.Parameters.Count == 2 && TShock.Config.Settings.AllowRegisterAnyUsername)
                {
                    account.Name = args.Parameters[0];
                    echoPassword = args.Parameters[1];
                    try
                    {
                        account.CreateBCryptHash(args.Parameters[1]);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        args.Player.SendErrorMessage("密码必须大于或等于 " + TShock.Config.Settings.MinimumPasswordLength + " 字符");
                        return;
                    }
                }
                else
                {
                    args.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}register <password>", Specifier);
                    return;
                }

                account.Group = TShock.Config.Settings.DefaultRegistrationGroupName; // FIXME -- we should get this from the DB. --Why?
                account.UUID = args.Player.UUID;

                if (TShock.UserAccounts.GetUserAccountByName(account.Name) == null && account.Name != TSServerPlayer.AccountName) // Cheap way of checking for existance of a user
                {
                    args.Player.SendSuccessMessage("帐户 \"{0}\" 注册成功！", account.Name);
                    args.Player.SendSuccessMessage("您的密码是： {0}.", echoPassword);

                    if (!TShock.Config.Settings.DisableUUIDLogin)
                        args.Player.SendMessage($"Type {Specifier}login to sign in to your account using your UUID.", Color.White);

                    if (TShock.Config.Settings.AllowLoginAnyUsername)
                        args.Player.SendMessage($"Type {Specifier}login \"{account.Name.Color(Utils.GreenHighlight)}\" {echoPassword.Color(Utils.BoldHighlight)} to sign in to your account.", Color.White);
                    else
                        args.Player.SendMessage($"Type {Specifier}login {echoPassword.Color(Utils.BoldHighlight)} to sign in to your account.", Color.White);

                    TShock.UserAccounts.AddUserAccount(account);
                    TShock.Log.ConsoleInfo("{0} 注册了一个帐户: \"{1}\".", args.Player.Name, account.Name);
                }
                else
                {
                    args.Player.SendErrorMessage("抱歉, " + account.Name + " 已经被注册！");
                    args.Player.SendErrorMessage("请尝试使用其他用户名。");
                    TShock.Log.ConsoleInfo(args.Player.Name + " 无法注册现有帐户: " + account.Name);
                }
            }
            catch (UserAccountManagerException ex)
            {
                args.Player.SendErrorMessage("抱歉，发生错误: " + ex.Message + ".");
                TShock.Log.ConsoleError("RegisterUser returned an error: " + ex);
            }
        }

        private static void ManageUsers(CommandArgs args)
        {
            // This guy needs to be here so that people don't get exceptions when they type /user
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("无效的用户语法, 尝试 {0}user help.", Specifier);
                return;
            }

            string subcmd = args.Parameters[0];

            // Add requires a username, password, and a group specified.
            if (subcmd == "add" && args.Parameters.Count == 4)
            {
                var account = new UserAccount();

                account.Name = args.Parameters[1];
                try
                {
                    account.CreateBCryptHash(args.Parameters[2]);
                }
                catch (ArgumentOutOfRangeException)
                {
                    args.Player.SendErrorMessage("密码必须大于或等于 " + TShock.Config.Settings.MinimumPasswordLength + " 字符。");
                    return;
                }
                account.Group = args.Parameters[3];

                try
                {
                    TShock.UserAccounts.AddUserAccount(account);
                    args.Player.SendSuccessMessage("账户 " + account.Name + " 已被添加到用户组 " + account.Group + "!");
                    TShock.Log.ConsoleInfo(args.Player.Name + " added Account " + account.Name + " to group " + account.Group);
                }
                catch (GroupNotExistsException)
                {
                    args.Player.SendErrorMessage("组 " + account.Group + " 不存在!");
                }
                catch (UserAccountExistsException)
                {
                    args.Player.SendErrorMessage("用户 " + account.Name + " 已经在用户组内!");
                }
                catch (UserAccountManagerException e)
                {
                    args.Player.SendErrorMessage("用户 " + account.Name + " 无法添加，请查看控制台以获取详细信息。");
                    TShock.Log.ConsoleError(e.ToString());
                }
            }
            // User deletion requires a username
            else if (subcmd == "del" && args.Parameters.Count == 2)
            {
                var account = new UserAccount();
                account.Name = args.Parameters[1];

                try
                {
                    TShock.UserAccounts.RemoveUserAccount(account);
                    args.Player.SendSuccessMessage("帐户已被成功删除。");
                    TShock.Log.ConsoleInfo(args.Player.Name + " successfully deleted account: " + args.Parameters[1] + ".");
                }
                catch (UserAccountNotExistException)
                {
                    args.Player.SendErrorMessage("用户 " + account.Name + " 不存在! 删除了个寂寞!");
                }
                catch (UserAccountManagerException ex)
                {
                    args.Player.SendErrorMessage(ex.Message);
                    TShock.Log.ConsoleError(ex.ToString());
                }
            }

            // Password changing requires a username, and a new password to set
            else if (subcmd == "password" && args.Parameters.Count == 3)
            {
                var account = new UserAccount();
                account.Name = args.Parameters[1];

                try
                {
                    TShock.UserAccounts.SetUserAccountPassword(account, args.Parameters[2]);
                    TShock.Log.ConsoleInfo(args.Player.Name + " 修改了帐号密码 " + account.Name);
                    args.Player.SendSuccessMessage("Password change succeeded for " + account.Name + ".");
                }
                catch (UserAccountNotExistException)
                {
                    args.Player.SendErrorMessage("用户 " + account.Name + " 不存在!");
                }
                catch (UserAccountManagerException e)
                {
                    args.Player.SendErrorMessage("密码更改为 " + account.Name + " 失败！检查控制台！");
                    TShock.Log.ConsoleError(e.ToString());
                }
                catch (ArgumentOutOfRangeException)
                {
                    args.Player.SendErrorMessage("密码必须大于或等于 " + TShock.Config.Settings.MinimumPasswordLength + " 字符。");
                }
            }
            // Group changing requires a username or IP address, and a new group to set
            else if (subcmd == "group" && args.Parameters.Count == 3)
            {
                var account = new UserAccount();
                account.Name = args.Parameters[1];

                try
                {
                    TShock.UserAccounts.SetUserGroup(account, args.Parameters[2]);
                    TShock.Log.ConsoleInfo(args.Player.Name + " 已更改帐户 " + account.Name + " 到用户组 " + args.Parameters[2] + ".");
                    args.Player.SendSuccessMessage("账户 " + account.Name + " 用户组已更改为 " + args.Parameters[2] + "!");

                    //send message to player with matching account name
                    var player = TShock.Players.FirstOrDefault(p => p != null && p.Account?.Name == account.Name);
                    if (player != null && !args.Silent)
                        player.SendSuccessMessage($"{args.Player.Name} 已经将你的组更改到 {args.Parameters[2]}");
                }
                catch (GroupNotExistsException)
                {
                    args.Player.SendErrorMessage("用户组不存在！");
                }
                catch (UserAccountNotExistException)
                {
                    args.Player.SendErrorMessage("用户 " + account.Name + " 不存在!");
                }
                catch (UserAccountManagerException e)
                {
                    args.Player.SendErrorMessage("用户 " + account.Name + " 无法添加，请查看控制台以获取详细信息。");
                    TShock.Log.ConsoleError(e.ToString());
                }
            }
            else if (subcmd == "help")
            {
                args.Player.SendInfoMessage("使用命令帮助:");
                args.Player.SendInfoMessage("{0}user add <用户名> <密码> <用户组>   - 添加指定的用户", Specifier);
                args.Player.SendInfoMessage("{0}user del <用户名> - 删除指定的用户", Specifier);
                args.Player.SendInfoMessage("{0}user password <用户名> <新密码> - 更改用户密码", Specifier);
                args.Player.SendInfoMessage("{0}user group <用户名> <用户组> - 更改所在用户组", Specifier);
            }
            else
            {
                args.Player.SendErrorMessage("无效的用户语法, 尝试 {0}user help.", Specifier);
            }
        }

        #endregion

        #region Stupid commands

        private static void ServerInfo(CommandArgs args)
        {
            args.Player.SendInfoMessage("内存使用情况: " + Process.GetCurrentProcess().WorkingSet64);
            args.Player.SendInfoMessage("分配的内存: " + Process.GetCurrentProcess().VirtualMemorySize64);
            args.Player.SendInfoMessage("总处理器时间: " + Process.GetCurrentProcess().TotalProcessorTime);
            args.Player.SendInfoMessage("Win版本: " + Environment.OSVersion);
            args.Player.SendInfoMessage("进程数: " + Environment.ProcessorCount);
            args.Player.SendInfoMessage("机器名称: " + Environment.MachineName);
        }

        private static void WorldInfo(CommandArgs args)
        {
            args.Player.SendInfoMessage("当前运行的世界的信息");
            args.Player.SendInfoMessage("名称: " + (TShock.Config.Settings.UseServerName ? TShock.Config.Settings.ServerName : Main.worldName));
            args.Player.SendInfoMessage("尺寸: {0}x{1}", Main.maxTilesX, Main.maxTilesY);
            args.Player.SendInfoMessage("ID: " + Main.worldID);
            args.Player.SendInfoMessage("种子: " + WorldGen.currentWorldSeed);
            args.Player.SendInfoMessage("模组: " + Main.GameMode);
            args.Player.SendInfoMessage("路径: " + Main.worldPathName);
        }

        #endregion

        #region Player Management Commands

        private static void GrabUserUserInfo(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("无效的语法!正确的语法: {0}userinfo <用户名>", Specifier);
                return;
            }

            var players = TSPlayer.FindByNameOrID(args.Parameters[0]);
            if (players.Count < 1)
                args.Player.SendErrorMessage("无效的用户。");
            else if (players.Count > 1)
                args.Player.SendMultipleMatchError(players.Select(p => p.Name));
            else
            {
                var message = new StringBuilder();
                message.Append("IP 地址: ").Append(players[0].IP);
                if (players[0].Account != null && players[0].IsLoggedIn)
                    message.Append(" | 登录为: ").Append(players[0].Account.Name).Append(" | 用户组: ").Append(players[0].Group.Name);
                args.Player.SendSuccessMessage(message.ToString());
            }
        }

        private static void ViewAccountInfo(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("无效的语法!正确的语法: {0}accountinfo <用户名>", Specifier);
                return;
            }

            string username = String.Join(" ", args.Parameters);
            if (!string.IsNullOrWhiteSpace(username))
            {
                var account = TShock.UserAccounts.GetUserAccountByName(username);
                if (account != null)
                {

                    string Timezone = TimeZoneInfo.GetSystemTimeZones().FirstOrDefault()?.GetUtcOffset(DateTime.Now).Hours.ToString("+#;-#");

                    if (DateTime.TryParse(account.LastAccessed, out DateTime LastSeen))
                    {
                        LastSeen = DateTime.Parse(account.LastAccessed).ToLocalTime();
                        args.Player.SendSuccessMessage("{0} 的最近一次登录时间 {1} {2} UTC{3}.", account.Name, LastSeen.ToShortDateString(),
                            LastSeen.ToShortTimeString(), Timezone);
                    }

                    if (args.Player.Group.HasPermission(Permissions.advaccountinfo))
                    {
                        List<string> KnownIps = JsonConvert.DeserializeObject<List<string>>(account.KnownIps?.ToString() ?? string.Empty);
                        string ip = KnownIps?[KnownIps.Count - 1] ?? "N/A";
                        DateTime Registered = DateTime.Parse(account.Registered).ToLocalTime();

                        args.Player.SendSuccessMessage("{0} 的用户组是： {1}.", account.Name, account.Group);
                        args.Player.SendSuccessMessage("{0} 的最近已知IP是： {1}.", account.Name, ip);
                        args.Player.SendSuccessMessage("{0} 的注册日期是：{1} {2} UTC{3}.", account.Name, Registered.ToShortDateString(), Registered.ToShortTimeString(), Timezone);
                    }
                }
                else
                    args.Player.SendErrorMessage("用户 {0} 不存在。", username);
            }
            else args.Player.SendErrorMessage("无效的语法!正确的语法: {0}accountinfo <用户名>", Specifier);
        }

        private static void Kick(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("无效的语法!正确的语法: {0} 踢出 <用户名> [原因]", Specifier);
                return;
            }
            if (args.Parameters[0].Length == 0)
            {
                args.Player.SendErrorMessage("缺少用户名称。");
                return;
            }

            string plStr = args.Parameters[0];
            var players = TSPlayer.FindByNameOrID(plStr);
            if (players.Count == 0)
            {
                args.Player.SendErrorMessage("无效用户!");
            }
            else if (players.Count > 1)
            {
                args.Player.SendMultipleMatchError(players.Select(p => p.Name));
            }
            else
            {
                string reason = args.Parameters.Count > 1
                                    ? String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1))
                                    : "Misbehaviour.";
                if (!players[0].Kick(reason, !args.Player.RealPlayer, false, args.Player.Name))
                {
                    args.Player.SendErrorMessage("你不能驱逐管理员!");
                }
            }
        }

        private static void Ban(CommandArgs args)
        {
            //Ban syntax:
            // ban add <target> [reason] [duration] [flags (default: -a -u -ip)]
            //						Duration is in the format 0d0h0m0s. Any part can be ignored. E.g., 1s is a valid ban time, as is 1d1s, etc. If no duration is specified, ban is permanent
            //						Valid flags: -a (ban account name), -u (ban UUID), -n (ban character name), -ip (ban IP address), -e (exact, ban the identifier provided as 'target')
            //						Unless -e is passed to the command, <target> is assumed to be a player or player index.
            // ban del <ban ID>
            //						Target is expected to be a ban Unique ID
            // ban list [page]
            //						Displays a paginated list of bans
            // ban details <ban ID>
            //						Target is expected to be a ban Unique ID
            //ban help [command]
            //						Provides extended help on specific ban commands

            void Help()
            {
                if (args.Parameters.Count > 1)
                {
                    MoreHelp(args.Parameters[1].ToLower());
                    return;
                }

                args.Player.SendMessage("服务器封禁帮助", Color.White);
                args.Player.SendMessage("可用的封禁指令:", Color.White);
                args.Player.SendMessage($"ban {"add".Color(Utils.RedHighlight)} <Target> [Flags]", Color.White);
                args.Player.SendMessage($"ban {"del".Color(Utils.RedHighlight)} <Ban ID>", Color.White);
                args.Player.SendMessage($"ban {"list".Color(Utils.RedHighlight)}", Color.White);
                args.Player.SendMessage($"ban {"details".Color(Utils.RedHighlight)} <Ban ID>", Color.White);
                args.Player.SendMessage($"Quick usage: {"ban add".Color(Utils.BoldHighlight)} {args.Player.Name.Color(Utils.RedHighlight)} \"Griefing\"", Color.White);
                args.Player.SendMessage($"For more info, use {"ban help".Color(Utils.BoldHighlight)} {"command".Color(Utils.RedHighlight)} or {"ban help".Color(Utils.BoldHighlight)} {"examples".Color(Utils.RedHighlight)}", Color.White);
            }

            void MoreHelp(string cmd)
            {
                switch (cmd)
                {
                    case "add":
                        args.Player.SendMessage("", Color.White);
                        args.Player.SendMessage("Ban Add Syntax", Color.White);
                        args.Player.SendMessage($"{"ban add".Color(Utils.BoldHighlight)} <{"Target".Color(Utils.RedHighlight)}> [{"Reason".Color(Utils.BoldHighlight)}] [{"Duration".Color(Utils.PinkHighlight)}] [{"Flags".Color(Utils.GreenHighlight)}]", Color.White);
                        args.Player.SendMessage($"- {"Duration".Color(Utils.PinkHighlight)}: uses the format {"0d0m0s".Color(Utils.PinkHighlight)} to determine the length of the ban.", Color.White);
                        args.Player.SendMessage($"   Eg a value of {"10d30m0s".Color(Utils.PinkHighlight)} would represent 10 days, 30 minutes, 0 seconds.", Color.White);
                        args.Player.SendMessage($"   If no duration is provided, the ban will be permanent.", Color.White);
                        args.Player.SendMessage($"- {"Flags".Color(Utils.GreenHighlight)}: -a (account name), -u (UUID), -n (character name), -ip (IP address), -e (exact, {"Target".Color(Utils.RedHighlight)} will be treated as identifier)", Color.White);
                        args.Player.SendMessage($"   Unless {"-e".Color(Utils.GreenHighlight)} is passed to the command, {"Target".Color(Utils.RedHighlight)} is assumed to be a player or player index", Color.White);
                        args.Player.SendMessage($"   If no {"Flags".Color(Utils.GreenHighlight)} are specified, the command uses {"-a -u -ip".Color(Utils.GreenHighlight)} by default.", Color.White);
                        args.Player.SendMessage($"Example usage: {"ban add".Color(Utils.BoldHighlight)} {args.Player.Name.Color(Utils.RedHighlight)} {"\"Cheating\"".Color(Utils.BoldHighlight)} {"10d30m0s".Color(Utils.PinkHighlight)} {"-a -u -ip".Color(Utils.GreenHighlight)}", Color.White);
                        break;

                    case "del":
                        args.Player.SendMessage("", Color.White);
                        args.Player.SendMessage("Ban Del Syntax", Color.White);
                        args.Player.SendMessage($"{"ban del".Color(Utils.BoldHighlight)} <{"Ticket Number".Color(Utils.RedHighlight)}>", Color.White);
                        args.Player.SendMessage($"- {"Ticket Numbers".Color(Utils.RedHighlight)} are provided when you add a ban, and can also be viewed with the {"ban list".Color(Utils.BoldHighlight)} command.", Color.White);
                        args.Player.SendMessage($"Example usage: {"ban del".Color(Utils.BoldHighlight)} {"12345".Color(Utils.RedHighlight)}", Color.White);
                        break;

                    case "list":
                        args.Player.SendMessage("", Color.White);
                        args.Player.SendMessage("Ban List Syntax", Color.White);
                        args.Player.SendMessage($"{"ban list".Color(Utils.BoldHighlight)} [{"Page".Color(Utils.PinkHighlight)}]", Color.White);
                        args.Player.SendMessage("- Lists active bans. Color trends towards green as the ban approaches expiration", Color.White);
                        args.Player.SendMessage($"Example usage: {"ban list".Color(Utils.BoldHighlight)}", Color.White);
                        break;

                    case "details":
                        args.Player.SendMessage("", Color.White);
                        args.Player.SendMessage("Ban Details Syntax", Color.White);
                        args.Player.SendMessage($"{"ban details".Color(Utils.BoldHighlight)} <{"Ticket Number".Color(Utils.RedHighlight)}>", Color.White);
                        args.Player.SendMessage($"- {"Ticket Numbers".Color(Utils.RedHighlight)} are provided when you add a ban, and can be found with the {"ban list".Color(Utils.BoldHighlight)} command.", Color.White);
                        args.Player.SendMessage($"Example usage: {"ban details".Color(Utils.BoldHighlight)} {"12345".Color(Utils.RedHighlight)}", Color.White);
                        break;

                    case "identifiers":
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, 2, args.Player, out int pageNumber))
                        {
                            args.Player.SendMessage($"Invalid page number. Page number must be numeric.", Color.White);
                            return;
                        }

                        var idents = from ident in Identifier.Available
                                     select $"{ident.Color(Utils.RedHighlight)} - {ident.Description}";

                        args.Player.SendMessage("", Color.White);
                        PaginationTools.SendPage(args.Player, pageNumber, idents.ToList(),
                            new PaginationTools.Settings
                            {
                                HeaderFormat = "Available identifiers ({0}/{1}):",
                                FooterFormat = "Type {0}ban help identifiers {{0}} for more.".SFormat(Specifier),
                                NothingToDisplayString = "There are currently no available identifiers.",
                                HeaderTextColor = Color.White,
                                LineTextColor = Color.White
                            });
                        break;

                    case "examples":
                        args.Player.SendMessage("", Color.White);
                        args.Player.SendMessage("Ban Usage Examples", Color.White);
                        args.Player.SendMessage("- Ban an offline player by account name", Color.White);
                        args.Player.SendMessage($"   {Specifier}{"ban add".Color(Utils.BoldHighlight)} \"{"acc:".Color(Utils.RedHighlight)}{args.Player.Account.Color(Utils.RedHighlight)}\" {"\"Multiple accounts are not allowed\"".Color(Utils.BoldHighlight)} {"-e".Color(Utils.GreenHighlight)} (Permanently bans this account name)", Color.White);
                        args.Player.SendMessage("- Ban an offline player by IP address", Color.White);
                        args.Player.SendMessage($"   {Specifier}{"ai".Color(Utils.BoldHighlight)} \"{args.Player.Account.Color(Utils.RedHighlight)}\" (Find the IP associated with the offline target's account)", Color.White);
                        args.Player.SendMessage($"   {Specifier}{"ban add".Color(Utils.BoldHighlight)} {"ip:".Color(Utils.RedHighlight)}{args.Player.IP.Color(Utils.RedHighlight)} {"\"Griefing\"".Color(Utils.BoldHighlight)} {"-e".Color(Utils.GreenHighlight)} (Permanently bans this IP address)", Color.White);
                        args.Player.SendMessage($"- Ban an online player by index (Useful for hard to type names)", Color.White);
                        args.Player.SendMessage($"   {Specifier}{"who".Color(Utils.BoldHighlight)} {"-i".Color(Utils.GreenHighlight)} (Find the player index for the target)", Color.White);
                        args.Player.SendMessage($"   {Specifier}{"ban add".Color(Utils.BoldHighlight)} {"tsi:".Color(Utils.RedHighlight)}{args.Player.Index.Color(Utils.RedHighlight)} {"\"Trolling\"".Color(Utils.BoldHighlight)} {"-a -u -ip".Color(Utils.GreenHighlight)} (Permanently bans the online player by Account, UUID, and IP)", Color.White);
                        // Ban by account ID when?
                        break;

                    default:
                        args.Player.SendMessage($"Unknown ban command. Try {"ban help".Color(Utils.BoldHighlight)} {"add".Color(Utils.RedHighlight)}, {"del".Color(Utils.RedHighlight)}, {"list".Color(Utils.RedHighlight)}, {"details".Color(Utils.RedHighlight)}, {"identifiers".Color(Utils.RedHighlight)}, or {"examples".Color(Utils.RedHighlight)}.", Color.White); break;
                }
            }

            void DisplayBanDetails(Ban ban)
            {
                args.Player.SendMessage($"{"Ban Details".Color(Utils.BoldHighlight)} - Ticket Number: {ban.TicketNumber.Color(Utils.GreenHighlight)}", Color.White);
                args.Player.SendMessage($"{"Identifier:".Color(Utils.BoldHighlight)} {ban.Identifier}", Color.White);
                args.Player.SendMessage($"{"Reason:".Color(Utils.BoldHighlight)} {ban.Reason}", Color.White);
                args.Player.SendMessage($"{"Banned by:".Color(Utils.BoldHighlight)} {ban.BanningUser.Color(Utils.GreenHighlight)} on {ban.BanDateTime.ToString("yyyy/MM/dd").Color(Utils.RedHighlight)} ({ban.GetPrettyTimeSinceBanString().Color(Utils.YellowHighlight)} ago)", Color.White);
                if (ban.ExpirationDateTime < DateTime.UtcNow)
                {
                    args.Player.SendMessage($"{"Ban expired:".Color(Utils.BoldHighlight)} {ban.ExpirationDateTime.ToString("yyyy/MM/dd").Color(Utils.RedHighlight)} ({ban.GetPrettyExpirationString().Color(Utils.YellowHighlight)} ago)", Color.White);
                }
                else
                {
                    string remaining;
                    if (ban.ExpirationDateTime == DateTime.MaxValue)
                    {
                        remaining = "Never".Color(Utils.YellowHighlight);
                    }
                    else
                    {
                        remaining = $"{ban.GetPrettyExpirationString().Color(Utils.YellowHighlight)} remaining";
                    }

                    args.Player.SendMessage($"{"Ban expires:".Color(Utils.BoldHighlight)} {ban.ExpirationDateTime.ToString("yyyy/MM/dd").Color(Utils.RedHighlight)} ({remaining})", Color.White);
                }
            }

            AddBanResult DoBan(string ident, string reason, DateTime expiration)
            {
                AddBanResult banResult = TShock.Bans.InsertBan(ident, reason, args.Player.Account.Name, DateTime.UtcNow, expiration);
                if (banResult.Ban != null)
                {
                    args.Player.SendSuccessMessage($"Ban added. Ticket Number {banResult.Ban.TicketNumber.Color(Utils.GreenHighlight)} was created for identifier {ident.Color(Utils.WhiteHighlight)}.");
                }
                else
                {
                    args.Player.SendWarningMessage($"Failed to add ban for identifier: {ident.Color(Utils.WhiteHighlight)}");
                    args.Player.SendWarningMessage($"Reason: {banResult.Message}");
                }

                return banResult;
            }

            void AddBan()
            {
                if (!args.Parameters.TryGetValue(1, out string target))
                {
                    args.Player.SendMessage($"Invalid Ban Add syntax. Refer to {"ban help add".Color(Utils.BoldHighlight)} for details on how to use the {"ban add".Color(Utils.BoldHighlight)} command", Color.White);
                    return;
                }

                bool exactTarget = args.Parameters.Any(p => p == "-e");
                bool banAccount = args.Parameters.Any(p => p == "-a");
                bool banUuid = args.Parameters.Any(p => p == "-u");
                bool banName = args.Parameters.Any(p => p == "-n");
                bool banIp = args.Parameters.Any(p => p == "-ip");

                List<string> flags = new List<string>() { "-e", "-a", "-u", "-n", "-ip" };

                string reason = "Banned.";
                string duration = null;
                DateTime expiration = DateTime.MaxValue;

                //This is hacky. We want flag values to be independent of order so we must force the consecutive ordering of the 'reason' and 'duration' parameters,
                //while still allowing them to be placed arbitrarily in the parameter list.
                //As an example, the following parameter lists (and more) should all be acceptable:
                //-u "reason" -a duration -ip
                //"reason" duration -u -a -ip
                //-u -a -ip "reason" duration
                //-u -a -ip
                for (int i = 2; i < args.Parameters.Count; i++)
                {
                    var param = args.Parameters[i];
                    if (!flags.Contains(param))
                    {
                        reason = param;
                        break;
                    }
                }
                for (int i = 3; i < args.Parameters.Count; i++)
                {
                    var param = args.Parameters[i];
                    if (!flags.Contains(param))
                    {
                        duration = param;
                        break;
                    }
                }

                if (TShock.Utils.TryParseTime(duration, out int seconds))
                {
                    expiration = DateTime.UtcNow.AddSeconds(seconds);
                }

                //If no flags were specified, default to account, uuid, and IP
                if (!exactTarget && !banAccount && !banUuid && !banName && !banIp)
                {
                    banAccount = banUuid = banIp = true;

                    if (TShock.Config.Settings.DisableDefaultIPBan)
                    {
                        banIp = false;
                    }
                }

                reason = reason ?? "Banned";

                if (exactTarget)
                {
                    DoBan(target, reason, expiration);
                    return;
                }

                var players = TSPlayer.FindByNameOrID(target);

                if (players.Count > 1)
                {
                    args.Player.SendMultipleMatchError(players.Select(p => p.Name));
                    return;
                }

                if (players.Count < 1)
                {
                    args.Player.SendErrorMessage("找不到指定的目标。检查拼写是否正确。");
                    return;
                }

                var player = players[0];
                AddBanResult banResult = null;

                if (banAccount)
                {
                    if (player.Account != null)
                    {
                        banResult = DoBan($"{Identifier.Account}{player.Account.Name}", reason, expiration);
                    }
                }

                if (banUuid)
                {
                    banResult = DoBan($"{Identifier.UUID}{player.UUID}", reason, expiration);
                }

                if (banName)
                {
                    banResult = DoBan($"{Identifier.Name}{player.Name}", reason, expiration);
                }

                if (banIp)
                {
                    banResult = DoBan($"{Identifier.IP}{player.IP}", reason, expiration);
                }

                if (banResult?.Ban != null)
                {
                    player.Disconnect($"#{banResult.Ban.TicketNumber} - You have been banned: {banResult.Ban.Reason}.");
                }
            }

            void DelBan()
            {
                if (!args.Parameters.TryGetValue(1, out string target))
                {
                    args.Player.SendMessage($"Invalid Ban Del syntax. Refer to {"ban help del".Color(Utils.BoldHighlight)} for details on how to use the {"ban del".Color(Utils.BoldHighlight)} command", Color.White);
                    return;
                }

                if (!int.TryParse(target, out int banId))
                {
                    args.Player.SendMessage($"Invalid Ticket Number. Refer to {"ban help del".Color(Utils.BoldHighlight)} for details on how to use the {"ban del".Color(Utils.BoldHighlight)} command", Color.White);
                    return;
                }

                if (TShock.Bans.RemoveBan(banId))
                {
                    TShock.Log.ConsoleInfo($"Ban {banId} has been revoked by {args.Player.Account.Name}.");
                    args.Player.SendSuccessMessage($"Ban {banId.Color(Utils.GreenHighlight)} has now been marked as expired.");
                }
                else
                {
                    args.Player.SendErrorMessage("Failed to remove ban.");
                }
            }

            void ListBans()
            {
                string PickColorForBan(Ban ban)
                {
                    double hoursRemaining = (ban.ExpirationDateTime - DateTime.UtcNow).TotalHours;
                    double hoursTotal = (ban.ExpirationDateTime - ban.BanDateTime).TotalHours;
                    double percentRemaining = TShock.Utils.Clamp(hoursRemaining / hoursTotal, 100, 0);

                    int red = TShock.Utils.Clamp((int)(255 * 2.0f * percentRemaining), 255, 0);
                    int green = TShock.Utils.Clamp((int)(255 * (2.0f * (1 - percentRemaining))), 255, 0);

                    return $"{red:X2}{green:X2}{0:X2}";
                }

                if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out int pageNumber))
                {
                    args.Player.SendMessage($"Invalid Ban List syntax. Refer to {"ban help list".Color(Utils.BoldHighlight)} for details on how to use the {"ban list".Color(Utils.BoldHighlight)} command", Color.White);
                    return;
                }

                var bans = from ban in TShock.Bans.Bans
                           where ban.Value.ExpirationDateTime > DateTime.UtcNow
                           orderby ban.Value.ExpirationDateTime ascending
                           select $"[{ban.Key.Color(Utils.GreenHighlight)}] {ban.Value.Identifier.Color(PickColorForBan(ban.Value))}";

                PaginationTools.SendPage(args.Player, pageNumber, bans.ToList(),
                    new PaginationTools.Settings
                    {
                        HeaderFormat = "Bans ({0}/{1}):",
                        FooterFormat = "Type {0}ban list {{0}} for more.".SFormat(Specifier),
                        NothingToDisplayString = "There are currently no active bans."
                    });
            }

            void BanDetails()
            {
                if (!args.Parameters.TryGetValue(1, out string target))
                {
                    args.Player.SendMessage($"Invalid Ban Details syntax. Refer to {"ban help details".Color(Utils.BoldHighlight)} for details on how to use the {"ban details".Color(Utils.BoldHighlight)} command", Color.White);
                    return;
                }

                if (!int.TryParse(target, out int banId))
                {
                    args.Player.SendMessage($"Invalid Ticket Number. Refer to {"ban help details".Color(Utils.BoldHighlight)} for details on how to use the {"ban details".Color(Utils.BoldHighlight)} command", Color.White);
                    return;
                }

                Ban ban = TShock.Bans.GetBanById(banId);

                if (ban == null)
                {
                    args.Player.SendErrorMessage("No bans found matching the provided ticket number");
                    return;
                }

                DisplayBanDetails(ban);
            }

            string subcmd = args.Parameters.Count == 0 ? "help" : args.Parameters[0].ToLower();
            switch (subcmd)
            {
                case "help":
                    Help();
                    break;

                case "add":
                    AddBan();
                    break;

                case "del":
                    DelBan();
                    break;

                case "list":
                    ListBans();
                    break;

                case "details":
                    BanDetails();
                    break;

                default:
                    break;
            }
        }

        private static void Whitelist(CommandArgs args)
        {
            if (args.Parameters.Count == 1)
            {
                using (var tw = new StreamWriter(FileTools.WhitelistPath, true))
                {
                    tw.WriteLine(args.Parameters[0]);
                }
                args.Player.SendSuccessMessage("已添加 " + args.Parameters[0] + " 到白名单。");
            }
        }

        private static void DisplayLogs(CommandArgs args)
        {
            args.Player.DisplayLogs = (!args.Player.DisplayLogs);
            args.Player.SendSuccessMessage("当前 " + (args.Player.DisplayLogs ? "正处于" : "不再处于") + " 接收日志状态中。");
        }

        private static void SaveSSC(CommandArgs args)
        {
            if (Main.ServerSideCharacter)
            {
                args.Player.SendSuccessMessage("SSC 已被保存。");
                foreach (TSPlayer player in TShock.Players)
                {
                    if (player != null && player.IsLoggedIn && !player.IsDisabledPendingTrashRemoval)
                    {
                        TShock.CharacterDB.InsertPlayerData(player, true);
                    }
                }
            }
        }

        private static void OverrideSSC(CommandArgs args)
        {
            if (!Main.ServerSideCharacter)
            {
                args.Player.SendErrorMessage("服务器端记录已禁用。");
                return;
            }
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("正确用法:{0}overridessc|{0}ossc <用户名>", Specifier);
                return;
            }

            string playerNameToMatch = string.Join(" ", args.Parameters);
            var matchedPlayers = TSPlayer.FindByNameOrID(playerNameToMatch);
            if (matchedPlayers.Count < 1)
            {
                args.Player.SendErrorMessage("没有匹配的用户 \"{0}\".", playerNameToMatch);
                return;
            }
            else if (matchedPlayers.Count > 1)
            {
                args.Player.SendMultipleMatchError(matchedPlayers.Select(p => p.Name));
                return;
            }

            TSPlayer matchedPlayer = matchedPlayers[0];
            if (matchedPlayer.IsLoggedIn)
            {
                args.Player.SendErrorMessage("用户 \"{0}\" 已登录。", matchedPlayer.Name);
                return;
            }
            if (!matchedPlayer.LoginFailsBySsi)
            {
                args.Player.SendErrorMessage("用户 \"{0}\" 必须先执行 /login 尝试登录。", matchedPlayer.Name);
                return;
            }
            if (matchedPlayer.IsDisabledPendingTrashRemoval)
            {
                args.Player.SendErrorMessage("用户 \"{0}\" 必须重新连接。", matchedPlayer.Name);
                return;
            }

            TShock.CharacterDB.InsertPlayerData(matchedPlayer);
            args.Player.SendSuccessMessage(" \"{0}\" 的SSC已被覆盖。", matchedPlayer.Name);
        }

        private static void UploadJoinData(CommandArgs args)
        {
            TSPlayer targetPlayer = args.Player;
            if (args.Parameters.Count == 1 && args.Player.HasPermission(Permissions.uploadothersdata))
            {
                List<TSPlayer> players = TSPlayer.FindByNameOrID(args.Parameters[0]);
                if (players.Count > 1)
                {
                    args.Player.SendMultipleMatchError(players.Select(p => p.Name));
                    return;
                }
                else if (players.Count == 0)
                {
                    args.Player.SendErrorMessage("找不到与'{0}'匹配的用户。", args.Parameters[0]);
                    return;
                }
                else
                {
                    targetPlayer = players[0];
                }
            }
            else if (args.Parameters.Count == 1)
            {
                args.Player.SendErrorMessage("你无权上载其他用户的角色数据。");
                return;
            }
            else if (args.Parameters.Count > 0)
            {
                args.Player.SendErrorMessage("用法: /uploadssc [用户名]");
                return;
            }
            else if (args.Parameters.Count == 0 && args.Player is TSServerPlayer)
            {
                args.Player.SendErrorMessage("在控制台无法上传其用户的数据。");
                args.Player.SendErrorMessage("用法: /uploadssc [用户名]");
                return;
            }

            if (targetPlayer.IsLoggedIn)
            {
                if (TShock.CharacterDB.InsertSpecificPlayerData(targetPlayer, targetPlayer.DataWhenJoined))
                {
                    targetPlayer.DataWhenJoined.RestoreCharacter(targetPlayer);
                    targetPlayer.SendSuccessMessage("你的本地数据已上传到服务器。");
                    args.Player.SendSuccessMessage("用户的角色数据已成功上传。");
                }
                else
                {
                    args.Player.SendErrorMessage("无法上载用户数据，是否已登录帐户？");
                }
            }
            else
            {
                args.Player.SendErrorMessage("目标用户尚未登录。");
            }
        }

        private static void ForceHalloween(CommandArgs args)
        {
            TShock.Config.Settings.ForceHalloween = !TShock.Config.Settings.ForceHalloween;
            Main.checkHalloween();
            if (args.Silent)
                args.Player.SendInfoMessage("{0}启用万圣节模式!", (TShock.Config.Settings.ForceHalloween ? "启用" : "禁用"));
            else
                TSPlayer.All.SendInfoMessage("{0} {1}万圣节模式!", args.Player.Name, (TShock.Config.Settings.ForceHalloween ? "启用" : "禁用"));
        }

        private static void ForceXmas(CommandArgs args)
        {
            TShock.Config.Settings.ForceXmas = !TShock.Config.Settings.ForceXmas;
            Main.checkXMas();
            if (args.Silent)
                args.Player.SendInfoMessage("{0}圣诞模式!", (TShock.Config.Settings.ForceXmas ? "启用" : "禁用"));
            else
                TSPlayer.All.SendInfoMessage("{0} {1}圣诞模式!", args.Player.Name, (TShock.Config.Settings.ForceXmas ? "启用" : "禁用"));
        }

        private static void TempGroup(CommandArgs args)
        {
            if (args.Parameters.Count < 2)
            {
                args.Player.SendInfoMessage("无效的用法");
                args.Player.SendInfoMessage("用法: {0}tempgroup <用户名> <临时用户组> [时长]", Specifier);
                return;
            }

            List<TSPlayer> ply = TSPlayer.FindByNameOrID(args.Parameters[0]);
            if (ply.Count < 1)
            {
                args.Player.SendErrorMessage("找不到用户 {0}.", args.Parameters[0]);
                return;
            }

            if (ply.Count > 1)
            {
                args.Player.SendMultipleMatchError(ply.Select(p => p.Account.Name));
            }

            if (!TShock.Groups.GroupExists(args.Parameters[1]))
            {
                args.Player.SendErrorMessage("找不到用户组 {0}", args.Parameters[1]);
                return;
            }

            if (args.Parameters.Count > 2)
            {
                int time;
                if (!TShock.Utils.TryParseTime(args.Parameters[2], out time))
                {
                    args.Player.SendErrorMessage("时长格式无效!正确的格式: _d_h_m_s, 至少带有一个时间分隔符。");
                    args.Player.SendErrorMessage("例如, 1d and 10h-30m+2m 都是有效的时间字符串，而单纯2则不是。");
                    return;
                }

                ply[0].tempGroupTimer = new System.Timers.Timer(time * 1000);
                ply[0].tempGroupTimer.Elapsed += ply[0].TempGroupTimerElapsed;
                ply[0].tempGroupTimer.Start();
            }

            Group g = TShock.Groups.GetGroupByName(args.Parameters[1]);

            ply[0].tempGroup = g;

            if (args.Parameters.Count < 3)
            {
                args.Player.SendSuccessMessage(String.Format("你已将 {0}的用户组更改为 {1}", ply[0].Name, g.Name));
                ply[0].SendSuccessMessage(String.Format("你的用户组别已临时更改为 {0}", g.Name));
            }
            else
            {
                args.Player.SendSuccessMessage(String.Format("你已将 {0} 的用户组由 {1} 改为 {2}",
                    ply[0].Name, g.Name, args.Parameters[2]));
                ply[0].SendSuccessMessage(String.Format("你的用户组已由 {0} 改为{1}",
                    g.Name, args.Parameters[2]));
            }
        }

        private static void SubstituteUser(CommandArgs args)
        {

            if (args.Player.tempGroup != null)
            {
                args.Player.tempGroup = null;
                args.Player.tempGroupTimer.Stop();
                args.Player.SendSuccessMessage("你已恢复到先前的权限。");
                return;
            }
            else
            {
                args.Player.tempGroup = new SuperAdminGroup();
                args.Player.tempGroupTimer = new System.Timers.Timer(600 * 1000);
                args.Player.tempGroupTimer.Elapsed += args.Player.TempGroupTimerElapsed;
                args.Player.tempGroupTimer.Start();
                args.Player.SendSuccessMessage("你的帐户已提升为超级管理员10分钟。");
                return;
            }
        }

        #endregion Player Management Commands

        #region Server Maintenence Commands

        // Executes a command as a superuser if you have sudo rights.
        private static void SubstituteUserDo(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("用法: /sudo [命令].");
                args.Player.SendErrorMessage("示例: /sudo /ban add Shank 2d Hacking.");
                return;
            }

            string replacementCommand = String.Join(" ", args.Parameters.Select(p => p.Contains(" ") ? $"\"{p}\"" : p));
            args.Player.tempGroup = new SuperAdminGroup();
            HandleCommand(args.Player, replacementCommand);
            args.Player.tempGroup = null;
            return;
        }

        private static void Broadcast(CommandArgs args)
        {
            string message = string.Join(" ", args.Parameters);

            TShock.Utils.Broadcast(
                "(服务器广播) " + message,
                Convert.ToByte(TShock.Config.Settings.BroadcastRGB[0]), Convert.ToByte(TShock.Config.Settings.BroadcastRGB[1]),
                Convert.ToByte(TShock.Config.Settings.BroadcastRGB[2]));
        }

        private static void Off(CommandArgs args)
        {

            if (Main.ServerSideCharacter)
            {
                foreach (TSPlayer player in TShock.Players)
                {
                    if (player != null && player.IsLoggedIn && !player.IsDisabledPendingTrashRemoval)
                    {
                        player.SaveServerCharacter();
                    }
                }
            }

            string reason = ((args.Parameters.Count > 0) ? "服务器正在关闭: " + String.Join(" ", args.Parameters) : "服务器正在关闭!");
            TShock.Utils.StopServer(true, reason);
        }

        private static void OffNoSave(CommandArgs args)
        {
            string reason = ((args.Parameters.Count > 0) ? "服务器正在关闭: " + String.Join(" ", args.Parameters) : "服务器正在关闭!");
            TShock.Utils.StopServer(false, reason);
        }

        private static void CheckUpdates(CommandArgs args)
        {
            args.Player.SendInfoMessage("更新列队已被加入日程。");
            try
            {
                TShock.UpdateManager.UpdateCheckAsync(null).Wait();
            }
            catch (Exception)
            {
                //swallow the exception
                return;
            }
        }

        private static void ManageRest(CommandArgs args)
        {
            string subCommand = "help";
            if (args.Parameters.Count > 0)
                subCommand = args.Parameters[0];

            switch (subCommand.ToLower())
            {
                case "listusers":
                    {
                        int pageNumber;
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                            return;

                        Dictionary<string, int> restUsersTokens = new Dictionary<string, int>();
                        foreach (Rests.SecureRest.TokenData tokenData in TShock.RestApi.Tokens.Values)
                        {
                            if (restUsersTokens.ContainsKey(tokenData.Username))
                                restUsersTokens[tokenData.Username]++;
                            else
                                restUsersTokens.Add(tokenData.Username, 1);
                        }

                        List<string> restUsers = new List<string>(
                            restUsersTokens.Select(ut => string.Format("{0} ({1} tokens)", ut.Key, ut.Value)));

                        PaginationTools.SendPage(
                            args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(restUsers), new PaginationTools.Settings
                            {
                                NothingToDisplayString = "There are currently no active REST users.",
                                HeaderFormat = "Active REST Users ({0}/{1}):",
                                FooterFormat = "Type {0}rest listusers {{0}} for more.".SFormat(Specifier)
                            }
                        );

                        break;
                    }
                case "destroytokens":
                    {
                        TShock.RestApi.Tokens.Clear();
                        args.Player.SendSuccessMessage("All REST tokens have been destroyed.");
                        break;
                    }
                default:
                    {
                        args.Player.SendInfoMessage("Available REST Sub-Commands:");
                        args.Player.SendMessage("listusers - Lists all REST users and their current active tokens.", Color.White);
                        args.Player.SendMessage("destroytokens - Destroys all current REST tokens.", Color.White);
                        break;
                    }
            }
        }

        #endregion Server Maintenence Commands

        #region Cause Events and Spawn Monsters Commands

        static readonly List<string> _validEvents = new List<string>()
        {
            "meteor",
            "fullmoon",
            "bloodmoon",
            "eclipse",
            "invasion",
            "sandstorm",
            "rain"
        };
        static readonly List<string> _validInvasions = new List<string>()
        {
            "goblins",
            "snowmen",
            "pirates",
            "pumpkinmoon",
            "frostmoon",
                      "martians"
        };

        private static void ManageWorldEvent(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("无效的语法!正确的语法: {0}worldevent <事件类型>", Specifier);
                args.Player.SendErrorMessage("有效的事件类型: {0}", String.Join(", ", _validEvents));
                args.Player.SendErrorMessage("有效的入侵类型: {0}", String.Join(", ", _validInvasions));
                return;
            }

            var eventType = args.Parameters[0].ToLowerInvariant();

            void FailedPermissionCheck()
            {
                args.Player.SendErrorMessage("您没有启动 {0} 事件的权限.", eventType);
                return;
            }

            switch (eventType)
            {
                case "meteor":
                    if (!args.Player.HasPermission(Permissions.dropmeteor) && !args.Player.HasPermission(Permissions.managemeteorevent))
                    {
                        FailedPermissionCheck();
                        return;
                    }

                    DropMeteor(args);
                    return;

                case "fullmoon":
                case "full moon":
                    if (!args.Player.HasPermission(Permissions.fullmoon) && !args.Player.HasPermission(Permissions.managefullmoonevent))
                    {
                        FailedPermissionCheck();
                        return;
                    }
                    Fullmoon(args);
                    return;

                case "bloodmoon":
                case "blood moon":
                    if (!args.Player.HasPermission(Permissions.bloodmoon) && !args.Player.HasPermission(Permissions.managebloodmoonevent))
                    {
                        FailedPermissionCheck();
                        return;
                    }
                    Bloodmoon(args);
                    return;

                case "eclipse":
                    if (!args.Player.HasPermission(Permissions.eclipse) && !args.Player.HasPermission(Permissions.manageeclipseevent))
                    {
                        FailedPermissionCheck();
                        return;
                    }
                    Eclipse(args);
                    return;

                case "invade":
                case "invasion":
                    if (!args.Player.HasPermission(Permissions.invade) && !args.Player.HasPermission(Permissions.manageinvasionevent))
                    {
                        FailedPermissionCheck();
                        return;
                    }
                    Invade(args);
                    return;

                case "sandstorm":
                    if (!args.Player.HasPermission(Permissions.sandstorm) && !args.Player.HasPermission(Permissions.managesandstormevent))
                    {
                        FailedPermissionCheck();
                        return;
                    }
                    Sandstorm(args);
                    return;

                case "rain":
                    if (!args.Player.HasPermission(Permissions.rain) && !args.Player.HasPermission(Permissions.managerainevent))
                    {
                        FailedPermissionCheck();
                        return;
                    }
                    Rain(args);
                    return;

                default:
                    args.Player.SendErrorMessage("无效的事件类型!有效事件类型: {0}", String.Join(", ", _validEvents));
                    return;
            }
        }

        private static void DropMeteor(CommandArgs args)
        {
            WorldGen.spawnMeteor = false;
            WorldGen.dropMeteor();
            if (args.Silent)
            {
                args.Player.SendInfoMessage("陨石来哩！");
            }
            else
            {
                TSPlayer.All.SendInfoMessage("{0} 触发了流星事件。", args.Player.Name);
            }
        }

        private static void Fullmoon(CommandArgs args)
        {
            TSPlayer.Server.SetFullMoon();
            if (args.Silent)
            {
                args.Player.SendInfoMessage("满月开始了。");
            }
            else
            {
                TSPlayer.All.SendInfoMessage("{0} 触发了满月事件。", args.Player.Name);
            }
        }

        private static void Bloodmoon(CommandArgs args)
        {
            TSPlayer.Server.SetBloodMoon(!Main.bloodMoon);
            if (args.Silent)
            {
                args.Player.SendInfoMessage("血月{0}。", Main.bloodMoon ? "触发了" : "停止了");
            }
            else
            {
                TSPlayer.All.SendInfoMessage("{0} {1}了血月。", args.Player.Name, Main.bloodMoon ? "触发了" : "停止了");
            }
        }

        private static void Eclipse(CommandArgs args)
        {
            TSPlayer.Server.SetEclipse(!Main.eclipse);
            if (args.Silent)
            {
                args.Player.SendInfoMessage("日食{0}", Main.eclipse ? "触发了" : "停止了");
            }
            else
            {
                TSPlayer.All.SendInfoMessage("{0}{1}了日食", args.Player.Name, Main.eclipse ? "触发了" : "停止了");
            }
        }

        private static void Invade(CommandArgs args)
        {
            if (Main.invasionSize <= 0)
            {
                if (args.Parameters.Count < 2)
                {
                    args.Player.SendErrorMessage("无效的语法!正确的语法:{0}worldevent invasion [入侵类型]  [入侵波]", Specifier);
                    args.Player.SendErrorMessage("有效的入侵类型: {0}", String.Join(", ", _validInvasions));
                    return;
                }

                int wave = 1;
                switch (args.Parameters[1].ToLowerInvariant())
                {
                    case "goblin":
                    case "goblins":
                        TSPlayer.All.SendInfoMessage("{0} 开始了地精军队入侵。", args.Player.Name);
                        TShock.Utils.StartInvasion(1);
                        break;

                    case "snowman":
                    case "snowmen":
                        TSPlayer.All.SendInfoMessage("{0}开始了雪人军团入侵", args.Player.Name);
                        TShock.Utils.StartInvasion(2);
                        break;

                    case "pirate":
                    case "pirates":
                        TSPlayer.All.SendInfoMessage("{0}开始了海盗入侵", args.Player.Name);
                        TShock.Utils.StartInvasion(3);
                        break;

                    case "pumpkin":
                    case "pumpkinmoon":
                        if (args.Parameters.Count > 2)
                        {
                            if (!int.TryParse(args.Parameters[2], out wave) || wave <= 0)
                            {
                                args.Player.SendErrorMessage("无效波！");
                                break;
                            }
                        }

                        TSPlayer.Server.SetPumpkinMoon(true);
                        Main.bloodMoon = false;
                        NPC.waveKills = 0f;
                        NPC.waveNumber = wave;
                        TSPlayer.All.SendInfoMessage("{0}开始了第{1}波南瓜月!", args.Player.Name, wave);
                        break;

                    case "frost":
                    case "frostmoon":
                        if (args.Parameters.Count > 2)
                        {
                            if (!int.TryParse(args.Parameters[2], out wave) || wave <= 0)
                            {
                                args.Player.SendErrorMessage("无效波!");
                                return;
                            }
                        }

                        TSPlayer.Server.SetFrostMoon(true);
                        Main.bloodMoon = false;
                        NPC.waveKills = 0f;
                        NPC.waveNumber = wave;
                        TSPlayer.All.SendInfoMessage("{0}开始了第{1}波霜月!", args.Player.Name, wave);
                        break;

                    case "martian":
                    case "martians":
                        TSPlayer.All.SendInfoMessage("{0}开始火星人入侵", args.Player.Name);
                        TShock.Utils.StartInvasion(4);
                        break;

                    default:
                        args.Player.SendErrorMessage("无效的入侵类型!有效的入侵类型:{0}", String.Join(", ", _validInvasions));
                        break;
                }
            }
            else if (DD2Event.Ongoing)
            {
                DD2Event.StopInvasion();
                TSPlayer.All.SendInfoMessage("{0} 已结束旧日军活动。", args.Player.Name);
            }
            else
            {
                TSPlayer.All.SendInfoMessage("{0} 已结束入侵。", args.Player.Name);
                Main.invasionSize = 0;
            }
        }

        private static void Sandstorm(CommandArgs args)
        {
            if (Terraria.GameContent.Events.Sandstorm.Happening)
            {
                Terraria.GameContent.Events.Sandstorm.StopSandstorm();
                TSPlayer.All.SendInfoMessage("{0} 停止了沙尘暴。", args.Player.Name);
            }
            else
            {
                Terraria.GameContent.Events.Sandstorm.StartSandstorm();
                TSPlayer.All.SendInfoMessage("{0} 开始了沙尘暴。", args.Player.Name);
            }
        }

        private static void Rain(CommandArgs args)
        {
            bool slime = false;
            if (args.Parameters.Count > 1 && args.Parameters[1].ToLowerInvariant() == "slime")
            {
                slime = true;
            }

            if (!slime)
            {
                args.Player.SendInfoMessage("使用 \"{0}worldevent rain slime\" 开启史莱姆雨!", Specifier);
            }

            if (slime && Main.raining) //Slime rain cannot be activated during normal rain
            {
                args.Player.SendErrorMessage("在开始史莱姆雨之前，你应该停止当前的倾盆大雨！");
                return;
            }

            if (slime && Main.slimeRain) //Toggle slime rain off
            {
                Main.StopSlimeRain(false);
                TSPlayer.All.SendData(PacketTypes.WorldInfo);
                TSPlayer.All.SendInfoMessage("{0} 结束了史莱姆雨。", args.Player.Name);
                return;
            }

            if (slime && !Main.slimeRain) //Toggle slime rain on
            {
                Main.StartSlimeRain(false);
                TSPlayer.All.SendData(PacketTypes.WorldInfo);
                TSPlayer.All.SendInfoMessage("{0} 触发了史莱姆雨。", args.Player.Name);
            }

            if (Main.raining && !slime) //Toggle rain off
            {
                Main.StopRain();
                TSPlayer.All.SendData(PacketTypes.WorldInfo);
                TSPlayer.All.SendInfoMessage("{0} 结束了大雨。", args.Player.Name);
                return;
            }

            if (!Main.raining && !slime) //Toggle rain on
            {
                Main.StartRain();
                TSPlayer.All.SendData(PacketTypes.WorldInfo);
                TSPlayer.All.SendInfoMessage("{0} 引发了大雨。", args.Player.Name);
                return;
            }
        }

        private static void ClearAnglerQuests(CommandArgs args)
        {
            if (args.Parameters.Count > 0)
            {
                var result = Main.anglerWhoFinishedToday.RemoveAll(s => s.ToLower().Equals(args.Parameters[0].ToLower()));
                if (result > 0)
                {
                    args.Player.SendSuccessMessage("从今天钓鱼任务列表中移除了{0}个用户。", result);
                    foreach (TSPlayer ply in TShock.Players.Where(p => p != null && p.Active && p.TPlayer.name.ToLower().Equals(args.Parameters[0].ToLower())))
                    {
                        //this will always tell the client that they have not done the quest today.
                        ply.SendData((PacketTypes)74, "");
                    }
                }
                else
                    args.Player.SendErrorMessage("在列表上找不到任何用户。");

            }
            else
            {
                Main.anglerWhoFinishedToday.Clear();
                NetMessage.SendAnglerQuest(-1);
                args.Player.SendSuccessMessage("从钓鱼任务列表中清除了所有用户。");
            }
        }

        static Dictionary<string, int> _worldModes = new Dictionary<string, int>
        {
            { "normal",    0 },
            { "expert",    1 },
            { "master",    2 },
            { "journey",   3 },
            { "creative",  3 }
        };

        private static void ChangeWorldMode(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("无效的语法!正确的语法： {0}worldmode <模式>", Specifier);
                args.Player.SendErrorMessage("有效模式: {0}", String.Join(", ", _worldModes.Keys));
                return;
            }

            int mode;

            if (int.TryParse(args.Parameters[0], out mode))
            {
                if (mode < 0 || mode > 3)
                {
                    args.Player.SendErrorMessage("无效模式!有效模式: {0}", String.Join(", ", _worldModes.Keys));
                    return;
                }
            }
            else if (_worldModes.ContainsKey(args.Parameters[0].ToLowerInvariant()))
            {
                mode = _worldModes[args.Parameters[0].ToLowerInvariant()];
            }
            else
            {
                args.Player.SendErrorMessage("无效模式!有效模式: {0}", String.Join(", ", _worldModes.Keys));
                return;
            }

            Main.GameMode = mode;
            args.Player.SendSuccessMessage("世界模式设置为 {0}", _worldModes.Keys.ElementAt(mode));
            TSPlayer.All.SendData(PacketTypes.WorldInfo);
        }

        private static void Hardmode(CommandArgs args)
        {
            if (Main.hardMode)
            {
                Main.hardMode = false;
                TSPlayer.All.SendData(PacketTypes.WorldInfo);
                args.Player.SendSuccessMessage("困难模式已关闭。");
            }
            else if (!TShock.Config.Settings.DisableHardmode)
            {
                WorldGen.StartHardmode();
                args.Player.SendSuccessMessage("困难模式已开启。");
            }
            else
            {
                args.Player.SendErrorMessage("困难模式在配置中被禁用。");
            }
        }

        private static void SpawnBoss(CommandArgs args)
        {
            if (args.Parameters.Count < 1 || args.Parameters.Count > 2)
            {
                args.Player.SendErrorMessage("无效的语法!正确的语法: {0}spawnboss <BOSS类型> [数量]", Specifier);
                return;
            }

            int amount = 1;
            if (args.Parameters.Count == 2 && (!int.TryParse(args.Parameters[1], out amount) || amount <= 0))
            {
                args.Player.SendErrorMessage("无效的数量!");
                return;
            }

            string message = "{0} spawned {1} {2} time(s)";
            string spawnName;
            NPC npc = new NPC();
            switch (args.Parameters[0].ToLower())
            {
                case "*":
                case "all":
                    int[] npcIds = { 4, 13, 35, 50, 125, 126, 127, 134, 222, 245, 262, 266, 370, 398, 439, 636, 657 };
                    TSPlayer.Server.SetTime(false, 0.0);
                    foreach (int i in npcIds)
                    {
                        npc.SetDefaults(i);
                        TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    }
                    spawnName = "所有BOSS";
                    break;

                case "brain":
                case "brain of cthulhu":
                case "boc":
                    npc.SetDefaults(266);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "克苏鲁之脑";
                    break;

                case "destroyer":
                    npc.SetDefaults(134);
                    TSPlayer.Server.SetTime(false, 0.0);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "毁灭者";
                    break;
                case "duke":
                case "duke fishron":
                case "fishron":
                    npc.SetDefaults(370);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "猪龙鱼公爵";
                    break;
                case "eater":
                case "eater of worlds":
                case "eow":
                    npc.SetDefaults(13);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "猪龙鱼公爵";
                    break;
                case "eye":
                case "eye of cthulhu":
                case "eoc":
                    npc.SetDefaults(4);
                    TSPlayer.Server.SetTime(false, 0.0);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "克苏鲁之眼";
                    break;
                case "golem":
                    npc.SetDefaults(245);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "石巨人";
                    break;
                case "king":
                case "king slime":
                case "ks":
                    npc.SetDefaults(50);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "史莱姆王";
                    break;
                case "plantera":
                    npc.SetDefaults(262);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "世纪之花";
                    break;
                case "prime":
                case "skeletron prime":
                    npc.SetDefaults(127);
                    TSPlayer.Server.SetTime(false, 0.0);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "机械骷髅王";
                    break;
                case "queen bee":
                case "qb":
                    npc.SetDefaults(222);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "蜂后";
                    break;
                case "skeletron":
                    npc.SetDefaults(35);
                    TSPlayer.Server.SetTime(false, 0.0);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "骷髅王";
                    break;
                case "twins":
                    TSPlayer.Server.SetTime(false, 0.0);
                    npc.SetDefaults(125);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    npc.SetDefaults(126);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "双子魔眼";
                    break;
                case "wof":
                case "wall of flesh":
                    if (Main.wofNPCIndex != -1)
                    {
                        args.Player.SendErrorMessage("血肉之墙出现辣！");
                        return;
                    }
                    if (args.Player.Y / 16f < Main.maxTilesY - 205)
                    {
                        args.Player.SendErrorMessage("你必须在地狱中生成血肉之墙!");
                        return;
                    }
                    NPC.SpawnWOF(new Vector2(args.Player.X, args.Player.Y));
                    spawnName = "血肉之墙";
                    break;
                case "moon":
                case "moon lord":
                case "ml":
                    npc.SetDefaults(398);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "月球领主";
                    break;
                case "empress":
                case "empress of light":
                case "eol":
                    npc.SetDefaults(636);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "光之女皇";
                    break;
                case "queen slime":
                case "qs":
                    npc.SetDefaults(657);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "史莱姆皇后";
                    break;
                case "lunatic":
                case "lunatic cultist":
                case "cultist":
                case "lc":
                    npc.SetDefaults(439);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "拜月教邪教徒";
                    break;
                case "betsy":
                    npc.SetDefaults(551);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "双足翼龙";
                    break;
                case "flying dutchman":
                case "flying":
                case "dutchman":
                    npc.SetDefaults(491);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "荷兰飞盗船";
                    break;
                case "mourning wood":
                    npc.SetDefaults(325);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "哀木";
                    break;
                case "pumpking":
                    npc.SetDefaults(327);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "南瓜王";
                    break;
                case "everscream":
                    npc.SetDefaults(344);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "常绿尖叫怪";
                    break;
                case "santa-nk1":
                case "santa":
                    npc.SetDefaults(346);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "圣诞坦克";
                    break;
                case "ice queen":
                    npc.SetDefaults(345);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "冰雪女王";
                    break;
                case "martian saucer":
                    npc.SetDefaults(392);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "火星飞碟";
                    break;
                case "solar pillar":
                    npc.SetDefaults(517);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "日耀柱";
                    break;
                case "nebula pillar":
                    npc.SetDefaults(507);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "星云柱";
                    break;
                case "vortex pillar":
                    npc.SetDefaults(422);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "星旋柱";
                    break;
                case "stardust pillar":
                    npc.SetDefaults(493);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "星尘柱";
                    break;
                case "deerclops":
                    npc.SetDefaults(668);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                    spawnName = "独眼巨鹿";
                    break;
                default:
                    args.Player.SendErrorMessage("无效BOSS类型!");
                    return;
            }

            if (args.Silent)
            {
                //"You spawned <spawn name> <x> time(s)"
                args.Player.SendSuccessMessage(message, "你", spawnName, amount);
            }
            else
            {
                //"<player> spawned <spawn name> <x> time(s)"
                TSPlayer.All.SendSuccessMessage(message, args.Player.Name, spawnName, amount);
            }
        }

        private static void SpawnMob(CommandArgs args)
        {
            if (args.Parameters.Count < 1 || args.Parameters.Count > 2)
            {
                args.Player.SendErrorMessage("无效的语法!正确的语法: {0}spawnmob <生物类型> [数量]", Specifier);
                return;
            }
            if (args.Parameters[0].Length == 0)
            {
                args.Player.SendErrorMessage("无效的生物类型!");
                return;
            }

            int amount = 1;
            if (args.Parameters.Count == 2 && !int.TryParse(args.Parameters[1], out amount))
            {
                args.Player.SendErrorMessage("无效的语法!正确的语法: {0}spawnmob <生物类型> [数量]", Specifier);
                return;
            }

            amount = Math.Min(amount, Main.maxNPCs);

            var npcs = TShock.Utils.GetNPCByIdOrName(args.Parameters[0]);
            if (npcs.Count == 0)
            {
                args.Player.SendErrorMessage("无效的生物类型!");
            }
            else if (npcs.Count > 1)
            {
                args.Player.SendMultipleMatchError(npcs.Select(n => $"{n.FullName}({n.type})"));
            }
            else
            {
                var npc = npcs[0];
                if (npc.type >= 1 && npc.type < Main.maxNPCTypes && npc.type != 113)
                {
                    TSPlayer.Server.SpawnNPC(npc.netID, npc.FullName, amount, args.Player.TileX, args.Player.TileY, 50, 20);
                    if (args.Silent)
                    {
                        args.Player.SendSuccessMessage("生成了 {0} {1} 次.", npc.FullName, amount);
                    }
                    else
                    {
                        TSPlayer.All.SendSuccessMessage("{0} 已经生成了 {1} {2} 次.", args.Player.Name, npc.FullName, amount);
                    }
                }
                else if (npc.type == 113)
                {
                    if (Main.wofNPCIndex != -1 || (args.Player.Y / 16f < (Main.maxTilesY - 205)))
                    {
                        args.Player.SendErrorMessage("无法生成肉山!");
                        return;
                    }
                    NPC.SpawnWOF(new Vector2(args.Player.X, args.Player.Y));
                    if (args.Silent)
                    {
                        args.Player.SendSuccessMessage("召唤了肉山!");
                    }
                    else
                    {
                        TSPlayer.All.SendSuccessMessage("{0} 召唤了肉山!", args.Player.Name);
                    }
                }
                else
                {
                    args.Player.SendErrorMessage("无效的生物类型!");
                }
            }
        }

        #endregion Cause Events and Spawn Monsters Commands

        #region Teleport Commands

        private static void Home(CommandArgs args)
        {
            if (args.Player.Dead)
            {
                args.Player.SendErrorMessage("You are dead.");
                return;
            }
            args.Player.Spawn(PlayerSpawnContext.RecallFromItem);
            args.Player.SendSuccessMessage("传送到你的出生点。");
        }

        private static void Spawn(CommandArgs args)
        {
            if (args.Player.Teleport(Main.spawnTileX * 16, (Main.spawnTileY * 16) - 48))
                args.Player.SendSuccessMessage("传送到世界出生点。");
        }

        private static void TP(CommandArgs args)
        {
            if (args.Parameters.Count != 1 && args.Parameters.Count != 2)
            {
                if (args.Player.HasPermission(Permissions.tpothers))
                    args.Player.SendErrorMessage("无效的语法!正确的语法: {0}tp <被传送的用户> [传送到的用户]", Specifier);
                else
                    args.Player.SendErrorMessage("无效的语法!正确的语法: {0}tp <把自己传送到的用户>", Specifier);
                return;
            }

            if (args.Parameters.Count == 1)
            {
                var players = TSPlayer.FindByNameOrID(args.Parameters[0]);
                if (players.Count == 0)
                    args.Player.SendErrorMessage("无效用户!");
                else if (players.Count > 1)
                    args.Player.SendMultipleMatchError(players.Select(p => p.Name));
                else
                {
                    var target = players[0];
                    if (!target.TPAllow && !args.Player.HasPermission(Permissions.tpoverride))
                    {
                        args.Player.SendErrorMessage("{0} 已经禁用传送。", target.Name);
                        return;
                    }
                    if (args.Player.Teleport(target.TPlayer.position.X, target.TPlayer.position.Y))
                    {
                        args.Player.SendSuccessMessage("传送到 {0}.", target.Name);
                        if (!args.Player.HasPermission(Permissions.tpsilent))
                            target.SendInfoMessage("{0} 传送到你附近.", args.Player.Name);
                    }
                }
            }
            else
            {
                if (!args.Player.HasPermission(Permissions.tpothers))
                {
                    args.Player.SendErrorMessage("你无权访问此命令。");
                    return;
                }

                var players1 = TSPlayer.FindByNameOrID(args.Parameters[0]);
                var players2 = TSPlayer.FindByNameOrID(args.Parameters[1]);

                if (players2.Count == 0)
                    args.Player.SendErrorMessage("无效用户!");
                else if (players2.Count > 1)
                    args.Player.SendMultipleMatchError(players2.Select(p => p.Name));
                else if (players1.Count == 0)
                {
                    if (args.Parameters[0] == "*")
                    {
                        if (!args.Player.HasPermission(Permissions.tpallothers))
                        {
                            args.Player.SendErrorMessage("你无权访问此命令。");
                            return;
                        }

                        var target = players2[0];
                        foreach (var source in TShock.Players.Where(p => p != null && p != args.Player))
                        {
                            if (!target.TPAllow && !args.Player.HasPermission(Permissions.tpoverride))
                                continue;
                            if (source.Teleport(target.TPlayer.position.X, target.TPlayer.position.Y))
                            {
                                if (args.Player != source)
                                {
                                    if (args.Player.HasPermission(Permissions.tpsilent))
                                        source.SendSuccessMessage("你被传送到 {0}.", target.Name);
                                    else
                                        source.SendSuccessMessage("{0} 将你传送到 {1}.", args.Player.Name, target.Name);
                                }
                                if (args.Player != target)
                                {
                                    if (args.Player.HasPermission(Permissions.tpsilent))
                                        target.SendInfoMessage("{0} 被传送到附近。", source.Name);
                                    if (!args.Player.HasPermission(Permissions.tpsilent))
                                        target.SendInfoMessage("{0} 传送了 {1} 到你附近。", args.Player.Name, source.Name);
                                }
                            }
                        }
                        args.Player.SendSuccessMessage("将所有人传送到 {0} 附近。", target.Name);
                    }
                    else
                        args.Player.SendErrorMessage("无效用户!");
                }
                else if (players1.Count > 1)
                    args.Player.SendMultipleMatchError(players1.Select(p => p.Name));
                else
                {
                    var source = players1[0];
                    if (!source.TPAllow && !args.Player.HasPermission(Permissions.tpoverride))
                    {
                        args.Player.SendErrorMessage("{0} 禁用传送。", source.Name);
                        return;
                    }
                    var target = players2[0];
                    if (!target.TPAllow && !args.Player.HasPermission(Permissions.tpoverride))
                    {
                        args.Player.SendErrorMessage("{0} 禁用传送。", target.Name);
                        return;
                    }
                    args.Player.SendSuccessMessage("Teleported {0} to {1}.", source.Name, target.Name);
                    if (source.Teleport(target.TPlayer.position.X, target.TPlayer.position.Y))
                    {
                        if (args.Player != source)
                        {
                            if (args.Player.HasPermission(Permissions.tpsilent))
                                source.SendSuccessMessage("你被传送到 {0}.", target.Name);
                            else
                                source.SendSuccessMessage("{0} 将你传送到 {1}.", args.Player.Name, target.Name);
                        }
                        if (args.Player != target)
                        {
                            if (args.Player.HasPermission(Permissions.tpsilent))
                                target.SendInfoMessage("{0} 被传送到附近。", source.Name);
                            if (!args.Player.HasPermission(Permissions.tpsilent))
                                target.SendInfoMessage("{0} 传送了 {1} 到你附近。", args.Player.Name, source.Name);
                        }
                    }
                }
            }
        }

        private static void TPHere(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                if (args.Player.HasPermission(Permissions.tpallothers))
                    args.Player.SendErrorMessage("无效的语法!正确的语法: {0}tphere <player|*>", Specifier);
                else
                    args.Player.SendErrorMessage("无效的语法!正确的语法: {0}tphere <player>", Specifier);
                return;
            }

            string playerName = String.Join(" ", args.Parameters);
            var players = TSPlayer.FindByNameOrID(playerName);
            if (players.Count == 0)
            {
                if (playerName == "*")
                {
                    if (!args.Player.HasPermission(Permissions.tpallothers))
                    {
                        args.Player.SendErrorMessage("You do not have permission to use this command.");
                        return;
                    }
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        if (Main.player[i].active && (Main.player[i] != args.TPlayer))
                        {
                            if (TShock.Players[i].Teleport(args.TPlayer.position.X, args.TPlayer.position.Y))
                                TShock.Players[i].SendSuccessMessage(String.Format("你被传送到 {0}.", args.Player.Name));
                        }
                    }
                    args.Player.SendSuccessMessage("Teleported everyone to yourself.");
                }
                else
                    args.Player.SendErrorMessage("Invalid player!");
            }
            else if (players.Count > 1)
                args.Player.SendMultipleMatchError(players.Select(p => p.Name));
            else
            {
                var plr = players[0];
                if (plr.Teleport(args.TPlayer.position.X, args.TPlayer.position.Y))
                {
                    plr.SendInfoMessage("你被传送到 {0}.", args.Player.Name);
                    args.Player.SendSuccessMessage("Teleported {0} to yourself.", plr.Name);
                }
            }
        }

        private static void TPNpc(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("无效的语法!正确的语法: {0}tpnpc <NPC>", Specifier);
                return;
            }

            var npcStr = string.Join(" ", args.Parameters);
            var matches = new List<NPC>();
            foreach (var npc in Main.npc.Where(npc => npc.active))
            {
                var englishName = EnglishLanguage.GetNpcNameById(npc.netID);

                if (string.Equals(npc.FullName, npcStr, StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(englishName, npcStr, StringComparison.InvariantCultureIgnoreCase))
                {
                    matches = new List<NPC> { npc };
                    break;
                }
                if (npc.FullName.ToLowerInvariant().StartsWith(npcStr.ToLowerInvariant()) ||
                    englishName?.StartsWith(npcStr, StringComparison.InvariantCultureIgnoreCase) == true)
                    matches.Add(npc);
            }

            if (matches.Count > 1)
            {
                args.Player.SendMultipleMatchError(matches.Select(n => $"{n.FullName}({n.whoAmI})"));
                return;
            }
            if (matches.Count == 0)
            {
                args.Player.SendErrorMessage("Invalid NPC!");
                return;
            }

            var target = matches[0];
            args.Player.Teleport(target.position.X, target.position.Y);
            args.Player.SendSuccessMessage("Teleported to the '{0}'.", target.FullName);
        }

        private static void GetPos(CommandArgs args)
        {
            var player = args.Player.Name;
            if (args.Parameters.Count > 0)
            {
                player = String.Join(" ", args.Parameters);
            }

            var players = TSPlayer.FindByNameOrID(player);
            if (players.Count == 0)
            {
                args.Player.SendErrorMessage("Invalid player!");
            }
            else if (players.Count > 1)
            {
                args.Player.SendMultipleMatchError(players.Select(p => p.Name));
            }
            else
            {
                args.Player.SendSuccessMessage("Location of {0} is ({1}, {2}).", players[0].Name, players[0].TileX, players[0].TileY);
            }
        }

        private static void TPPos(CommandArgs args)
        {
            if (args.Parameters.Count != 2)
            {
                args.Player.SendErrorMessage("无效的语法!正确的语法: {0}tppos <坐标x> <坐标y>", Specifier);
                return;
            }

            int x, y;
            if (!int.TryParse(args.Parameters[0], out x) || !int.TryParse(args.Parameters[1], out y))
            {
                args.Player.SendErrorMessage("无效的坐标!");
                return;
            }
            x = Math.Max(0, x);
            y = Math.Max(0, y);
            x = Math.Min(x, Main.maxTilesX - 1);
            y = Math.Min(y, Main.maxTilesY - 1);

            args.Player.Teleport(16 * x, 16 * y);
            args.Player.SendSuccessMessage("已被传送到 {0}, {1}!", x, y);
        }

        private static void TPAllow(CommandArgs args)
        {
            if (!args.Player.TPAllow)
                args.Player.SendSuccessMessage("你已停用了被传送功能。");
            if (args.Player.TPAllow)
                args.Player.SendSuccessMessage("你已启用了被传送功能。");
            args.Player.TPAllow = !args.Player.TPAllow;
        }

        private static void Warp(CommandArgs args)
        {
            bool hasManageWarpPermission = args.Player.HasPermission(Permissions.managewarp);
            if (args.Parameters.Count < 1)
            {
                if (hasManageWarpPermission)
                {
                    args.Player.SendInfoMessage("无效的语法!正确的语法:{0}warp [命令] [参数]", Specifier);
                    args.Player.SendInfoMessage("命令:add(添加)，del(删除)，hide(隐藏)，list(列出)，send(发送)，[传送点名称]");
                    args.Player.SendInfoMessage("参数:add [传送点名称]，del [传送点名称]，列表 [页码]");
                    args.Player.SendInfoMessage("参数:send [用户名] [传送点名称]，hide [传送点名称] [启用(true/false)]");
                    args.Player.SendInfoMessage("示例:{0}warp add foobar，{0}warp hide foobar true，{0}warp foobar", Specifier);
                    return;
                }
                else
                {
                    args.Player.SendErrorMessage("无效的语法!正确的语法: {0}warp [传送点名称] or {0}warp list <页码>", Specifier);
                    return;
                }
            }

            if (args.Parameters[0].Equals("list"))
            {
                #region List warps
                int pageNumber;
                if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                    return;
                IEnumerable<string> warpNames = from warp in TShock.Warps.Warps
                                                where !warp.IsPrivate
                                                select warp.Name;
                PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(warpNames),
                    new PaginationTools.Settings
                    {
                        HeaderFormat = "Warps ({0}/{1}):",
                        FooterFormat = "输入 {0}warp list {{0}} 以了解更多。".SFormat(Specifier),
                        NothingToDisplayString = "当前没有定义传送点。"
                    });
                #endregion
            }
            else if (args.Parameters[0].ToLower() == "add" && hasManageWarpPermission)
            {
                #region Add warp
                if (args.Parameters.Count == 2)
                {
                    string warpName = args.Parameters[1];
                    if (warpName == "list" || warpName == "hide" || warpName == "del" || warpName == "add")
                    {
                        args.Player.SendErrorMessage("Name reserved, use a different name.");
                    }
                    else if (TShock.Warps.Add(args.Player.TileX, args.Player.TileY, warpName))
                    {
                        args.Player.SendSuccessMessage("传送点被添加: " + warpName);
                    }
                    else
                    {
                        args.Player.SendErrorMessage("Warp " + warpName + " 已经在用户组内。");
                    }
                }
                else
                    args.Player.SendErrorMessage("无效的语法!正确的语法: {0}warp add [传送点名称]", Specifier);
                #endregion
            }
            else if (args.Parameters[0].ToLower() == "del" && hasManageWarpPermission)
            {
                #region Del warp
                if (args.Parameters.Count == 2)
                {
                    string warpName = args.Parameters[1];
                    if (TShock.Warps.Remove(warpName))
                    {
                        args.Player.SendSuccessMessage("传送点被删除: " + warpName);
                    }
                    else
                        args.Player.SendErrorMessage("找不到指定的传送点。");
                }
                else
                    args.Player.SendErrorMessage("无效的语法!正确的语法: {0}warp del [传送点名称]", Specifier);
                #endregion
            }
            else if (args.Parameters[0].ToLower() == "hide" && hasManageWarpPermission)
            {
                #region Hide warp
                if (args.Parameters.Count == 3)
                {
                    string warpName = args.Parameters[1];
                    bool state = false;
                    if (Boolean.TryParse(args.Parameters[2], out state))
                    {
                        if (TShock.Warps.Hide(args.Parameters[1], state))
                        {
                            if (state)
                                args.Player.SendSuccessMessage("Warp " + warpName + " 是私有的。");
                            else
                                args.Player.SendSuccessMessage("Warp " + warpName + " 是公开的。");
                        }
                        else
                            args.Player.SendErrorMessage("找不到指定的传送点。");
                    }
                    else
                        args.Player.SendErrorMessage("无效的语法!正确的语法: {0}warp hide [传送点名称] <true/false>", Specifier);
                }
                else
                    args.Player.SendErrorMessage("无效的语法!正确的语法: {0}warp hide [传送点名称] <true/false>", Specifier);
                #endregion
            }
            else if (args.Parameters[0].ToLower() == "send" && args.Player.HasPermission(Permissions.tpothers))
            {
                #region Warp send
                if (args.Parameters.Count < 3)
                {
                    args.Player.SendErrorMessage("无效的语法!正确的语法: {0}warp send [用户名] [传送点名称]", Specifier);
                    return;
                }

                var foundplr = TSPlayer.FindByNameOrID(args.Parameters[1]);
                if (foundplr.Count == 0)
                {
                    args.Player.SendErrorMessage("无效用户！");
                    return;
                }
                else if (foundplr.Count > 1)
                {
                    args.Player.SendMultipleMatchError(foundplr.Select(p => p.Name));
                    return;
                }

                string warpName = args.Parameters[2];
                var warp = TShock.Warps.Find(warpName);
                var plr = foundplr[0];
                if (warp != null)
                {
                    if (plr.Teleport(warp.Position.X * 16, warp.Position.Y * 16))
                    {
                        plr.SendSuccessMessage(String.Format("{0} warped you to {1}.", args.Player.Name, warpName));
                        args.Player.SendSuccessMessage(String.Format("You warped {0} to {1}.", plr.Name, warpName));
                    }
                }
                else
                {
                    args.Player.SendErrorMessage("找不到指定的传送点。");
                }
                #endregion
            }
            else
            {
                string warpName = String.Join(" ", args.Parameters);
                var warp = TShock.Warps.Find(warpName);
                if (warp != null)
                {
                    if (args.Player.Teleport(warp.Position.X * 16, warp.Position.Y * 16))
                        args.Player.SendSuccessMessage("传送到 " + warpName + ".");
                }
                else
                {
                    args.Player.SendErrorMessage("找不到指定的传送点。");
                }
            }
        }

        #endregion Teleport Commands

        #region Group Management

        private static void Group(CommandArgs args)
        {
            string subCmd = args.Parameters.Count == 0 ? "help" : args.Parameters[0].ToLower();

            switch (subCmd)
            {
                case "add":
                    #region Add group
                    {
                        if (args.Parameters.Count < 2)
                        {
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}group add <用户组> [权限]", Specifier);
                            return;
                        }

                        string groupName = args.Parameters[1];
                        args.Parameters.RemoveRange(0, 2);
                        string permissions = String.Join(",", args.Parameters);

                        try
                        {
                            TShock.Groups.AddGroup(groupName, null, permissions, TShockAPI.Group.defaultChatColor);
                            args.Player.SendSuccessMessage("已成功添加用户组！");
                        }
                        catch (GroupExistsException)
                        {
                            args.Player.SendErrorMessage("该用户组已存在!");
                        }
                        catch (GroupManagerException ex)
                        {
                            args.Player.SendErrorMessage(ex.ToString());
                        }
                    }
                    #endregion
                    return;
                case "addperm":
                    #region Add permissions
                    {
                        if (args.Parameters.Count < 3)
                        {
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}group addperm <用户组> <权限...>", Specifier);
                            return;
                        }

                        string groupName = args.Parameters[1];
                        args.Parameters.RemoveRange(0, 2);
                        if (groupName == "*")
                        {
                            foreach (Group g in TShock.Groups)
                            {
                                TShock.Groups.AddPermissions(g.Name, args.Parameters);
                            }
                            args.Player.SendSuccessMessage("修改了所有用户组。");
                            return;
                        }
                        try
                        {
                            string response = TShock.Groups.AddPermissions(groupName, args.Parameters);
                            if (response.Length > 0)
                            {
                                args.Player.SendSuccessMessage(response);
                            }
                            return;
                        }
                        catch (GroupManagerException ex)
                        {
                            args.Player.SendErrorMessage(ex.ToString());
                        }
                    }
                    #endregion
                    return;
                case "help":
                    #region Help
                    {
                        int pageNumber;
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                            return;

                        var lines = new List<string>
                        {
                            "add <名称> <权限...> - 添加新用户组。",
                            "addperm <用户组> <权限...> - 给指定用户组添加权限。",
                            "color <用户组> <rrr,ggg,bbb> - 改变用户组的对话颜色。",
                            "rename <用户组> <新组名> - 改变用户组名称。",
                            "del <用户组> - 删除用户组。",
                            "delperm <用户组> <权限...> - 移除指定用户组的权限。",
                            "list [页码] - 显示当前用户组列表。",
                            "listperm <用户组> [页码] - 显示指定用户组的所有权限。",
                            "parent <用户组> <父级组> - 改变指定用户组的父级组。",
                            "prefix <用户组> <前缀> - 改变指定用户组的前缀。",
                            "suffix <用户组> <后缀> - 改变指定用户组的后缀。"
                        };

                        PaginationTools.SendPage(args.Player, pageNumber, lines,
                            new PaginationTools.Settings
                            {
                                HeaderFormat = "用户组管理子指令 ({0}/{1}):",
                                FooterFormat = "输入 {0}group help {{0}} 以获取更多。".SFormat(Specifier)
                            }
                        );
                    }
                    #endregion
                    return;
                case "parent":
                    #region Parent
                    {
                        if (args.Parameters.Count < 2)
                        {
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}group parent <用户组> [新的父级组名]", Specifier);
                            return;
                        }

                        string groupName = args.Parameters[1];
                        Group group = TShock.Groups.GetGroupByName(groupName);
                        if (group == null)
                        {
                            args.Player.SendErrorMessage("没有用户组 \"{0}\".", groupName);
                            return;
                        }

                        if (args.Parameters.Count > 2)
                        {
                            string newParentGroupName = string.Join(" ", args.Parameters.Skip(2));
                            if (!string.IsNullOrWhiteSpace(newParentGroupName) && !TShock.Groups.GroupExists(newParentGroupName))
                            {
                                args.Player.SendErrorMessage("No such group \"{0}\".", newParentGroupName);
                                return;
                            }

                            try
                            {
                                TShock.Groups.UpdateGroup(groupName, newParentGroupName, group.Permissions, group.ChatColor, group.Suffix, group.Prefix);

                                if (!string.IsNullOrWhiteSpace(newParentGroupName))
                                    args.Player.SendSuccessMessage("设置组 \"{0}\" 的父级组为 \"{1}\".", groupName, newParentGroupName);
                                else
                                    args.Player.SendSuccessMessage("去除用户组 \"{0}\" 的父级组。", groupName);
                            }
                            catch (GroupManagerException ex)
                            {
                                args.Player.SendErrorMessage(ex.Message);
                            }
                        }
                        else
                        {
                            if (group.Parent != null)
                                args.Player.SendSuccessMessage(" \"{0}\" 的父级组是 \"{1}\".", group.Name, group.Parent.Name);
                            else
                                args.Player.SendSuccessMessage("用户组 \"{0}\" 没有父级组。", group.Name);
                        }
                    }
                    #endregion
                    return;
                case "suffix":
                    #region Suffix
                    {
                        if (args.Parameters.Count < 2)
                        {
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}group suffix <用户组> [新后缀]", Specifier);
                            return;
                        }

                        string groupName = args.Parameters[1];
                        Group group = TShock.Groups.GetGroupByName(groupName);
                        if (group == null)
                        {
                            args.Player.SendErrorMessage("没有用户组 \"{0}\".", groupName);
                            return;
                        }

                        if (args.Parameters.Count > 2)
                        {
                            string newSuffix = string.Join(" ", args.Parameters.Skip(2));

                            try
                            {
                                TShock.Groups.UpdateGroup(groupName, group.ParentName, group.Permissions, group.ChatColor, newSuffix, group.Prefix);

                                if (!string.IsNullOrWhiteSpace(newSuffix))
                                    args.Player.SendSuccessMessage("用户组 \"{0}\" 的后缀设置为 \"{1}\"。", groupName, newSuffix);
                                else
                                    args.Player.SendSuccessMessage("去除用户组 \"{0}\"的后缀。", groupName);
                            }
                            catch (GroupManagerException ex)
                            {
                                args.Player.SendErrorMessage(ex.Message);
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(group.Suffix))
                                args.Player.SendSuccessMessage(" \"{0}\" 组的后缀是 \"{1}\"。", group.Name, group.Suffix);
                            else
                                args.Player.SendSuccessMessage("用户组 \"{0}\" 没有后缀。", group.Name);
                        }
                    }
                    #endregion
                    return;
                case "prefix":
                    #region Prefix
                    {
                        if (args.Parameters.Count < 2)
                        {
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}group prefix <用户组> [新前缀]", Specifier);
                            return;
                        }

                        string groupName = args.Parameters[1];
                        Group group = TShock.Groups.GetGroupByName(groupName);
                        if (group == null)
                        {
                            args.Player.SendErrorMessage("没有用户组 \"{0}\".", groupName);
                            return;
                        }

                        if (args.Parameters.Count > 2)
                        {
                            string newPrefix = string.Join(" ", args.Parameters.Skip(2));

                            try
                            {
                                TShock.Groups.UpdateGroup(groupName, group.ParentName, group.Permissions, group.ChatColor, group.Suffix, newPrefix);

                                if (!string.IsNullOrWhiteSpace(newPrefix))
                                    args.Player.SendSuccessMessage("用户组 \"{0}\" 的前缀设置为 \"{1}\"。", groupName, newPrefix);
                                else
                                    args.Player.SendSuccessMessage("已去除用户组 \"{0}\" 的前缀。", groupName);
                            }
                            catch (GroupManagerException ex)
                            {
                                args.Player.SendErrorMessage(ex.Message);
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(group.Prefix))
                                args.Player.SendSuccessMessage(" \"{0}\" 的前缀为 \"{1}\"。", group.Name, group.Prefix);
                            else
                                args.Player.SendSuccessMessage("用户组 \"{0}\" 没有前缀。", group.Name);
                        }
                    }
                    #endregion
                    return;
                case "color":
                    #region Color
                    {
                        if (args.Parameters.Count < 2 || args.Parameters.Count > 3)
                        {
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}group color <用户组> [新颜色 (000,000,000)]", Specifier);
                            return;
                        }

                        string groupName = args.Parameters[1];
                        Group group = TShock.Groups.GetGroupByName(groupName);
                        if (group == null)
                        {
                            args.Player.SendErrorMessage("No such group \"{0}\".", groupName);
                            return;
                        }

                        if (args.Parameters.Count == 3)
                        {
                            string newColor = args.Parameters[2];

                            String[] parts = newColor.Split(',');
                            byte r;
                            byte g;
                            byte b;
                            if (parts.Length == 3 && byte.TryParse(parts[0], out r) && byte.TryParse(parts[1], out g) && byte.TryParse(parts[2], out b))
                            {
                                try
                                {
                                    TShock.Groups.UpdateGroup(groupName, group.ParentName, group.Permissions, newColor, group.Suffix, group.Prefix);

                                    args.Player.SendSuccessMessage("用户组 \"{0}\" 的颜色设置为 \"{1}\"。", groupName, newColor);
                                }
                                catch (GroupManagerException ex)
                                {
                                    args.Player.SendErrorMessage(ex.Message);
                                }
                            }
                            else
                            {
                                args.Player.SendErrorMessage("颜色的语法无效, 应为 \"rrr,ggg,bbb\"");
                            }
                        }
                        else
                        {
                            args.Player.SendSuccessMessage(" \"{0}\" 的颜色为 \"{1}\"。", group.Name, group.ChatColor);
                        }
                    }
                    #endregion
                    return;
                case "rename":
                    #region Rename group
                    {
                        if (args.Parameters.Count != 3)
                        {
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}group rename <用户组> <新名称>", Specifier);
                            return;
                        }

                        string group = args.Parameters[1];
                        string newName = args.Parameters[2];
                        try
                        {
                            string response = TShock.Groups.RenameGroup(group, newName);
                            args.Player.SendSuccessMessage(response);
                        }
                        catch (GroupManagerException ex)
                        {
                            args.Player.SendErrorMessage(ex.Message);
                        }
                    }
                    #endregion
                    return;
                case "del":
                    #region Delete group
                    {
                        if (args.Parameters.Count != 2)
                        {
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}group del <用户组>", Specifier);
                            return;
                        }

                        try
                        {
                            string response = TShock.Groups.DeleteGroup(args.Parameters[1], true);
                            if (response.Length > 0)
                            {
                                args.Player.SendSuccessMessage(response);
                            }
                        }
                        catch (GroupManagerException ex)
                        {
                            args.Player.SendErrorMessage(ex.Message);
                        }
                    }
                    #endregion
                    return;
                case "delperm":
                    #region Delete permissions
                    {
                        if (args.Parameters.Count < 3)
                        {
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}group delperm <用户组> <权限...>", Specifier);
                            return;
                        }

                        string groupName = args.Parameters[1];
                        args.Parameters.RemoveRange(0, 2);
                        if (groupName == "*")
                        {
                            foreach (Group g in TShock.Groups)
                            {
                                TShock.Groups.DeletePermissions(g.Name, args.Parameters);
                            }
                            args.Player.SendSuccessMessage("修改了所有用户组。");
                            return;
                        }
                        try
                        {
                            string response = TShock.Groups.DeletePermissions(groupName, args.Parameters);
                            if (response.Length > 0)
                            {
                                args.Player.SendSuccessMessage(response);
                            }
                            return;
                        }
                        catch (GroupManagerException ex)
                        {
                            args.Player.SendErrorMessage(ex.Message);
                        }
                    }
                    #endregion
                    return;
                case "list":
                    #region List groups
                    {
                        int pageNumber;
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                            return;
                        var groupNames = from grp in TShock.Groups.groups
                                         select grp.Name;
                        PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(groupNames),
                            new PaginationTools.Settings
                            {
                                HeaderFormat = "用户组 ({0}/{1}):",
                                FooterFormat = "输入 {0}group list {{0}} 以获得更多。".SFormat(Specifier)
                            });
                    }
                    #endregion
                    return;
                case "listperm":
                    #region List permissions
                    {
                        if (args.Parameters.Count == 1)
                        {
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}group listperm <用户组> [页面]", Specifier);
                            return;
                        }
                        int pageNumber;
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, 2, args.Player, out pageNumber))
                            return;

                        if (!TShock.Groups.GroupExists(args.Parameters[1]))
                        {
                            args.Player.SendErrorMessage("无效的用户组。");
                            return;
                        }
                        Group grp = TShock.Groups.GetGroupByName(args.Parameters[1]);
                        List<string> permissions = grp.TotalPermissions;

                        PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(permissions),
                            new PaginationTools.Settings
                            {
                                HeaderFormat = "用户组的权限 " + grp.Name + " ({0}/{1}):",
                                FooterFormat = "输入 {0}group listperm {1} {{0}} 以获得更多。".SFormat(Specifier, grp.Name),
                                NothingToDisplayString = "目前没有任何权限 " + grp.Name + "."
                            });
                    }
                    #endregion
                    return;
                default:
                    args.Player.SendErrorMessage("无效的子命令!输入 {0}group help 以获得有关有效命令的更多信息。", Specifier);
                    return;
            }
        }
        #endregion Group Management

        #region Item Management

        private static void ItemBan(CommandArgs args)
        {
            string subCmd = args.Parameters.Count == 0 ? "help" : args.Parameters[0].ToLower();
            switch (subCmd)
            {
                case "add":
                    #region Add item
                    {
                        if (args.Parameters.Count != 2)
                        {
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}itemban add <物品名>", Specifier);
                            return;
                        }

                        List<Item> items = TShock.Utils.GetItemByIdOrName(args.Parameters[1]);
                        if (items.Count == 0)
                        {
                            args.Player.SendErrorMessage("无效的物品。");
                        }
                        else if (items.Count > 1)
                        {
                            args.Player.SendMultipleMatchError(items.Select(i => $"{i.Name}({i.netID})"));
                        }
                        else
                        {
                            // Yes this is required because of localization
                            // User may have passed in localized name but itembans works on English names
                            string englishNameForStorage = EnglishLanguage.GetItemNameById(items[0].type);
                            TShock.ItemBans.DataModel.AddNewBan(englishNameForStorage);

                            // It was decided in Telegram that we would continue to ban
                            // projectiles based on whether or not their associated item was
                            // banned. However, it was also decided that we'd change the way
                            // this worked: in particular, we'd make it so that the item ban
                            // system just adds things to the projectile ban system at the
                            // command layer instead of inferring the state of projectile
                            // bans based on the state of the item ban system.

                            if (items[0].type == ItemID.DirtRod)
                            {
                                TShock.ProjectileBans.AddNewBan(ProjectileID.DirtBall);
                            }

                            if (items[0].type == ItemID.Sandgun)
                            {
                                TShock.ProjectileBans.AddNewBan(ProjectileID.SandBallGun);
                                TShock.ProjectileBans.AddNewBan(ProjectileID.EbonsandBallGun);
                                TShock.ProjectileBans.AddNewBan(ProjectileID.PearlSandBallGun);
                            }

                            // This returns the localized name to the player, not the item as it was stored.
                            args.Player.SendSuccessMessage("禁用了 " + items[0].Name + ".");
                        }
                    }
                    #endregion
                    return;
                case "allow":
                    #region Allow group to item
                    {
                        if (args.Parameters.Count != 3)
                        {
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}itemban allow <物品名> <用户组>", Specifier);
                            return;
                        }

                        List<Item> items = TShock.Utils.GetItemByIdOrName(args.Parameters[1]);
                        if (items.Count == 0)
                        {
                            args.Player.SendErrorMessage("无效的物品。");
                        }
                        else if (items.Count > 1)
                        {
                            args.Player.SendMultipleMatchError(items.Select(i => $"{i.Name}({i.netID})"));
                        }
                        else
                        {
                            if (!TShock.Groups.GroupExists(args.Parameters[2]))
                            {
                                args.Player.SendErrorMessage("无效的用户组。");
                                return;
                            }

                            ItemBan ban = TShock.ItemBans.DataModel.GetItemBanByName(EnglishLanguage.GetItemNameById(items[0].type));
                            if (ban == null)
                            {
                                args.Player.SendErrorMessage("{0} 没有被封禁。", items[0].Name);
                                return;
                            }
                            if (!ban.AllowedGroups.Contains(args.Parameters[2]))
                            {
                                TShock.ItemBans.DataModel.AllowGroup(EnglishLanguage.GetItemNameById(items[0].type), args.Parameters[2]);
                                args.Player.SendSuccessMessage("{0} 被允许使用 {1}.", args.Parameters[2], items[0].Name);
                            }
                            else
                            {
                                args.Player.SendWarningMessage("{0} 已经被允许使用 {1}.", args.Parameters[2], items[0].Name);
                            }
                        }
                    }
                    #endregion
                    return;
                case "del":
                    #region Delete item
                    {
                        if (args.Parameters.Count != 2)
                        {
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}itemban del <物品名>", Specifier);
                            return;
                        }

                        List<Item> items = TShock.Utils.GetItemByIdOrName(args.Parameters[1]);
                        if (items.Count == 0)
                        {
                            args.Player.SendErrorMessage("无效的物品。");
                        }
                        else if (items.Count > 1)
                        {
                            args.Player.SendMultipleMatchError(items.Select(i => $"{i.Name}({i.netID})"));
                        }
                        else
                        {
                            TShock.ItemBans.DataModel.RemoveBan(EnglishLanguage.GetItemNameById(items[0].type));
                            args.Player.SendSuccessMessage("解除了禁用 " + items[0].Name + ".");
                        }
                    }
                    #endregion
                    return;
                case "disallow":
                    #region Disllow group from item
                    {
                        if (args.Parameters.Count != 3)
                        {
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}itemban disallow <物品名> <用户组>", Specifier);
                            return;
                        }

                        List<Item> items = TShock.Utils.GetItemByIdOrName(args.Parameters[1]);
                        if (items.Count == 0)
                        {
                            args.Player.SendErrorMessage("无效的物品。");
                        }
                        else if (items.Count > 1)
                        {
                            args.Player.SendMultipleMatchError(items.Select(i => $"{i.Name}({i.netID})"));
                        }
                        else
                        {
                            if (!TShock.Groups.GroupExists(args.Parameters[2]))
                            {
                                args.Player.SendErrorMessage("无效的用户组。");
                                return;
                            }

                            ItemBan ban = TShock.ItemBans.DataModel.GetItemBanByName(EnglishLanguage.GetItemNameById(items[0].type));
                            if (ban == null)
                            {
                                args.Player.SendErrorMessage("{0} 没有被封禁。", items[0].Name);
                                return;
                            }
                            if (ban.AllowedGroups.Contains(args.Parameters[2]))
                            {
                                TShock.ItemBans.DataModel.RemoveGroup(EnglishLanguage.GetItemNameById(items[0].type), args.Parameters[2]);
                                args.Player.SendSuccessMessage("{0} 被禁止使用 {1}.", args.Parameters[2], items[0].Name);
                            }
                            else
                            {
                                args.Player.SendWarningMessage("{0} 已被禁止使用 {1}.", args.Parameters[2], items[0].Name);
                            }
                        }
                    }
                    #endregion
                    return;
                case "help":
                    #region Help
                    {
                        int pageNumber;
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                            return;

                        var lines = new List<string>
                        {
                            "add <物品名> - 添加一个物品禁令。",
                            "allow <物品名> <用户组> - 允许特定用户组使用物品。",
                            "del <物品名> - 删除物品禁令。",
                            "disallow <物品名> <用户组> - 禁止特定用户组使用物品。",
                            "list [页] - 列出所有物品禁令。"
                        };

                        PaginationTools.SendPage(args.Player, pageNumber, lines,
                            new PaginationTools.Settings
                            {
                                HeaderFormat = "项目禁令子命令 ({0}/{1}):",
                                FooterFormat = "输入 {0}itemban help {{0}} 以获得更多子命令。".SFormat(Specifier)
                            }
                        );
                    }
                    #endregion
                    return;
                case "list":
                    #region List items
                    {
                        int pageNumber;
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                            return;
                        IEnumerable<string> itemNames = from itemBan in TShock.ItemBans.DataModel.ItemBans
                                                        select itemBan.Name;
                        PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(itemNames),
                            new PaginationTools.Settings
                            {
                                HeaderFormat = "禁用物 ({0}/{1}):",
                                FooterFormat = "输入 {0}itemban list {{0}} 以获取更多。".SFormat(Specifier),
                                NothingToDisplayString = "当前没有被禁止的物品。"
                            });
                    }
                    #endregion
                    return;
                default:
                    #region Default
                    {
                        args.Player.SendErrorMessage("无效的子命令!输入 {0}itemban help ，以获取有关子命令的更多信息。", Specifier);
                    }
                    #endregion
                    return;

            }
        }
        #endregion Item Management

        #region Projectile Management

        private static void ProjectileBan(CommandArgs args)
        {
            string subCmd = args.Parameters.Count == 0 ? "help" : args.Parameters[0].ToLower();
            switch (subCmd)
            {
                case "add":
                    #region Add projectile
                    {
                        if (args.Parameters.Count != 2)
                        {
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}projban add <投射物ID>", Specifier);
                            return;
                        }
                        short id;
                        if (Int16.TryParse(args.Parameters[1], out id) && id > 0 && id < Main.maxProjectileTypes)
                        {
                            TShock.ProjectileBans.AddNewBan(id);
                            args.Player.SendSuccessMessage("禁用投射物 {0}.", id);
                        }
                        else
                            args.Player.SendErrorMessage("无效的投射物ID!");
                    }
                    #endregion
                    return;
                case "allow":
                    #region Allow group to projectile
                    {
                        if (args.Parameters.Count != 3)
                        {
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}projban allow <投射物ID> <用户组>", Specifier);
                            return;
                        }

                        short id;
                        if (Int16.TryParse(args.Parameters[1], out id) && id > 0 && id < Main.maxProjectileTypes)
                        {
                            if (!TShock.Groups.GroupExists(args.Parameters[2]))
                            {
                                args.Player.SendErrorMessage("无效的用户组。");
                                return;
                            }

                            ProjectileBan ban = TShock.ProjectileBans.GetBanById(id);
                            if (ban == null)
                            {
                                args.Player.SendErrorMessage("投射物 {0} 没有被禁止。", id);
                                return;
                            }
                            if (!ban.AllowedGroups.Contains(args.Parameters[2]))
                            {
                                TShock.ProjectileBans.AllowGroup(id, args.Parameters[2]);
                                args.Player.SendSuccessMessage("{0} 允许使用投射物 {1}.", args.Parameters[2], id);
                            }
                            else
                                args.Player.SendWarningMessage("{0} 已被允许使用投射物 {1}.", args.Parameters[2], id);
                        }
                        else
                            args.Player.SendErrorMessage("无效的投射物ID!");
                    }
                    #endregion
                    return;
                case "del":
                    #region Delete projectile
                    {
                        if (args.Parameters.Count != 2)
                        {
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}projban del <投射物ID>", Specifier);
                            return;
                        }

                        short id;
                        if (Int16.TryParse(args.Parameters[1], out id) && id > 0 && id < Main.maxProjectileTypes)
                        {
                            TShock.ProjectileBans.RemoveBan(id);
                            args.Player.SendSuccessMessage("解除投射物 {0} 的禁令。", id);
                            return;
                        }
                        else
                            args.Player.SendErrorMessage("无效的投射物 ID!");
                    }
                    #endregion
                    return;
                case "disallow":
                    #region Disallow group from projectile
                    {
                        if (args.Parameters.Count != 3)
                        {
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}projban disallow <投射物ID> <用户组>", Specifier);
                            return;
                        }

                        short id;
                        if (Int16.TryParse(args.Parameters[1], out id) && id > 0 && id < Main.maxProjectileTypes)
                        {
                            if (!TShock.Groups.GroupExists(args.Parameters[2]))
                            {
                                args.Player.SendErrorMessage("无效的用户组。");
                                return;
                            }

                            ProjectileBan ban = TShock.ProjectileBans.GetBanById(id);
                            if (ban == null)
                            {
                                args.Player.SendErrorMessage("投射物 {0} 没有被禁止。", id);
                                return;
                            }
                            if (ban.AllowedGroups.Contains(args.Parameters[2]))
                            {
                                TShock.ProjectileBans.RemoveGroup(id, args.Parameters[2]);
                                args.Player.SendSuccessMessage("{0} 禁止使用投射物 {1} 。", args.Parameters[2], id);
                                return;
                            }
                            else
                                args.Player.SendWarningMessage("{0} 已被禁止投射物 {1}。", args.Parameters[2], id);
                        }
                        else
                            args.Player.SendErrorMessage("无效的投射物 ID!");
                    }
                    #endregion
                    return;
                case "help":
                    #region Help
                    {
                        int pageNumber;
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                            return;

                        var lines = new List<string>
                        {
                            "add <投射物ID> - 添加投射物禁令。",
                            "allow <投射物ID> <用户组> - 允许用户组使用投射物。",
                            "del <投射物ID> - 解除投射物禁令。",
                            "disallow <投射物ID> <用户组> - 禁止组使用投射物。",
                            "list [页码] - 列出所有投射物禁令。"
                        };

                        PaginationTools.SendPage(args.Player, pageNumber, lines,
                            new PaginationTools.Settings
                            {
                                HeaderFormat = "投射物禁令子命令 ({0}/{1}):",
                                FooterFormat = "输入 {0}projban help {{0}} 以获得更多子命令。".SFormat(Specifier)
                            }
                        );
                    }
                    #endregion
                    return;
                case "list":
                    #region List projectiles
                    {
                        int pageNumber;
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                            return;
                        IEnumerable<Int16> projectileIds = from projectileBan in TShock.ProjectileBans.ProjectileBans
                                                           select projectileBan.ID;
                        PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(projectileIds),
                            new PaginationTools.Settings
                            {
                                HeaderFormat = "投射物禁令 ({0}/{1}):",
                                FooterFormat = "输入 {0}projban list {{0}} 以获得更多。".SFormat(Specifier),
                                NothingToDisplayString = "当前没有禁止的投射物。"
                            });
                    }
                    #endregion
                    return;
                default:
                    #region Default
                    {
                        args.Player.SendErrorMessage("无效的子命令!输入{0}projban help，以获取有关有效子命令的更多信息。", Specifier);
                    }
                    #endregion
                    return;
            }
        }
        #endregion Projectile Management

        #region Tile Management
        private static void TileBan(CommandArgs args)
        {
            string subCmd = args.Parameters.Count == 0 ? "help" : args.Parameters[0].ToLower();
            switch (subCmd)
            {
                case "add":
                    #region Add tile
                    {
                        if (args.Parameters.Count != 2)
                        {
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}tileban add <物块>", Specifier);
                            return;
                        }
                        short id;
                        if (Int16.TryParse(args.Parameters[1], out id) && id >= 0 && id < Main.maxTileSets)
                        {
                            TShock.TileBans.AddNewBan(id);
                            args.Player.SendSuccessMessage("禁止的物块 {0}.", id);
                        }
                        else
                            args.Player.SendErrorMessage("无效的物块 ID!");
                    }
                    #endregion
                    return;
                case "allow":
                    #region Allow group to place tile
                    {
                        if (args.Parameters.Count != 3)
                        {
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}tileban allow <物块ID> <用户组>", Specifier);
                            return;
                        }

                        short id;
                        if (Int16.TryParse(args.Parameters[1], out id) && id >= 0 && id < Main.maxTileSets)
                        {
                            if (!TShock.Groups.GroupExists(args.Parameters[2]))
                            {
                                args.Player.SendErrorMessage("无效的用户组。");
                                return;
                            }

                            TileBan ban = TShock.TileBans.GetBanById(id);
                            if (ban == null)
                            {
                                args.Player.SendErrorMessage("物块 {0} 没有被禁用。", id);
                                return;
                            }
                            if (!ban.AllowedGroups.Contains(args.Parameters[2]))
                            {
                                TShock.TileBans.AllowGroup(id, args.Parameters[2]);
                                args.Player.SendSuccessMessage("{0} 被允许放置物块 {1}.", args.Parameters[2], id);
                            }
                            else
                                args.Player.SendWarningMessage("{0} 已被允许放置物块 {1}.", args.Parameters[2], id);
                        }
                        else
                            args.Player.SendErrorMessage("无效的物块 ID!");
                    }
                    #endregion
                    return;
                case "del":
                    #region Delete tile ban
                    {
                        if (args.Parameters.Count != 2)
                        {
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}tileban del <物块ID>", Specifier);
                            return;
                        }

                        short id;
                        if (Int16.TryParse(args.Parameters[1], out id) && id >= 0 && id < Main.maxTileSets)
                        {
                            TShock.TileBans.RemoveBan(id);
                            args.Player.SendSuccessMessage("解除物块{0}的禁令。", id);
                            return;
                        }
                        else
                            args.Player.SendErrorMessage("无效的物块 ID!");
                    }
                    #endregion
                    return;
                case "disallow":
                    #region Disallow group from placing tile
                    {
                        if (args.Parameters.Count != 3)
                        {
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}tileban disallow <物块ID> <用户组>", Specifier);
                            return;
                        }

                        short id;
                        if (Int16.TryParse(args.Parameters[1], out id) && id >= 0 && id < Main.maxTileSets)
                        {
                            if (!TShock.Groups.GroupExists(args.Parameters[2]))
                            {
                                args.Player.SendErrorMessage("无效的用户组。");
                                return;
                            }

                            TileBan ban = TShock.TileBans.GetBanById(id);
                            if (ban == null)
                            {
                                args.Player.SendErrorMessage("物块 {0} 没有被禁用。", id);
                                return;
                            }
                            if (ban.AllowedGroups.Contains(args.Parameters[2]))
                            {
                                TShock.TileBans.RemoveGroup(id, args.Parameters[2]);
                                args.Player.SendSuccessMessage("{0} 被禁止放置图块 {1}.", args.Parameters[2], id);
                                return;
                            }
                            else
                                args.Player.SendWarningMessage("{0} 已被禁止放置图块 {1}.", args.Parameters[2], id);
                        }
                        else
                            args.Player.SendErrorMessage("无效的物块 ID!");
                    }
                    #endregion
                    return;
                case "help":
                    #region Help
                    {
                        int pageNumber;
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                            return;

                        var lines = new List<string>
                        {
                            "add <物块ID> - 添加物块禁令。",
                            "allow <物块ID> <用户组> - 允许用户组放置物块。",
                            "del <物块ID> - 删除物块禁令。",
                            "disallow <物块ID> <用户组> - 禁止用户组放置物块。",
                            "list [页] - 列出所有物块禁令。"
                        };

                        PaginationTools.SendPage(args.Player, pageNumber, lines,
                            new PaginationTools.Settings
                            {
                                HeaderFormat = "物块禁令子命令 ({0}/{1}):",
                                FooterFormat = "输入 {0}tileban help {{0}} 以获得更多子命令。".SFormat(Specifier)
                            }
                        );
                    }
                    #endregion
                    return;
                case "list":
                    #region List tile bans
                    {
                        int pageNumber;
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                            return;
                        IEnumerable<Int16> tileIds = from tileBan in TShock.TileBans.TileBans
                                                     select tileBan.ID;
                        PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(tileIds),
                            new PaginationTools.Settings
                            {
                                HeaderFormat = "物块禁令 ({0}/{1}):",
                                FooterFormat = "输入 {0}tileban list {{0}} 以获得更多信息。".SFormat(Specifier),
                                NothingToDisplayString = "当前没有禁止的物块。"
                            });
                    }
                    #endregion
                    return;
                default:
                    #region Default
                    {
                        args.Player.SendErrorMessage("无效的子命令!输入{0}tileban help以获取有关有效子命令的更多信息。", Specifier);
                    }
                    #endregion
                    return;
            }
        }
        #endregion Tile Management

        #region Server Config Commands

        private static void SetSpawn(CommandArgs args)
        {
            Main.spawnTileX = args.Player.TileX + 1;
            Main.spawnTileY = args.Player.TileY + 3;
            SaveManager.Instance.SaveWorld(false);
            args.Player.SendSuccessMessage("出生点已被设置到你当前的位置。");
        }

        private static void SetDungeon(CommandArgs args)
        {
            Main.dungeonX = args.Player.TileX + 1;
            Main.dungeonY = args.Player.TileY + 3;
            SaveManager.Instance.SaveWorld(false);
            args.Player.SendSuccessMessage("地牢点已被设置到你当前的位置。");
        }

        private static void Reload(CommandArgs args)
        {
            TShock.Utils.Reload();
            Hooks.GeneralHooks.OnReloadEvent(args.Player);

            args.Player.SendSuccessMessage(
                "配置，权限和区域重新加载完成。某些变更可能需要重新启动服务器。");
        }

        private static void ServerPassword(CommandArgs args)
        {
            if (args.Parameters.Count != 1)
            {
                args.Player.SendErrorMessage("无效的语法!正确的语法: {0}serverpassword \"<新密码>\"", Specifier);
                return;
            }
            string passwd = args.Parameters[0];
            TShock.Config.Settings.ServerPassword = passwd;
            args.Player.SendSuccessMessage(string.Format("服务器密码已更改为： {0}.", passwd));
        }

        private static void Save(CommandArgs args)
        {
            SaveManager.Instance.SaveWorld(false);
            foreach (TSPlayer tsply in TShock.Players.Where(tsply => tsply != null))
            {
                tsply.SaveServerCharacter();
            }
        }

        private static void Settle(CommandArgs args)
        {
            if (Liquid.panicMode)
            {
                args.Player.SendWarningMessage("液体已经平衡完毕!");
                return;
            }
            Liquid.StartPanic();
            args.Player.SendInfoMessage("正在平衡液体。");
        }

        private static void MaxSpawns(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendInfoMessage("当前最大生成量: {0}", TShock.Config.Settings.DefaultMaximumSpawns);
                return;
            }

            if (String.Equals(args.Parameters[0], "default", StringComparison.CurrentCultureIgnoreCase))
            {
                TShock.Config.Settings.DefaultMaximumSpawns = NPC.defaultMaxSpawns = 5;
                if (args.Silent)
                {
                    args.Player.SendInfoMessage("最大生成量更改为5。");
                }
                else
                {
                    TSPlayer.All.SendInfoMessage("{0} 将最大生成量更改为5。", args.Player.Name);
                }
                return;
            }

            int maxSpawns = -1;
            if (!int.TryParse(args.Parameters[0], out maxSpawns) || maxSpawns < 0 || maxSpawns > Main.maxNPCs)
            {
                args.Player.SendWarningMessage("无效的最大生成数量!范围是 {0} 到 {1}", 0, Main.maxNPCs);
                return;
            }

            TShock.Config.Settings.DefaultMaximumSpawns = NPC.defaultMaxSpawns = maxSpawns;
            if (args.Silent)
            {
                args.Player.SendInfoMessage("最大生成量更改为 {0}.", maxSpawns);
            }
            else
            {
                TSPlayer.All.SendInfoMessage("{0} 将最大生成量更改为 {1}.", args.Player.Name, maxSpawns);
            }
        }

        private static void SpawnRate(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendInfoMessage("当前生成量: {0}", TShock.Config.Settings.DefaultSpawnRate);
                return;
            }

            if (String.Equals(args.Parameters[0], "default", StringComparison.CurrentCultureIgnoreCase))
            {
                TShock.Config.Settings.DefaultSpawnRate = NPC.defaultSpawnRate = 600;
                if (args.Silent)
                {
                    args.Player.SendInfoMessage("生成量更改为 600.");
                }
                else
                {
                    TSPlayer.All.SendInfoMessage("{0} 将生成量更改为 600.", args.Player.Name);
                }
                return;
            }

            int spawnRate = -1;
            if (!int.TryParse(args.Parameters[0], out spawnRate) || spawnRate < 0)
            {
                args.Player.SendWarningMessage("无效的生成量!");
                return;
            }
            TShock.Config.Settings.DefaultSpawnRate = NPC.defaultSpawnRate = spawnRate;
            if (args.Silent)
            {
                args.Player.SendInfoMessage("生成量更改为 {0}.", spawnRate);
            }
            else
            {
                TSPlayer.All.SendInfoMessage("{0} 将生成量更改为 {1}.", args.Player.Name, spawnRate);
            }
        }

        #endregion Server Config Commands

        #region Time/PvpFun Commands

        private static void Time(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                double time = Main.time / 3600.0;
                time += 4.5;
                if (!Main.dayTime)
                    time += 15.0;
                time = time % 24.0;
                args.Player.SendInfoMessage("当前时间 {0}:{1:D2}.", (int)Math.Floor(time), (int)Math.Floor((time % 1.0) * 60.0));
                return;
            }

            switch (args.Parameters[0].ToLower())
            {
                case "day":
                    TSPlayer.Server.SetTime(true, 0.0);
                    TSPlayer.All.SendInfoMessage("{0} 把时间设置到 4:30.", args.Player.Name);
                    break;
                case "night":
                    TSPlayer.Server.SetTime(false, 0.0);
                    TSPlayer.All.SendInfoMessage("{0} 把时间设置到 19:30.", args.Player.Name);
                    break;
                case "noon":
                    TSPlayer.Server.SetTime(true, 27000.0);
                    TSPlayer.All.SendInfoMessage("{0} 把时间设置到 12:00.", args.Player.Name);
                    break;
                case "midnight":
                    TSPlayer.Server.SetTime(false, 16200.0);
                    TSPlayer.All.SendInfoMessage("{0} 把时间设置到 0:00.", args.Player.Name);
                    break;
                default:
                    string[] array = args.Parameters[0].Split(':');
                    if (array.Length != 2)
                    {
                        args.Player.SendErrorMessage("时间无效!正确格式: hh:mm, 以 24 小时制显示。");
                        return;
                    }

                    int hours;
                    int minutes;
                    if (!int.TryParse(array[0], out hours) || hours < 0 || hours > 23
                        || !int.TryParse(array[1], out minutes) || minutes < 0 || minutes > 59)
                    {
                        args.Player.SendErrorMessage("时间无效!正确格式: hh:mm, 以 24 小时制显示。");
                        return;
                    }

                    decimal time = hours + (minutes / 60.0m);
                    time -= 4.50m;
                    if (time < 0.00m)
                        time += 24.00m;

                    if (time >= 15.00m)
                    {
                        TSPlayer.Server.SetTime(false, (double)((time - 15.00m) * 3600.0m));
                    }
                    else
                    {
                        TSPlayer.Server.SetTime(true, (double)(time * 3600.0m));
                    }
                    TSPlayer.All.SendInfoMessage("{0} 把时间设置到 {1}:{2:D2}.", args.Player.Name, hours, minutes);
                    break;
            }
        }

        private static void Slap(CommandArgs args)
        {
            if (args.Parameters.Count < 1 || args.Parameters.Count > 2)
            {
                args.Player.SendErrorMessage("无效的语法!正确的语法: {0}击打 <player> [损伤]", Specifier);
                return;
            }
            if (args.Parameters[0].Length == 0)
            {
                args.Player.SendErrorMessage("无效用户!");
                return;
            }

            string plStr = args.Parameters[0];
            var players = TSPlayer.FindByNameOrID(plStr);
            if (players.Count == 0)
            {
                args.Player.SendErrorMessage("无效用户!");
            }
            else if (players.Count > 1)
            {
                args.Player.SendMultipleMatchError(players.Select(p => p.Name));
            }
            else
            {
                var plr = players[0];
                int damage = 5;
                if (args.Parameters.Count == 2)
                {
                    int.TryParse(args.Parameters[1], out damage);
                }
                if (!args.Player.HasPermission(Permissions.kill))
                {
                    damage = TShock.Utils.Clamp(damage, 15, 0);
                }
                plr.DamagePlayer(damage);
                TSPlayer.All.SendInfoMessage("{0}击打{1}造成了{2}点伤害。", args.Player.Name, plr.Name, damage);
                TShock.Log.Info("{0}击打{1}造成了{2}点伤害。", args.Player.Name, plr.Name, damage);
            }
        }

        private static void Wind(CommandArgs args)
        {
            if (args.Parameters.Count != 1)
            {
                args.Player.SendErrorMessage("无效的语法!正确的语法: {0}wind <风速>", Specifier);
                return;
            }

            int speed;
            if (!int.TryParse(args.Parameters[0], out speed) || speed * 100 < 0)
            {
                args.Player.SendErrorMessage("无效的风速!");
                return;
            }

            Main.windSpeedCurrent = speed;
            Main.windSpeedTarget = speed;
            TSPlayer.All.SendData(PacketTypes.WorldInfo);
            TSPlayer.All.SendInfoMessage("{0} 将风速改为 {1}.", args.Player.Name, speed);
        }

        #endregion Time/PvpFun Commands

        #region Region Commands

        private static void Region(CommandArgs args)
        {
            string cmd = "help";
            if (args.Parameters.Count > 0)
            {
                cmd = args.Parameters[0].ToLower();
            }
            switch (cmd)
            {
                case "name":
                    {
                        {
                            args.Player.SendInfoMessage("敲击一个物块以获取该区域获取名称。");
                            args.Player.AwaitingName = true;
                            args.Player.AwaitingNameParameters = args.Parameters.Skip(1).ToArray();
                        }
                        break;
                    }
                case "set":
                    {
                        int choice = 0;
                        if (args.Parameters.Count == 2 &&
                            int.TryParse(args.Parameters[1], out choice) &&
                            choice >= 1 && choice <= 2)
                        {
                            args.Player.SendInfoMessage("敲击一个物块以设定坐标 " + choice);
                            args.Player.AwaitingTempPoint = choice;
                        }
                        else
                        {
                            args.Player.SendErrorMessage("无效的语法!正确的语法: /region set <1/2>");
                        }
                        break;
                    }
                case "define":
                    {
                        if (args.Parameters.Count > 1)
                        {
                            if (!args.Player.TempPoints.Any(p => p == Point.Zero))
                            {
                                string regionName = String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1));
                                var x = Math.Min(args.Player.TempPoints[0].X, args.Player.TempPoints[1].X);
                                var y = Math.Min(args.Player.TempPoints[0].Y, args.Player.TempPoints[1].Y);
                                var width = Math.Abs(args.Player.TempPoints[0].X - args.Player.TempPoints[1].X);
                                var height = Math.Abs(args.Player.TempPoints[0].Y - args.Player.TempPoints[1].Y);

                                if (TShock.Regions.AddRegion(x, y, width, height, regionName, args.Player.Account.Name,
                                                             Main.worldID.ToString()))
                                {
                                    args.Player.TempPoints[0] = Point.Zero;
                                    args.Player.TempPoints[1] = Point.Zero;
                                    args.Player.SendInfoMessage("设定区域 " + regionName);
                                }
                                else
                                {
                                    args.Player.SendErrorMessage("区域 " + regionName + " 已经在用户组内。");
                                }
                            }
                            else
                            {
                                args.Player.SendErrorMessage("尚未设置坐标点。");
                            }
                        }
                        else
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}region define <区域名称>", Specifier);
                        break;
                    }
                case "protect":
                    {
                        if (args.Parameters.Count == 3)
                        {
                            string regionName = args.Parameters[1];
                            if (args.Parameters[2].ToLower() == "true")
                            {
                                if (TShock.Regions.SetRegionState(regionName, true))
                                    args.Player.SendInfoMessage("区域受到保护 " + regionName);
                                else
                                    args.Player.SendErrorMessage("找不到指定的区域。");
                            }
                            else if (args.Parameters[2].ToLower() == "false")
                            {
                                if (TShock.Regions.SetRegionState(regionName, false))
                                    args.Player.SendInfoMessage("区域未受保护 " + regionName);
                                else
                                    args.Player.SendErrorMessage("找不到指定的区域。");
                            }
                            else
                                args.Player.SendErrorMessage("无效的语法!正确的语法: {0}region protect <区域名称> <true/false>", Specifier);
                        }
                        else
                            args.Player.SendErrorMessage("无效的语法!正确的语法: /region protect <名区域名称> <true/false>", Specifier);
                        break;
                    }
                case "delete":
                    {
                        if (args.Parameters.Count > 1)
                        {
                            string regionName = String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1));
                            if (TShock.Regions.DeleteRegion(regionName))
                            {
                                args.Player.SendInfoMessage("删除区域 \"{0}\".", regionName);
                            }
                            else
                                args.Player.SendErrorMessage("找不到指定的区域!");
                        }
                        else
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}region delete <区域名称>", Specifier);
                        break;
                    }
                case "clear":
                    {
                        args.Player.TempPoints[0] = Point.Zero;
                        args.Player.TempPoints[1] = Point.Zero;
                        args.Player.SendInfoMessage("清除临时点。");
                        args.Player.AwaitingTempPoint = 0;
                        break;
                    }
                case "allow":
                    {
                        if (args.Parameters.Count > 2)
                        {
                            string playerName = args.Parameters[1];
                            string regionName = "";

                            for (int i = 2; i < args.Parameters.Count; i++)
                            {
                                if (regionName == "")
                                {
                                    regionName = args.Parameters[2];
                                }
                                else
                                {
                                    regionName = regionName + " " + args.Parameters[i];
                                }
                            }
                            if (TShock.UserAccounts.GetUserAccountByName(playerName) != null)
                            {
                                if (TShock.Regions.AddNewUser(regionName, playerName))
                                {
                                    args.Player.SendInfoMessage("添加" + playerName + "到" + regionName);
                                }
                                else
                                    args.Player.SendErrorMessage("区域" + regionName + "未找到");
                            }
                            else
                            {
                                args.Player.SendErrorMessage("用户" + playerName + "未找到");
                            }
                        }
                        else
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}region allow <用户名> <区域名称>", Specifier);
                        break;
                    }
                case "remove":
                    if (args.Parameters.Count > 2)
                    {
                        string playerName = args.Parameters[1];
                        string regionName = "";

                        for (int i = 2; i < args.Parameters.Count; i++)
                        {
                            if (regionName == "")
                            {
                                regionName = args.Parameters[2];
                            }
                            else
                            {
                                regionName = regionName + " " + args.Parameters[i];
                            }
                        }
                        if (TShock.UserAccounts.GetUserAccountByName(playerName) != null)
                        {
                            if (TShock.Regions.RemoveUser(regionName, playerName))
                            {
                                args.Player.SendInfoMessage("删除的用户" + playerName + " from " + regionName);
                            }
                            else
                                args.Player.SendErrorMessage("区域" + regionName + "未找到");
                        }
                        else
                        {
                            args.Player.SendErrorMessage("用户" + playerName + "未找到");
                        }
                    }
                    else
                        args.Player.SendErrorMessage("无效的语法!正确的语法: {0}region remove <用户名> <区域名称>", Specifier);
                    break;
                case "allowg":
                    {
                        if (args.Parameters.Count > 2)
                        {
                            string group = args.Parameters[1];
                            string regionName = "";

                            for (int i = 2; i < args.Parameters.Count; i++)
                            {
                                if (regionName == "")
                                {
                                    regionName = args.Parameters[2];
                                }
                                else
                                {
                                    regionName = regionName + " " + args.Parameters[i];
                                }
                            }
                            if (TShock.Groups.GroupExists(group))
                            {
                                if (TShock.Regions.AllowGroup(regionName, group))
                                {
                                    args.Player.SendInfoMessage("新增用户组" + group + "到" + regionName);
                                }
                                else
                                    args.Player.SendErrorMessage("区域" + regionName + "未找到");
                            }
                            else
                            {
                                args.Player.SendErrorMessage("用户组" + group + "未找到");
                            }
                        }
                        else
                            args.Player.SendErrorMessage("无效的语法!正确的语法:{0}region allowg <用户组> <区域名称>", Specifier);
                        break;
                    }
                case "removeg":
                    if (args.Parameters.Count > 2)
                    {
                        string group = args.Parameters[1];
                        string regionName = "";

                        for (int i = 2; i < args.Parameters.Count; i++)
                        {
                            if (regionName == "")
                            {
                                regionName = args.Parameters[2];
                            }
                            else
                            {
                                regionName = regionName + " " + args.Parameters[i];
                            }
                        }
                        if (TShock.Groups.GroupExists(group))
                        {
                            if (TShock.Regions.RemoveGroup(regionName, group))
                            {
                                args.Player.SendInfoMessage("移除用户组" + group + " from " + regionName);
                            }
                            else
                                args.Player.SendErrorMessage("用户组" + regionName + "未找到");
                        }
                        else
                        {
                            args.Player.SendErrorMessage("Group " + group + " not found");
                        }
                    }
                    else
                        args.Player.SendErrorMessage("无效的语法!正确的语法:{0}region removeg <用户组> <区域名称>", Specifier);
                    break;
                case "list":
                    {
                        int pageNumber;
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                            return;

                        IEnumerable<string> regionNames = from region in TShock.Regions.Regions
                                                          where region.WorldID == Main.worldID.ToString()
                                                          select region.Name;
                        PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(regionNames),
                            new PaginationTools.Settings
                            {
                                HeaderFormat = "区域 ({0}/{1}):",
                                FooterFormat = "输入 {0}region list {{0}} 以获得更多。".SFormat(Specifier),
                                NothingToDisplayString = "当前没有定义区域"
                            });
                        break;
                    }
                case "info":
                    {
                        if (args.Parameters.Count == 1 || args.Parameters.Count > 4)
                        {
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}region info <区域名称> [-d] [页]", Specifier);
                            break;
                        }

                        string regionName = args.Parameters[1];
                        bool displayBoundaries = args.Parameters.Skip(2).Any(
                            p => p.Equals("-d", StringComparison.InvariantCultureIgnoreCase)
                        );

                        Region region = TShock.Regions.GetRegionByName(regionName);
                        if (region == null)
                        {
                            args.Player.SendErrorMessage("区域{0}不存在", regionName);
                            break;
                        }

                        int pageNumberIndex = displayBoundaries ? 3 : 2;
                        int pageNumber;
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, pageNumberIndex, args.Player, out pageNumber))
                            break;

                        List<string> lines = new List<string>
                        {
                            string.Format("X: {0}; Y: {1}; W: {2}; H: {3}, Z: {4}", region.Area.X, region.Area.Y, region.Area.Width, region.Area.Height, region.Z),
                            string.Concat("所有者: ", region.Owner),
                            string.Concat("受保护的: ", region.DisableBuild.ToString()),
                        };

                        if (region.AllowedIDs.Count > 0)
                        {
                            IEnumerable<string> sharedUsersSelector = region.AllowedIDs.Select(userId =>
                            {
                                UserAccount account = TShock.UserAccounts.GetUserAccountByID(userId);
                                if (account != null)
                                    return account.Name;

                                return string.Concat("{ID: ", userId, "}");
                            });
                            List<string> extraLines = PaginationTools.BuildLinesFromTerms(sharedUsersSelector.Distinct());
                            extraLines[0] = "共享给: " + extraLines[0];
                            lines.AddRange(extraLines);
                        }
                        else
                        {
                            lines.Add("区域未与任何用户共享。");
                        }

                        if (region.AllowedGroups.Count > 0)
                        {
                            List<string> extraLines = PaginationTools.BuildLinesFromTerms(region.AllowedGroups.Distinct());
                            extraLines[0] = "共享的用户组: " + extraLines[0];
                            lines.AddRange(extraLines);
                        }
                        else
                        {
                            lines.Add("区域未与任何用户组共享。");
                        }

                        PaginationTools.SendPage(
                            args.Player, pageNumber, lines, new PaginationTools.Settings
                            {
                                HeaderFormat = string.Format("关于区域 \"{0}\" ({{0}}/{{1}}) 的信息:", region.Name),
                                FooterFormat = string.Format("输入 {0}region info {1} {{0}} 以获取更多信息。", Specifier, regionName)
                            }
                        );

                        if (displayBoundaries)
                        {
                            Rectangle regionArea = region.Area;
                            foreach (Point boundaryPoint in Utils.Instance.EnumerateRegionBoundaries(regionArea))
                            {
                                // Preferring dotted lines as those should easily be distinguishable from actual wires.
                                if ((boundaryPoint.X + boundaryPoint.Y & 1) == 0)
                                {
                                    // Could be improved by sending raw tile data to the client instead but not really
                                    // worth the effort as chances are very low that overwriting the wire for a few
                                    // nanoseconds will cause much trouble.
                                    ITile tile = Main.tile[boundaryPoint.X, boundaryPoint.Y];
                                    bool oldWireState = tile.wire();
                                    tile.wire(true);

                                    try
                                    {
                                        args.Player.SendTileSquare(boundaryPoint.X, boundaryPoint.Y, 1);
                                    }
                                    finally
                                    {
                                        tile.wire(oldWireState);
                                    }
                                }
                            }

                            Timer boundaryHideTimer = null;
                            boundaryHideTimer = new Timer((state) =>
                            {
                                foreach (Point boundaryPoint in Utils.Instance.EnumerateRegionBoundaries(regionArea))
                                    if ((boundaryPoint.X + boundaryPoint.Y & 1) == 0)
                                        args.Player.SendTileSquare(boundaryPoint.X, boundaryPoint.Y, 1);

                                Debug.Assert(boundaryHideTimer != null);
                                boundaryHideTimer.Dispose();
                            },
                                null, 5000, Timeout.Infinite
                            );
                        }

                        break;
                    }
                case "z":
                    {
                        if (args.Parameters.Count == 3)
                        {
                            string regionName = args.Parameters[1];
                            int z = 0;
                            if (int.TryParse(args.Parameters[2], out z))
                            {
                                if (TShock.Regions.SetZ(regionName, z))
                                    args.Player.SendInfoMessage("区域的z是 " + z);
                                else
                                    args.Player.SendErrorMessage("找不到指定的区域。");
                            }
                            else
                                args.Player.SendErrorMessage("无效的语法!正确的语法: {0}region z <区域名称> <数值>", Specifier);
                        }
                        else
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}region z <区域名称> <数值>", Specifier);
                        break;
                    }
                case "resize":
                case "expand":
                    {
                        if (args.Parameters.Count == 4)
                        {
                            int direction;
                            switch (args.Parameters[2])
                            {
                                case "u":
                                case "up":
                                    {
                                        direction = 0;
                                        break;
                                    }
                                case "r":
                                case "right":
                                    {
                                        direction = 1;
                                        break;
                                    }
                                case "d":
                                case "down":
                                    {
                                        direction = 2;
                                        break;
                                    }
                                case "l":
                                case "left":
                                    {
                                        direction = 3;
                                        break;
                                    }
                                default:
                                    {
                                        direction = -1;
                                        break;
                                    }
                            }
                            int addAmount;
                            int.TryParse(args.Parameters[3], out addAmount);
                            if (TShock.Regions.ResizeRegion(args.Parameters[1], addAmount, direction))
                            {
                                args.Player.SendInfoMessage("Region Resized Successfully!");
                                TShock.Regions.Reload();
                            }
                            else
                                args.Player.SendErrorMessage("无效的语法!正确的语法: {0}region resize <区域名称> <u(上)/d(下)/l(左)/r(右)> <数值>", Specifier);
                        }
                        else
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}region resize <区域名称> <u(上)/d(下)/l(左)/r(右)> <数值>", Specifier);
                        break;
                    }
                case "rename":
                    {
                        if (args.Parameters.Count != 3)
                        {
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}region rename <区域名称> <新名称>", Specifier);
                            break;
                        }
                        else
                        {
                            string oldName = args.Parameters[1];
                            string newName = args.Parameters[2];

                            if (oldName == newName)
                            {
                                args.Player.SendErrorMessage("错误:两个名称相同。");
                                break;
                            }

                            Region oldRegion = TShock.Regions.GetRegionByName(oldName);

                            if (oldRegion == null)
                            {
                                args.Player.SendErrorMessage("无效的区域 \"{0}\".", oldName);
                                break;
                            }

                            Region newRegion = TShock.Regions.GetRegionByName(newName);

                            if (newRegion != null)
                            {
                                args.Player.SendErrorMessage("区域 \"{0}\" 已存在。", newName);
                                break;
                            }

                            if (TShock.Regions.RenameRegion(oldName, newName))
                            {
                                args.Player.SendInfoMessage("区域重命名成功!");
                            }
                            else
                            {
                                args.Player.SendErrorMessage("区域重命名失败。");
                            }
                        }
                        break;
                    }
                case "tp":
                    {
                        if (!args.Player.HasPermission(Permissions.tp))
                        {
                            args.Player.SendErrorMessage("你无权进行传送。");
                            break;
                        }
                        if (args.Parameters.Count <= 1)
                        {
                            args.Player.SendErrorMessage("无效的语法!正确的语法: {0}region tp <区域名称>.", Specifier);
                            break;
                        }

                        string regionName = string.Join(" ", args.Parameters.Skip(1));
                        Region region = TShock.Regions.GetRegionByName(regionName);
                        if (region == null)
                        {
                            args.Player.SendErrorMessage("区域 \"{0}\" 不存在。", regionName);
                            break;
                        }

                        args.Player.Teleport(region.Area.Center.X * 16, region.Area.Center.Y * 16);
                        break;
                    }
                case "help":
                default:
                    {
                        int pageNumber;
                        int pageParamIndex = 0;
                        if (args.Parameters.Count > 1)
                            pageParamIndex = 1;
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, pageParamIndex, args.Player, out pageNumber))
                            return;

                        List<string> lines = new List<string> {
                          "set <1/2> - 设置临时区域点。",
                          "clear - 清除临时区域点。",
                          "define <区域名称> - 定义区域名称。",
                          "delete <区域名称> - 删除指定区域。",
                          "name [-u][-z][-p] - 显示给定点的区域名称。",
                          "rename <区域名称> <新的区域名称> - 重命名指定区域。",
                          "list - 列出所有区域。",
                          "resize <区域名称> <u(上)/d(下)/l(左)/r(右)> <数值> - 调整区域大小。",
                          "allow <用户名> <区域名称> - 允许用户进入区域。",
                          "remove <用户名> <区域名称> - 从区域中删除用户。",
                          "allowg <用户组> <区域名称> - 允许用户组进入区域。",
                          "removeg <用户组> <区域名称> - 从区域中移除用户组。",
                          "info <区域名称> [-d] - 显示有关给定区域的一些信息。",
                          "protect <区域名称> <true/false> - 设置区域内的图块是否受保护。",
                          "z <区域名称> <数值> - 设置区域的z顺序。",
                        };
                        if (args.Player.HasPermission(Permissions.tp))
                            lines.Add("tp <区域名称> - 将你传送到给定区域的中心。");

                        PaginationTools.SendPage(
                          args.Player, pageNumber, lines,
                          new PaginationTools.Settings
                          {
                              HeaderFormat = "可用的区域子命令 ({0}/{1}):",
                              FooterFormat = "输入 {0}region {{0}} 以获得更多子命令。".SFormat(Specifier)
                          }
                        );
                        break;
                    }
            }
        }

        #endregion Region Commands

        #region World Protection Commands

        private static void ToggleAntiBuild(CommandArgs args)
        {
            TShock.Config.Settings.DisableBuild = !TShock.Config.Settings.DisableBuild;
            TSPlayer.All.SendSuccessMessage(string.Format("建筑保护为 {0}.", (TShock.Config.Settings.DisableBuild ? "开启" : "关闭")));
        }

        private static void ProtectSpawn(CommandArgs args)
        {
            TShock.Config.Settings.SpawnProtection = !TShock.Config.Settings.SpawnProtection;
            TSPlayer.All.SendSuccessMessage(string.Format("出生点当前为 {0}.", (TShock.Config.Settings.SpawnProtection ? "受保护的" : "开放的")));
        }

        #endregion World Protection Commands

        #region General Commands

        private static void Help(CommandArgs args)
        {
            if (args.Parameters.Count > 1)
            {
                args.Player.SendErrorMessage("无效的语法!正确的语法: {0}help <命令/页码>", Specifier);
                return;
            }

            int pageNumber;
            if (args.Parameters.Count == 0 || int.TryParse(args.Parameters[0], out pageNumber))
            {
                if (!PaginationTools.TryParsePageNumber(args.Parameters, 0, args.Player, out pageNumber))
                {
                    return;
                }

                IEnumerable<string> cmdNames = from cmd in ChatCommands
                                               where cmd.CanRun(args.Player) && (cmd.Name != "setup" || TShock.SetupToken != 0)
                                               select Specifier + cmd.Name;

                PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(cmdNames),
                    new PaginationTools.Settings
                    {
                        HeaderFormat = "命令 ({0}/{1}):",
                        FooterFormat = "输入 {0}help {{0}} 以获取更多。".SFormat(Specifier)
                    });
            }
            else
            {
                string commandName = args.Parameters[0].ToLower();
                if (commandName.StartsWith(Specifier))
                {
                    commandName = commandName.Substring(1);
                }

                Command command = ChatCommands.Find(c => c.Names.Contains(commandName));
                if (command == null)
                {
                    args.Player.SendErrorMessage("无效命令。");
                    return;
                }
                if (!command.CanRun(args.Player))
                {
                    args.Player.SendErrorMessage("你无权访问此命令。");
                    return;
                }

                args.Player.SendSuccessMessage("{0}{1} 帮助: ", Specifier, command.Name);
                if (command.HelpDesc == null)
                {
                    args.Player.SendInfoMessage(command.HelpText);
                    return;
                }
                foreach (string line in command.HelpDesc)
                {
                    args.Player.SendInfoMessage(line);
                }
            }
        }

        private static void GetVersion(CommandArgs args)
        {
            args.Player.SendMessage($"TShock: {TShock.VersionNum.Color(Utils.BoldHighlight)} {TShock.VersionCodename.Color(Utils.RedHighlight)}.", Color.White);
        }

        private static void ListConnectedPlayers(CommandArgs args)
        {
            bool invalidUsage = (args.Parameters.Count > 2);

            bool displayIdsRequested = false;
            int pageNumber = 1;
            if (!invalidUsage)
            {
                foreach (string parameter in args.Parameters)
                {
                    if (parameter.Equals("-i", StringComparison.InvariantCultureIgnoreCase))
                    {
                        displayIdsRequested = true;
                        continue;
                    }

                    if (!int.TryParse(parameter, out pageNumber))
                    {
                        invalidUsage = true;
                        break;
                    }
                }
            }
            if (invalidUsage)
            {
                args.Player.SendMessage($"在线玩家列表", Color.White);
                args.Player.SendMessage($"{"playing".Color(Utils.BoldHighlight)} {"[-i]".Color(Utils.RedHighlight)} {"[page]".Color(Utils.GreenHighlight)}", Color.White);
                args.Player.SendMessage($"命令别名: {"playing".Color(Utils.GreenHighlight)}, {"online".Color(Utils.GreenHighlight)}, {"who".Color(Utils.GreenHighlight)}", Color.White);
                args.Player.SendMessage($"使用示例: {"who".Color(Utils.BoldHighlight)} {"-i".Color(Utils.RedHighlight)}", Color.White);
                return;
            }
            if (displayIdsRequested && !args.Player.HasPermission(Permissions.seeids))
            {
                args.Player.SendErrorMessage("你无权列出用户 ID。");
                return;
            }

            if (TShock.Utils.GetActivePlayerCount() == 0)
            {
                args.Player.SendMessage("目前没有在线玩家。", Color.White);
                return;
            }
            args.Player.SendMessage($"在线用 ({TShock.Utils.GetActivePlayerCount().Color(Utils.GreenHighlight)}/{TShock.Config.Settings.MaxSlots})", Color.White);

            var players = new List<string>();

            foreach (TSPlayer ply in TShock.Players)
            {
                if (ply != null && ply.Active)
                {
                    if (displayIdsRequested)
                        players.Add($"{ply.Name} (索引: {ply.Index}{(ply.Account != null ? ", Account ID: " + ply.Account.ID : "")})");
                    else
                        players.Add(ply.Name);
                }
            }

            PaginationTools.SendPage(
                args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(players),
                new PaginationTools.Settings
                {
                    IncludeHeader = false,
                    FooterFormat = $"输入 {Specifier}who {(displayIdsRequested ? "-i" : string.Empty)}{Specifier} 以获取更多。"
                }
            );
        }

        private static void SetupToken(CommandArgs args)
        {
            if (TShock.SetupToken == 0)
            {
                args.Player.SendWarningMessage("初始设置系统已禁用，该事件已被记录。");
                args.Player.SendWarningMessage("如果你无法使用所有管理员帐户，请寻求帮助于 https://tshock.co/");
                TShock.Log.Warn("{0} 尝试使用初始系统设置。", args.Player.IP);
                return;
            }

            // If the user account is already logged in, turn off the setup system
            if (args.Player.IsLoggedIn && args.Player.tempGroup == null)
            {
                args.Player.SendSuccessMessage("你的新帐户已通过验证，并且 {0}setup 设置系统已关闭。", Specifier);
                args.Player.SendSuccessMessage("分享你的服务器，与管理员交谈，并在 GitHub & Discord. -- https://tshock.co/");
                args.Player.SendSuccessMessage("感谢你使用 TShock for Terraria!");
                FileTools.CreateFile(Path.Combine(TShock.SavePath, "setup.lock"));
                File.Delete(Path.Combine(TShock.SavePath, "setup-code.txt"));
                TShock.SetupToken = 0;
                return;
            }

            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("你必须提供设置密码!");
                return;
            }

            int givenCode;
            if (!Int32.TryParse(args.Parameters[0], out givenCode) || givenCode != TShock.SetupToken)
            {
                args.Player.SendErrorMessage("初始设置密码错误，已被记录。");
                TShock.Log.Warn(args.Player.IP + " 尝试使用错误密码初始设置系统。");
                return;
            }

            if (args.Player.Group.Name != "superadmin")
                args.Player.tempGroup = new SuperAdminGroup();

            args.Player.SendInfoMessage("授予你临时系统访问权限，因此你可以运行相应命令。");
            args.Player.SendWarningMessage("请使用以下方法创建一个用户帐户。");
            args.Player.SendWarningMessage("{0}user add <用户名> <密码> owner", Specifier);
            args.Player.SendInfoMessage("创建的<用户名>，其密码为<password>，请牢记。");
            args.Player.SendInfoMessage("在此过程之后，请使用{0}login <用户名> <密码> 登录系统。", Specifier);
            args.Player.SendWarningMessage("如果你理解了，现在就可以请 {0}登录 <用户名> <密码> , 然后按 {0}setup.", Specifier);
            return;
        }

        private static void ThirdPerson(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("无效的语法!正确的语法: {0}me <文本>", Specifier);
                return;
            }
            if (args.Player.mute)
                args.Player.SendErrorMessage("你被禁言。");
            else
                TSPlayer.All.SendMessage(string.Format("*{0} {1}", args.Player.Name, String.Join(" ", args.Parameters)), 205, 133, 63);
        }

        private static void PartyChat(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("无效的语法!正确的语法: {0}p <团队聊天文本>", Specifier);
                return;
            }
            int playerTeam = args.Player.Team;

            if (args.Player.mute)
                args.Player.SendErrorMessage("你被禁言。");
            else if (playerTeam != 0)
            {
                string msg = string.Format("<{0}> {1}", args.Player.Name, String.Join(" ", args.Parameters));
                foreach (TSPlayer player in TShock.Players)
                {
                    if (player != null && player.Active && player.Team == playerTeam)
                        player.SendMessage(msg, Main.teamColor[playerTeam].R, Main.teamColor[playerTeam].G, Main.teamColor[playerTeam].B);
                }
            }
            else
                args.Player.SendErrorMessage("你没有在队伍中!");
        }

        private static void Mute(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendMessage("Mute Syntax", Color.White);
                args.Player.SendMessage($"{"mute".Color(Utils.BoldHighlight)} <{"player".Color(Utils.RedHighlight)}> [{"reason".Color(Utils.GreenHighlight)}]", Color.White);
                args.Player.SendMessage($"使用示例: {"mute".Color(Utils.BoldHighlight)} \"{args.Player.Name.Color(Utils.RedHighlight)}\" \"{"No swearing on my Christian server".Color(Utils.GreenHighlight)}\"", Color.White);
                args.Player.SendMessage($"在不广播聊天的情况下静音播放器，使用命令： {SilentSpecifier.Color(Utils.GreenHighlight)} instead of {Specifier.Color(Utils.RedHighlight)}", Color.White);
                return;
            }

            var players = TSPlayer.FindByNameOrID(args.Parameters[0]);
            if (players.Count == 0)
            {
                args.Player.SendErrorMessage($"无法找到用户名 \"{args.Parameters[0]}\"");
            }
            else if (players.Count > 1)
            {
                args.Player.SendMultipleMatchError(players.Select(p => p.Name));
            }
            else if (players[0].HasPermission(Permissions.mute))
            {
                args.Player.SendErrorMessage($"你不能禁言用户 {players[0].Name}");
            }
            else if (players[0].mute)
            {
                var plr = players[0];
                plr.mute = false;
                if (args.Silent)
                    args.Player.SendSuccessMessage($"你已被撤销禁言 {plr.Name}.");
                else
                    TSPlayer.All.SendInfoMessage($"{args.Player.Name} 已被 {plr.Name} 取消禁言。");
            }
            else
            {
                string reason = "没有原因。";
                if (args.Parameters.Count > 1)
                    reason = String.Join(" ", args.Parameters.ToArray(), 1, args.Parameters.Count - 1);
                var plr = players[0];
                plr.mute = true;
                if (args.Silent)
                    args.Player.SendSuccessMessage($"你已被禁言 {plr.Name} ，原因： {reason}");
                else
                    TSPlayer.All.SendInfoMessage($"{args.Player.Name} 已被 {plr.Name} 禁言，原因： {reason}.");
            }
        }

        private static void Motd(CommandArgs args)
        {
            args.Player.SendFileTextAsMessage(FileTools.MotdPath);
        }

        private static void Rules(CommandArgs args)
        {
            args.Player.SendFileTextAsMessage(FileTools.RulesPath);
        }

        public static void Whisper(CommandArgs args)
        {
            if (args.Parameters.Count < 2)
            {
                args.Player.SendMessage("私聊语法", Color.White);
                args.Player.SendMessage($"{"私聊".Color(Utils.BoldHighlight)} <{"player".Color(Utils.RedHighlight)}> <{"message".Color(Utils.PinkHighlight)}>", Color.White);
                args.Player.SendMessage($"使用示例: {"w".Color(Utils.BoldHighlight)} {args.Player.Name.Color(Utils.RedHighlight)} {"We're no strangers to love, you know the rules, and so do I.".Color(Utils.PinkHighlight)}", Color.White);
                return;
            }
            var players = TSPlayer.FindByNameOrID(args.Parameters[0]);
            if (players.Count == 0)
            {
                args.Player.SendErrorMessage($"找不到此用户名 \"{args.Parameters[0]}\"");
            }
            else if (players.Count > 1)
            {
                args.Player.SendMultipleMatchError(players.Select(p => p.Name));
            }
            else if (args.Player.mute)
            {
                args.Player.SendErrorMessage("你被禁言了。");
            }
            else
            {
                var plr = players[0];

                if (!plr.AcceptingWhispers)
                {
                    args.Player.SendErrorMessage($"{plr.Name} 没有接受私聊。");
                    return;
                }

                var msg = string.Join(" ", args.Parameters.ToArray(), 1, args.Parameters.Count - 1);
                //旁白
                if (plr == args.Player)
                {
                    args.Player.SendMessage($"[旁白] {msg}", Color.MediumPurple);
                    return;
                }
                plr.SendMessage($"<From {args.Player.Name}> {msg}", Color.MediumPurple);
                args.Player.SendMessage($"<给 {plr.Name}> {msg}", Color.MediumPurple);
                plr.LastWhisper = args.Player;
                args.Player.LastWhisper = plr;
            }
        }

        private static void Wallow(CommandArgs args)
        {
            args.Player.AcceptingWhispers = !args.Player.AcceptingWhispers;
            args.Player.SendSuccessMessage($"你 {(args.Player.AcceptingWhispers ? "may now" : "will no longer")} 接收到了来自其他玩家的私聊。");
            args.Player.SendMessage($"你可以使用 {Specifier.Color(Utils.GreenHighlight)}{"wa".Color(Utils.GreenHighlight)} 切换此设置。", Color.White);
        }

        private static void Reply(CommandArgs args)
        {
            if (args.Player.mute)
            {
                args.Player.SendErrorMessage("你被禁言了。");
            }
            else if (args.Player.LastWhisper != null && args.Player.LastWhisper.Active)
            {
                if (!args.Player.LastWhisper.AcceptingWhispers)
                {
                    args.Player.SendErrorMessage($"{args.Player.LastWhisper.Name} 没有接受私聊。");
                    return;
                }
                var msg = string.Join(" ", args.Parameters);
                args.Player.LastWhisper.SendMessage($"<From {args.Player.Name}> {msg}", Color.MediumPurple);
                args.Player.SendMessage($"<给 {args.Player.LastWhisper.Name}> {msg}", Color.MediumPurple);
            }
            else if (args.Player.LastWhisper != null)
            {
                args.Player.SendErrorMessage($"{args.Player.LastWhisper.Name} 离线，无法收到您的回复。");
            }
            else
            {
                args.Player.SendErrorMessage("你之前没有收到任何私聊。");
                args.Player.SendMessage($"您可以使用 {Specifier.Color(Utils.GreenHighlight)}{"w".Color(Utils.GreenHighlight)} 来向其他玩家发起私聊。", Color.White);
            }
        }

        private static void Annoy(CommandArgs args)
        {
            if (args.Parameters.Count != 2)
            {
                args.Player.SendMessage("骚扰语法", Color.White);
                args.Player.SendMessage($"{"annoy".Color(Utils.BoldHighlight)} <{"player".Color(Utils.RedHighlight)}> <{"seconds".Color(Utils.PinkHighlight)}>", Color.White);
                args.Player.SendMessage($"使用示例: {"annoy".Color(Utils.BoldHighlight)} <{args.Player.Name.Color(Utils.RedHighlight)}> <{"10".Color(Utils.PinkHighlight)}>", Color.White);
                args.Player.SendMessage($"你可以使用 {SilentSpecifier.Color(Utils.GreenHighlight)} 而不是 {Specifier.Color(Utils.RedHighlight)} 无声地惹恼一个玩家。", Color.White);
                return;
            }
            int annoy = 5;
            int.TryParse(args.Parameters[1], out annoy);

            var players = TSPlayer.FindByNameOrID(args.Parameters[0]);
            if (players.Count == 0)
                args.Player.SendErrorMessage($"找不到指定的玩家 \"{args.Parameters[0]}\"");
            else if (players.Count > 1)
                args.Player.SendMultipleMatchError(players.Select(p => p.Name));
            else
            {
                var ply = players[0];
                args.Player.SendSuccessMessage($"Annoying {ply.Name} for {annoy} seconds.");
                if (!args.Silent)
                    ply.SendMessage("你现在被惹恼了.", Color.LightGoldenrodYellow);
                new Thread(ply.Whoopie).Start(annoy);
            }
        }

        private static void Rocket(CommandArgs args)
        {
            if (args.Parameters.Count != 1)
            {
                args.Player.SendMessage("火箭语法", Color.White);
                args.Player.SendMessage($"{"rocket".Color(Utils.BoldHighlight)} <{"player".Color(Utils.RedHighlight)}>", Color.White);
                args.Player.SendMessage($"使用示例: {"rocket".Color(Utils.BoldHighlight)} {args.Player.Name.Color(Utils.RedHighlight)}", Color.White);
                args.Player.SendMessage($"你可以使用 {SilentSpecifier.Color(Utils.GreenHighlight)} 而不是 {Specifier.Color(Utils.RedHighlight)} 无声地让一个玩家升天。", Color.White);
                return;
            }
            var players = TSPlayer.FindByNameOrID(args.Parameters[0]);
            if (players.Count == 0)
                args.Player.SendErrorMessage($"无效用户 \"{args.Parameters[0]}\"");
            else if (players.Count > 1)
                args.Player.SendMultipleMatchError(players.Select(p => p.Name));
            else
            {
                var target = players[0];

                if (target.IsLoggedIn && Main.ServerSideCharacter)
                {
                    target.TPlayer.velocity.Y = -50;
                    TSPlayer.All.SendData(PacketTypes.PlayerUpdate, "", target.Index);

                    if (!args.Silent)
                    {
                        TSPlayer.All.SendInfoMessage($"{args.Player.Name} 已经启动了 {(target == args.Player ? (args.Player.TPlayer.Male ? "himself" : "herself") : target.Name)} 到了太空。");
                        return;
                    }

                    if (target == args.Player)
                        args.Player.SendSuccessMessage("你把自己送上了太空。");
                    else
                        args.Player.SendSuccessMessage($"你把 {target.Name} 送上了太空。");
                }
                else
                {
                    if (!Main.ServerSideCharacter)
                        args.Player.SendErrorMessage("必须启用SSC才能使用该命令。");
                    else
                        args.Player.SendErrorMessage($"不能使用火箭 {target.Name} 因为 {(target.TPlayer.Male ? "he" : "she")} 未登录。");
                }
            }
        }

        private static void FireWork(CommandArgs args)
        {
            var user = args.Player;
            if (args.Parameters.Count < 1)
            {
                // firework <player> [R|G|B|Y]
                user.SendMessage("烟花语法", Color.White);
                user.SendMessage($"{"firework".Color(Utils.CyanHighlight)} <{"player".Color(Utils.PinkHighlight)}> [{"R".Color(Utils.RedHighlight)}|{"G".Color(Utils.GreenHighlight)}|{"B".Color(Utils.BoldHighlight)}|{"Y".Color(Utils.YellowHighlight)}]", Color.White);
                user.SendMessage($"使用示例: {"firework".Color(Utils.CyanHighlight)} {user.Name.Color(Utils.PinkHighlight)} {"R".Color(Utils.RedHighlight)}", Color.White);
                user.SendMessage($"你可以使用 {SilentSpecifier.Color(Utils.GreenHighlight)} 而不是 {Specifier.Color(Utils.RedHighlight)} 来悄无声息的发射烟花。", Color.White);
                return;
            }
            var players = TSPlayer.FindByNameOrID(args.Parameters[0]);
            if (players.Count == 0)
                user.SendErrorMessage($"无效用户 \"{args.Parameters[0]}\"");
            else if (players.Count > 1)
                user.SendMultipleMatchError(players.Select(p => p.Name));
            else
            {
                int type = ProjectileID.RocketFireworkRed;
                if (args.Parameters.Count > 1)
                {
                    switch (args.Parameters[1].ToLower())
                    {
                        case "red":
                        case "r":
                            type = ProjectileID.RocketFireworkRed;
                            break;
                        case "green":
                        case "g":
                            type = ProjectileID.RocketFireworkGreen;
                            break;
                        case "blue":
                        case "b":
                            type = ProjectileID.RocketFireworkBlue;
                            break;
                        case "yellow":
                        case "y":
                            type = ProjectileID.RocketFireworkYellow;
                            break;
                        case "r2":
                        case "star":
                            type = ProjectileID.RocketFireworksBoxRed;
                            break;
                        case "g2":
                        case "spiral":
                            type = ProjectileID.RocketFireworksBoxGreen;
                            break;
                        case "b2":
                        case "rings":
                            type = ProjectileID.RocketFireworksBoxBlue;
                            break;
                        case "y2":
                        case "flower":
                            type = ProjectileID.RocketFireworksBoxYellow;
                            break;
                        default:
                            type = ProjectileID.RocketFireworkRed;
                            break;
                    }
                }
                var target = players[0];
                int p = Projectile.NewProjectile(Projectile.GetNoneSource(), target.TPlayer.position.X, target.TPlayer.position.Y - 64f, 0f, -8f, type, 0, 0);
                Main.projectile[p].Kill();
                args.Player.SendSuccessMessage($"你在 {(target == user ? "yourself" : target.Name)} 放了烟花。");
                if (!args.Silent && target != user)
                    target.SendSuccessMessage($"{user.Name} 在你面前放了烟花。");
            }
        }

        private static void Aliases(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("无效的语法!正确的语法: {0}aliases <命令或别名>", Specifier);
                return;
            }

            string givenCommandName = string.Join(" ", args.Parameters);
            if (string.IsNullOrWhiteSpace(givenCommandName))
            {
                args.Player.SendErrorMessage("请输入正确的命令名称或别名。");
                return;
            }

            string commandName;
            if (givenCommandName[0] == Specifier[0])
                commandName = givenCommandName.Substring(1);
            else
                commandName = givenCommandName;

            bool didMatch = false;
            foreach (Command matchingCommand in ChatCommands.Where(cmd => cmd.Names.IndexOf(commandName) != -1))
            {
                if (matchingCommand.Names.Count > 1)
                    args.Player.SendInfoMessage(
                        " {0}{1}: {0}{2} 的别名", Specifier, matchingCommand.Name, string.Join(", {0}".SFormat(Specifier), matchingCommand.Names.Skip(1)));
                else
                    args.Player.SendInfoMessage("{0}{1} 没有定义别名。", Specifier, matchingCommand.Name);

                didMatch = true;
            }

            if (!didMatch)
                args.Player.SendErrorMessage("找不到与 \"{0}\" 相匹配的命令或命令别名。", givenCommandName);
        }

        private static void CreateDumps(CommandArgs args)
        {
            TShock.Utils.DumpPermissionMatrix("PermissionMatrix.txt");
            TShock.Utils.Dump(false);
            args.Player.SendSuccessMessage("参考转储文件已在服务器对应文件夹中创建。");
            return;
        }

        private static void SyncLocalArea(CommandArgs args)
        {
            args.Player.SendTileSquare((int)args.Player.TileX, (int)args.Player.TileY, 32);
            args.Player.SendWarningMessage("已同步!");
            return;
        }

        #endregion General Commands

        #region Game Commands

        private static void Clear(CommandArgs args)
        {
            var user = args.Player;
            var everyone = TSPlayer.All;
            int radius = 50;

            if (args.Parameters.Count != 1 && args.Parameters.Count != 2)
            {
                user.SendMessage("清除语法", Color.White);
                user.SendMessage($"{"clear".Color(Utils.BoldHighlight)} <{"item".Color(Utils.GreenHighlight)}|{"npc".Color(Utils.RedHighlight)}|{"projectile".Color(Utils.YellowHighlight)}> [{"radius".Color(Utils.PinkHighlight)}]", Color.White);
                user.SendMessage($"使用示例: {"clear".Color(Utils.BoldHighlight)} {"i".Color(Utils.RedHighlight)} {"10000".Color(Utils.GreenHighlight)}", Color.White); user.SendMessage($"使用示例: {"clear".Color(Utils.BoldHighlight)} {"item".Color(Utils.RedHighlight)} {"10000".Color(Utils.GreenHighlight)}", Color.White);
                user.SendMessage($"如果你没有指定一个半径， 它将在使用一个默认的半径 {radius} 你的人物周围。", Color.White);
                user.SendMessage($"你可以使用 {SilentSpecifier.Color(Utils.GreenHighlight)} 而不是 {Specifier.Color(Utils.RedHighlight)} 静默执行该命令。", Color.White);
                return;
            }

            if (args.Parameters.Count == 2)
            {
                if (!int.TryParse(args.Parameters[1], out radius) || radius <= 0)
                {
                    user.SendErrorMessage($"\"{args.Parameters[1]}\" 是无效的半径。");
                    return;
                }
            }

            switch (args.Parameters[0].ToLower())
            {
                case "item":
                case "items":
                case "i":
                    {
                        int cleared = 0;
                        for (int i = 0; i < Main.maxItems; i++)
                        {
                            float dX = Main.item[i].position.X - user.X;
                            float dY = Main.item[i].position.Y - user.Y;

                            if (Main.item[i].active && dX * dX + dY * dY <= radius * radius * 256f)
                            {
                                Main.item[i].active = false;
                                everyone.SendData(PacketTypes.ItemDrop, "", i);
                                cleared++;
                            }
                        }
                        if (args.Silent)
                            user.SendSuccessMessage($"你已清除 {cleared} 物品 {(cleared > 1 ? "s" : "")} 在半径 {radius} 内。");
                        else
                            everyone.SendInfoMessage($"{user.Name} 清除了 {cleared} 物品 {(cleared > 1 ? "s" : "")} 在半径 {radius} 内。");
                    }
                    break;
                case "npc":
                case "npcs":
                case "n":
                    {
                        int cleared = 0;
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            float dX = Main.npc[i].position.X - user.X;
                            float dY = Main.npc[i].position.Y - user.Y;

                            if (Main.npc[i].active && dX * dX + dY * dY <= radius * radius * 256f)
                            {
                                Main.npc[i].active = false;
                                Main.npc[i].type = 0;
                                everyone.SendData(PacketTypes.NpcUpdate, "", i);
                                cleared++;
                            }
                        }
                        if (args.Silent)
                            user.SendSuccessMessage($"你已清除 {cleared} NPC{(cleared > 1 ? "s" : "")} 在半径 {radius} 内。");
                        else
                            everyone.SendInfoMessage($"{user.Name} 清除 {cleared} NPC{(cleared > 1 ? "s" : "")} 在半径 {radius} 内。");
                    }
                    break;
                case "proj":
                case "projectile":
                case "projectiles":
                case "p":
                    {
                        int cleared = 0;
                        for (int i = 0; i < Main.maxProjectiles; i++)
                        {
                            float dX = Main.projectile[i].position.X - user.X;
                            float dY = Main.projectile[i].position.Y - user.Y;

                            if (Main.projectile[i].active && dX * dX + dY * dY <= radius * radius * 256f)
                            {
                                Main.projectile[i].active = false;
                                Main.projectile[i].type = 0;
                                everyone.SendData(PacketTypes.ProjectileNew, "", i);
                                cleared++;
                            }
                        }
                        if (args.Silent)
                            user.SendSuccessMessage($"你已清除 {cleared} 弹幕 {(cleared > 1 ? "s" : "")} 在半径 {radius} 内。");
                        else
                            everyone.SendInfoMessage($"{user.Name} 清除 {cleared} 弹幕 {(cleared > 1 ? "s" : "")} 在半径 {radius} 内。");
                    }
                    break;
                default:
                    user.SendErrorMessage($"\"{args.Parameters[0]}\" 是无效的清除选项!");
                    break;
            }
        }

        private static void Kill(CommandArgs args)
        {
            // To-Do: separate kill self and kill other player into two permissions
            var user = args.Player;
            if (args.Parameters.Count < 1)
            {
                user.SendMessage("击杀语法 及 示例", Color.White);
                user.SendMessage($"{"kill".Color(Utils.BoldHighlight)} <{"player".Color(Utils.RedHighlight)}>", Color.White);
                user.SendMessage($"使用示例: {"kill".Color(Utils.BoldHighlight)} {user.Name.Color(Utils.RedHighlight)}", Color.White);
                user.SendMessage($"你可以使用 {SilentSpecifier.Color(Utils.GreenHighlight)} 而不是 {Specifier.Color(Utils.RedHighlight)} 悄无声息地执行该命令。", Color.White);
                return;
            }

            string targetName = String.Join(" ", args.Parameters);
            var players = TSPlayer.FindByNameOrID(targetName);

            if (players.Count == 0)
                user.SendErrorMessage($"无效用户 \"{targetName}\"。");
            else if (players.Count > 1)
                user.SendMultipleMatchError(players.Select(p => p.Name));
            else
            {
                var target = players[0];

                if (target.Dead)
                {
                    user.SendErrorMessage($"{(target == user ? "You" : target.Name)} {(target == user ? "are" : "is")} 已经寄了！");
                    return;
                }
                target.KillPlayer();
                user.SendSuccessMessage($"你被 {(target == user ? " 你自己个儿噶了" : target.Name)}!");
                if (!args.Silent && target != user)
                    target.SendErrorMessage($"{user.Name} 噶了你！");
            }
        }

        private static void Respawn(CommandArgs args)
        {
            if (!args.Player.RealPlayer && args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("你不能重置服务器控制台!");
                return;
            }
            TSPlayer playerToRespawn;
            if (args.Parameters.Count > 0)
            {
                if (!args.Player.HasPermission(Permissions.respawnother))
                {
                    args.Player.SendErrorMessage("你没有重生其他玩家的权限。");
                    return;
                }
                string plStr = String.Join(" ", args.Parameters);
                var players = TSPlayer.FindByNameOrID(plStr);
                if (players.Count == 0)
                {
                    args.Player.SendErrorMessage($"无效用户 \"{plStr}\"");
                    return;
                }
                if (players.Count > 1)
                {
                    args.Player.SendMultipleMatchError(players.Select(p => p.Name));
                    return;
                }
                playerToRespawn = players[0];
            }
            else
                playerToRespawn = args.Player;

            if (!playerToRespawn.Dead)
            {
                args.Player.SendErrorMessage($"{(playerToRespawn == args.Player ? "你" : playerToRespawn.Name)} {(playerToRespawn == args.Player ? "are" : "is")} 没死。");
                return;
            }
            playerToRespawn.Spawn(PlayerSpawnContext.ReviveFromDeath);

            if (playerToRespawn != args.Player)
            {
                args.Player.SendSuccessMessage($"你已经重生了 {playerToRespawn.Name}");
                if (!args.Silent)
                    playerToRespawn.SendSuccessMessage($"{args.Player.Name} 复活了你。");
            }
            else
                playerToRespawn.SendSuccessMessage("你重获新生！你应得的！");
        }

        private static void Butcher(CommandArgs args)
        {
            var user = args.Player;
            if (args.Parameters.Count > 1)
            {
                user.SendMessage("屠夫的语法 和 示例", Color.White);
                user.SendMessage($"{"butcher".Color(Utils.BoldHighlight)} [{"NPC name".Color(Utils.RedHighlight)}|{"ID".Color(Utils.RedHighlight)}]", Color.White);
                user.SendMessage($"使用示例: {"butcher".Color(Utils.BoldHighlight)} {"pigron".Color(Utils.RedHighlight)}", Color.White);
                user.SendMessage("如果你不输入名字或ID，服务器上所有活着的NPC(不包括城镇npc)将被杀死。", Color.White);
                user.SendMessage($"为了释放NPC而不让他们掉落物品，使用 {"clear".Color(Utils.BoldHighlight)} 命令来替代。", Color.White);
                user.SendMessage($"如要悄无声息地执行此命令，请使用 {SilentSpecifier.Color(Utils.GreenHighlight)} 而不是 {Specifier.Color(Utils.RedHighlight)}", Color.White);
                return;
            }

            int npcId = 0;

            if (args.Parameters.Count == 1)
            {
                var npcs = TShock.Utils.GetNPCByIdOrName(args.Parameters[0]);
                if (npcs.Count == 0)
                {
                    user.SendErrorMessage($"\"{args.Parameters[0]}\" 是无效的NPC.");
                    return;
                }

                if (npcs.Count > 1)
                {
                    user.SendMultipleMatchError(npcs.Select(n => $"{n.FullName}({n.type})"));
                    return;
                }
                npcId = npcs[0].netID;
            }

            int kills = 0;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if (Main.npc[i].active && ((npcId == 0 && !Main.npc[i].townNPC && Main.npc[i].netID != NPCID.TargetDummy) || Main.npc[i].netID == npcId))
                {
                    TSPlayer.Server.StrikeNPC(i, (int)(Main.npc[i].life + (Main.npc[i].defense * 0.6)), 0, 0);
                    kills++;
                }
            }

            if (args.Silent)
                user.SendSuccessMessage($"你屠鲨了 {kills} NPC{(kills > 1 ? "s" : "")}。");
            else
                TSPlayer.All.SendInfoMessage($"{user.Name} 被屠鲨 {kills} NPC{(kills > 1 ? "s" : "")}。");
        }

        private static void Item(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("无效的语法!正确的语法: {0}item <物品名称/物品ID> [数量] [前缀名称/前缀ID]", Specifier);
                return;
            }

            int amountParamIndex = -1;
            int itemAmount = 0;
            for (int i = 1; i < args.Parameters.Count; i++)
            {
                if (int.TryParse(args.Parameters[i], out itemAmount))
                {
                    amountParamIndex = i;
                    break;
                }
            }

            string itemNameOrId;
            if (amountParamIndex == -1)
                itemNameOrId = string.Join(" ", args.Parameters);
            else
                itemNameOrId = string.Join(" ", args.Parameters.Take(amountParamIndex));

            Item item;
            List<Item> matchedItems = TShock.Utils.GetItemByIdOrName(itemNameOrId);
            if (matchedItems.Count == 0)
            {
                args.Player.SendErrorMessage("无效的物品类型!");
                return;
            }
            else if (matchedItems.Count > 1)
            {
                args.Player.SendMultipleMatchError(matchedItems.Select(i => $"{i.Name}({i.netID})"));
                return;
            }
            else
            {
                item = matchedItems[0];
            }
            if (item.type < 1 && item.type >= Main.maxItemTypes)
            {
                args.Player.SendErrorMessage("物品类型 {0} 无效。", itemNameOrId);
                return;
            }

            int prefixId = 0;
            if (amountParamIndex != -1 && args.Parameters.Count > amountParamIndex + 1)
            {
                string prefixidOrName = args.Parameters[amountParamIndex + 1];
                var prefixIds = TShock.Utils.GetPrefixByIdOrName(prefixidOrName);

                if (item.accessory && prefixIds.Contains(PrefixID.Quick))
                {
                    prefixIds.Remove(PrefixID.Quick);
                    prefixIds.Remove(PrefixID.Quick2);
                    prefixIds.Add(PrefixID.Quick2);
                }
                else if (!item.accessory && prefixIds.Contains(PrefixID.Quick))
                    prefixIds.Remove(PrefixID.Quick2);

                if (prefixIds.Count > 1)
                {
                    args.Player.SendMultipleMatchError(prefixIds.Select(p => p.ToString()));
                    return;
                }
                else if (prefixIds.Count == 0)
                {
                    args.Player.SendErrorMessage("没有匹配的前缀 \"{0}\".", prefixidOrName);
                    return;
                }
                else
                {
                    prefixId = prefixIds[0];
                }
            }

            if (args.Player.InventorySlotAvailable || (item.type > 70 && item.type < 75) || item.ammo > 0 || item.type == 58 || item.type == 184)
            {
                if (itemAmount == 0 || itemAmount > item.maxStack)
                    itemAmount = item.maxStack;

                if (args.Player.GiveItemCheck(item.type, EnglishLanguage.GetItemNameById(item.type), itemAmount, prefixId))
                {
                    item.prefix = (byte)prefixId;
                    args.Player.SendSuccessMessage("给予 {0} {1} 个。", itemAmount, item.AffixName());
                }
                else
                {
                    args.Player.SendErrorMessage("你无法生成违禁物品，");
                }
            }
            else
            {
                args.Player.SendErrorMessage("你的仓库似乎已满。");
            }
        }

        private static void RenameNPC(CommandArgs args)
        {
            if (args.Parameters.Count != 2)
            {
                args.Player.SendErrorMessage("无效的语法!正确的语法: {0}renameNPC <guide(向导), nurse(护士), 等等。> <新名称>", Specifier);
                return;
            }
            int npcId = 0;
            if (args.Parameters.Count == 2)
            {
                List<NPC> npcs = TShock.Utils.GetNPCByIdOrName(args.Parameters[0]);
                if (npcs.Count == 0)
                {
                    args.Player.SendErrorMessage("无效的生物类型!");
                    return;
                }
                else if (npcs.Count > 1)
                {
                    args.Player.SendMultipleMatchError(npcs.Select(n => $"{n.FullName}({n.type})"));
                    return;
                }
                else if (args.Parameters[1].Length > 200)
                {
                    args.Player.SendErrorMessage("新用户名过长!");
                    return;
                }
                else
                {
                    npcId = npcs[0].netID;
                }
            }
            int done = 0;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if (Main.npc[i].active && ((npcId == 0 && !Main.npc[i].townNPC) || (Main.npc[i].netID == npcId && Main.npc[i].townNPC)))
                {
                    Main.npc[i].GivenName = args.Parameters[1];
                    NetMessage.SendData(56, -1, -1, NetworkText.FromLiteral(args.Parameters[1]), i, 0f, 0f, 0f, 0);
                    done++;
                }
            }
            if (done > 0)
            {
                TSPlayer.All.SendInfoMessage("{0} 重命名了 {1}.", args.Player.Name, args.Parameters[0]);
            }
            else
            {
                args.Player.SendErrorMessage("无法重命名 {0}!", args.Parameters[0]);
            }
        }

        private static void Give(CommandArgs args)
        {
            if (args.Parameters.Count < 2)
            {
                args.Player.SendErrorMessage(
                    "无效的语法!正确的语法: {0}give <物品ID或名称> <用户> [数量] [前缀 ID/名称]", Specifier);
                return;
            }
            if (args.Parameters[0].Length == 0)
            {
                args.Player.SendErrorMessage("缺少物品名称或编号。");
                return;
            }
            if (args.Parameters[1].Length == 0)
            {
                args.Player.SendErrorMessage("缺少用户名称。");
                return;
            }
            int itemAmount = 0;
            int prefix = 0;
            var items = TShock.Utils.GetItemByIdOrName(args.Parameters[0]);
            args.Parameters.RemoveAt(0);
            string plStr = args.Parameters[0];
            args.Parameters.RemoveAt(0);
            if (args.Parameters.Count == 1)
                int.TryParse(args.Parameters[0], out itemAmount);
            if (items.Count == 0)
            {
                args.Player.SendErrorMessage("无效的物品类型!");
            }
            else if (items.Count > 1)
            {
                args.Player.SendMultipleMatchError(items.Select(i => $"{i.Name}({i.netID})"));
            }
            else
            {
                var item = items[0];

                if (args.Parameters.Count == 2)
                {
                    int.TryParse(args.Parameters[0], out itemAmount);
                    var prefixIds = TShock.Utils.GetPrefixByIdOrName(args.Parameters[1]);
                    if (item.accessory && prefixIds.Contains(PrefixID.Quick))
                    {
                        prefixIds.Remove(PrefixID.Quick);
                        prefixIds.Remove(PrefixID.Quick2);
                        prefixIds.Add(PrefixID.Quick2);
                    }
                    else if (!item.accessory && prefixIds.Contains(PrefixID.Quick))
                        prefixIds.Remove(PrefixID.Quick2);
                    if (prefixIds.Count == 1)
                        prefix = prefixIds[0];
                }

                if (item.type >= 1 && item.type < Main.maxItemTypes)
                {
                    var players = TSPlayer.FindByNameOrID(plStr);
                    if (players.Count == 0)
                    {
                        args.Player.SendErrorMessage("无效用户!");
                    }
                    else if (players.Count > 1)
                    {
                        args.Player.SendMultipleMatchError(players.Select(p => p.Name));
                    }
                    else
                    {
                        var plr = players[0];
                        if (plr.InventorySlotAvailable || (item.type > 70 && item.type < 75) || item.ammo > 0 || item.type == 58 || item.type == 184)
                        {
                            if (itemAmount == 0 || itemAmount > item.maxStack)
                                itemAmount = item.maxStack;
                            if (plr.GiveItemCheck(item.type, EnglishLanguage.GetItemNameById(item.type), itemAmount, prefix))
                            {
                                args.Player.SendSuccessMessage(string.Format("给予 {0} {1} {2}(s).", plr.Name, itemAmount, item.Name));
                                plr.SendSuccessMessage(string.Format("{0} 给了你 {1} {2}(s).", args.Player.Name, itemAmount, item.Name));
                            }
                            else
                            {
                                args.Player.SendErrorMessage("你无法生成违禁物品。");
                            }

                        }
                        else
                        {
                            args.Player.SendErrorMessage("用户背包无空槽！");
                        }
                    }
                }
                else
                {
                    args.Player.SendErrorMessage("无效的物品类型!");
                }
            }
        }

        private static void Heal(CommandArgs args)
        {
            // heal <player> [amount]
            // To-Do: break up heal self and heal other into two separate permissions
            var user = args.Player;
            if (args.Parameters.Count < 1 || args.Parameters.Count > 2)
            {
                user.SendMessage("治愈语法 和 示例", Color.White);
                user.SendMessage($"{"heal".Color(Utils.BoldHighlight)} <{"player".Color(Utils.RedHighlight)}> [{"amount".Color(Utils.GreenHighlight)}]", Color.White);
                user.SendMessage($"使用示例: {"heal".Color(Utils.BoldHighlight)} {user.Name.Color(Utils.RedHighlight)} {"100".Color(Utils.GreenHighlight)}", Color.White);
                user.SendMessage($"如果没有指定数量，它将默认治疗目标玩家的最大HP。", Color.White);
                user.SendMessage($"要无声地执行此命令，请使用 {SilentSpecifier.Color(Utils.GreenHighlight)} 而不是 {Specifier.Color(Utils.RedHighlight)}", Color.White);
                return;
            }
            if (args.Parameters[0].Length == 0)
            {
                user.SendErrorMessage($"你没有写玩家的名字。");
                return;
            }

            string targetName = args.Parameters[0];
            var players = TSPlayer.FindByNameOrID(targetName);
            if (players.Count == 0)
                user.SendErrorMessage($"无法找到任何指定的玩家 \"{targetName}\"");
            else if (players.Count > 1)
                user.SendMultipleMatchError(players.Select(p => p.Name));
            else
            {
                var target = players[0];
                int amount = target.TPlayer.statLifeMax2;

                if (target.Dead)
                {
                    user.SendErrorMessage("你不能治愈死去的玩家!");
                    return;
                }

                if (args.Parameters.Count == 2)
                {
                    int.TryParse(args.Parameters[1], out amount);
                }
                target.Heal(amount);

                if (args.Silent)
                    user.SendSuccessMessage($"你治疗了 {(target == user ? "你自己" : target.Name)} 增加了 {amount} HP.");
                else
                    TSPlayer.All.SendInfoMessage($"{user.Name} 治疗了 {(target == user ? (target.TPlayer.Male ? "他自己" : "她自己") : target.Name)} 增加了 {amount} HP.");
            }
        }

        private static void Buff(CommandArgs args)
        {
            // buff <"buff name|ID"> [duration]
            var user = args.Player;
            if (args.Parameters.Count < 1 || args.Parameters.Count > 2)
            {
                user.SendMessage("Buff语法 和 示例", Color.White);
                user.SendMessage($"{"buff".Color(Utils.BoldHighlight)} <\"{"buff name".Color(Utils.RedHighlight)}|{"ID".Color(Utils.RedHighlight)}\"> [{"duration".Color(Utils.GreenHighlight)}]", Color.White);
                user.SendMessage($"使用示例: {"buff".Color(Utils.BoldHighlight)} \"{"obsidian skin".Color(Utils.RedHighlight)}\" {"-1".Color(Utils.GreenHighlight)}", Color.White);
                user.SendMessage($"如果不指定持续时间，则默认为 {"60".Color(Utils.GreenHighlight)} 秒。", Color.White);
                user.SendMessage($"如果你把 {"-1".Color(Utils.GreenHighlight)} 作为持续时间, 它将使用最大可能的415天的时间。", Color.White);
                return;
            }

            int id = 0;
            int time = 60;
            var timeLimit = (int.MaxValue / 60) - 1;

            if (!int.TryParse(args.Parameters[0], out id))
            {
                var found = TShock.Utils.GetBuffByName(args.Parameters[0]);

                if (found.Count == 0)
                {
                    user.SendErrorMessage($"无效的增益名称 \"{args.Parameters[0]}\"");
                    return;
                }

                if (found.Count > 1)
                {
                    user.SendMultipleMatchError(found.Select(f => Lang.GetBuffName(f)));
                    return;
                }
                id = found[0];
            }

            if (args.Parameters.Count == 2)
                int.TryParse(args.Parameters[1], out time);

            if (id > 0 && id < Main.maxBuffTypes)
            {
                // Max possible buff duration as of Terraria 1.4.2.3 is 35791393 seconds (415 days).
                if (time < 0 || time > timeLimit)
                    time = timeLimit;
                user.SetBuff(id, time * 60);
                user.SendSuccessMessage($"你为自己提供了增益 {TShock.Utils.GetBuffName(id)} ({TShock.Utils.GetBuffDescription(id)}) for {time} seconds.");
            }
            else
                user.SendErrorMessage($"\"{id}\" 无效的增益ID");
        }

        private static void GBuff(CommandArgs args)
        {
            var user = args.Player;
            if (args.Parameters.Count < 2 || args.Parameters.Count > 3)
            {
                user.SendMessage("给buff语法 和 示例", Color.White);
                user.SendMessage($"{"gbuff".Color(Utils.BoldHighlight)} <{"player".Color(Utils.RedHighlight)}> <{"buff name".Color(Utils.PinkHighlight)}|{"ID".Color(Utils.PinkHighlight)}> [{"seconds".Color(Utils.GreenHighlight)}]", Color.White);
                user.SendMessage($"使用示例: {"gbuff".Color(Utils.BoldHighlight)} {user.Name.Color(Utils.RedHighlight)} {"regen".Color(Utils.PinkHighlight)} {"-1".Color(Utils.GreenHighlight)}", Color.White);
                user.SendMessage($"在玩家不知情的情况下buff他们，使用 {SilentSpecifier.Color(Utils.RedHighlight)} 而不是 {Specifier.Color(Utils.GreenHighlight)}", Color.White);
                return;
            }
            int id = 0;
            int time = 60;
            var timeLimit = (int.MaxValue / 60) - 1;
            var foundplr = TSPlayer.FindByNameOrID(args.Parameters[0]);
            if (foundplr.Count == 0)
            {
                user.SendErrorMessage($"无效用户 \"{args.Parameters[0]}\"");
                return;
            }
            else if (foundplr.Count > 1)
            {
                user.SendMultipleMatchError(foundplr.Select(p => p.Name));
                return;
            }
            else
            {
                if (!int.TryParse(args.Parameters[1], out id))
                {
                    var found = TShock.Utils.GetBuffByName(args.Parameters[1]);
                    if (found.Count == 0)
                    {
                        user.SendErrorMessage($"无效的增益名称 \"{args.Parameters[1]}\"");
                        return;
                    }
                    else if (found.Count > 1)
                    {
                        user.SendMultipleMatchError(found.Select(b => Lang.GetBuffName(b)));
                        return;
                    }
                    id = found[0];
                }
                if (args.Parameters.Count == 3)
                    int.TryParse(args.Parameters[2], out time);
                if (id > 0 && id < Main.maxBuffTypes)
                {
                    var target = foundplr[0];
                    if (time < 0 || time > timeLimit)
                        time = timeLimit;
                    target.SetBuff(id, time * 60);
                    user.SendSuccessMessage($"你已经给自己加了 {(target == user ? " buff。" : target.Name)} with {TShock.Utils.GetBuffName(id)} ({TShock.Utils.GetBuffDescription(id)}) for {time} seconds!");
                    if (!args.Silent && target != user)
                        target.SendSuccessMessage($"{user.Name} 已经给你加了buff {TShock.Utils.GetBuffName(id)} ({TShock.Utils.GetBuffDescription(id)}) for {time} seconds!");
                }
                else
                    user.SendErrorMessage("无效 buff id!");
            }
        }

        public static void Grow(CommandArgs args)
        {
            bool growevilAmb = args.Player.HasPermission(Permissions.growevil);
            string subcmd = args.Parameters.Count == 0 ? "help" : args.Parameters[0].ToLower();

            var name = "Fail";
            var x = args.Player.TileX;
            var y = args.Player.TileY + 3;

            if (!TShock.Regions.CanBuild(x, y, args.Player))
            {
                args.Player.SendErrorMessage("你不能在这里替换砖块!");
                return;
            }

            switch (subcmd)
            {
                case "help":
                    {
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out int pageNumber))
                            return;

                        var lines = new List<string>
                    {
                        "- Default trees :",
                        "     'basic', 'sakura', 'willow', 'boreal', 'mahogany', 'ebonwood', 'shadewood', 'pearlwood'.",
                        "- Palm trees :",
                        "     'palm', 'corruptpalm', 'crimsonpalm', 'hallowpalm'.",
                        "- Gem trees :",
                        "     'topaz', 'amethyst', 'sapphire', 'emerald', 'ruby', 'diamond', 'amber'.",
                        "- Misc :",
                        "     'cactus', 'herb', 'mushroom'."
                    };

                        PaginationTools.SendPage(args.Player, pageNumber, lines,
                                new PaginationTools.Settings
                                {
                                    HeaderFormat = "可用的树类型和杂项。 ({0}/{1}):",
                                    FooterFormat = "输入 {0}grow help {{0}} 来获取更多指令。".SFormat(Commands.Specifier)
                                }
                            );
                    }
                    break;

                case "basic":
                    for (int i = x - 2; i < x + 3; i++)
                    {
                        Main.tile[i, y].active(true);
                        Main.tile[i, y].type = 2;
                        Main.tile[i, y].wall = 0;
                    }
                    Main.tile[x, y - 1].wall = 0;
                    WorldGen.GrowTree(x, y);
                    name = "初始树";
                    break;

                case "boreal":
                    for (int i = x - 2; i < x + 3; i++)
                    {
                        Main.tile[i, y].active(true);
                        Main.tile[i, y].type = 147;
                        Main.tile[i, y].wall = 0;
                    }
                    Main.tile[x, y - 1].wall = 0;
                    WorldGen.GrowTree(x, y);
                    name = "北方的树";
                    break;

                case "mahogany":
                    for (int i = x - 2; i < x + 3; i++)
                    {
                        Main.tile[i, y].active(true);
                        Main.tile[i, y].type = 60;
                        Main.tile[i, y].wall = 0;
                    }
                    Main.tile[x, y - 1].wall = 0;
                    WorldGen.GrowTree(x, y);
                    name = "桃花树";
                    break;

                case "sakura":
                    for (int i = x - 2; i < x + 3; i++)
                    {
                        Main.tile[i, y].active(true);
                        Main.tile[i, y].type = 2;
                        Main.tile[i, y].wall = 0;
                    }
                    Main.tile[x, y - 1].wall = 0;
                    WorldGen.TryGrowingTreeByType(596, x, y);
                    name = "樱花的树";
                    break;

                case "willow":
                    for (int i = x - 2; i < x + 3; i++)
                    {
                        Main.tile[i, y].active(true);
                        Main.tile[i, y].type = 2;
                        Main.tile[i, y].wall = 0;
                    }
                    Main.tile[x, y - 1].wall = 0;
                    WorldGen.TryGrowingTreeByType(616, x, y);
                    name = "柳树";
                    break;

                case "shadewood":
                    if (growevilAmb)
                    {
                        for (int i = x - 2; i < x + 3; i++)
                        {
                            Main.tile[i, y].active(true);
                            Main.tile[i, y].type = 199;
                            Main.tile[i, y].wall = 0;
                        }
                        Main.tile[x, y - 1].wall = 0;
                        WorldGen.GrowTree(x, y);
                        name = "暗影木";
                    }
                    else args.Player.SendErrorMessage("You do not have permission to grow this tree type");
                    break;

                case "ebonwood":
                    if (growevilAmb)
                    {
                        for (int i = x - 2; i < x + 3; i++)
                        {
                            Main.tile[i, y].active(true);
                            Main.tile[i, y].type = 23;
                            Main.tile[i, y].wall = 0;
                        }
                        Main.tile[x, y - 1].wall = 0;
                        WorldGen.GrowTree(x, y);
                        name = "乌木树";
                    }
                    else args.Player.SendErrorMessage("You do not have permission to grow this tree type");
                    break;

                case "pearlwood":
                    if (growevilAmb)
                    {
                        for (int i = x - 2; i < x + 3; i++)
                        {
                            Main.tile[i, y].active(true);
                            Main.tile[i, y].type = 109;
                            Main.tile[i, y].wall = 0;
                        }
                        Main.tile[x, y - 1].wall = 0;
                        WorldGen.GrowTree(x, y);
                        name = "珍珠树";
                    }
                    else args.Player.SendErrorMessage("You do not have permission to grow this tree type");
                    break;

                case "palm":
                    for (int i = x - 2; i < x + 3; i++)
                    {
                        Main.tile[i, y].active(true);
                        Main.tile[i, y].type = 53;
                        Main.tile[i, y].wall = 0;
                    }
                    for (int i = x - 2; i < x + 3; i++)
                    {
                        Main.tile[i, y + 1].active(true);
                        Main.tile[i, y + 1].type = 397;
                        Main.tile[i, y + 1].wall = 0;
                    }
                    Main.tile[x, y - 1].wall = 0;
                    WorldGen.GrowPalmTree(x, y);
                    name = "沙漠棕榈";
                    break;

                case "hallowpalm":
                    if (growevilAmb)
                    {
                        for (int i = x - 2; i < x + 3; i++)
                        {
                            Main.tile[i, y].active(true);
                            Main.tile[i, y].type = 116;
                            Main.tile[i, y].wall = 0;
                        }
                        for (int i = x - 2; i < x + 3; i++)
                        {
                            Main.tile[i, y + 1].active(true);
                            Main.tile[i, y + 1].type = 402;
                            Main.tile[i, y + 1].wall = 0;
                        }
                        Main.tile[x, y - 1].wall = 0;
                        WorldGen.GrowPalmTree(x, y);
                        name = "神圣棕榈";
                    }
                    else args.Player.SendErrorMessage("You do not have permission to grow this tree type");
                    break;

                case "crimsonpalm":
                    if (growevilAmb)
                    {
                        for (int i = x - 2; i < x + 3; i++)
                        {
                            Main.tile[i, y].active(true);
                            Main.tile[i, y].type = 234;
                            Main.tile[i, y].wall = 0;
                        }
                        for (int i = x - 2; i < x + 3; i++)
                        {
                            Main.tile[i, y + 1].active(true);
                            Main.tile[i, y + 1].type = 399;
                            Main.tile[i, y + 1].wall = 0;
                        }
                        Main.tile[x, y - 1].wall = 0;
                        WorldGen.GrowPalmTree(x, y);
                        name = "血腥棕榈";
                    }
                    else args.Player.SendErrorMessage("You do not have permission to grow this tree type");
                    break;

                case "corruptpalm":
                    if (growevilAmb)
                    {
                        for (int i = x - 2; i < x + 3; i++)
                        {
                            Main.tile[i, y].active(true);
                            Main.tile[i, y].type = 112;
                            Main.tile[i, y].wall = 0;
                        }
                        for (int i = x - 2; i < x + 3; i++)
                        {
                            Main.tile[i, y + 1].active(true);
                            Main.tile[i, y + 1].type = 398;
                            Main.tile[i, y + 1].wall = 0;
                        }
                        Main.tile[x, y - 1].wall = 0;
                        WorldGen.GrowPalmTree(x, y);
                        name = "腐化棕榈";
                    }
                    else args.Player.SendErrorMessage("You do not have permission to grow this tree type");
                    break;

                case "topaz":
                    for (int i = x - 2; i < x + 3; i++)
                    {
                        Main.tile[i, y].active(true);
                        Main.tile[i, y].type = 1;
                        Main.tile[i, y].wall = 0;
                    }
                    Main.tile[x, y - 1].wall = 0;
                    WorldGen.TryGrowingTreeByType(583, x, y);
                    name = "黄玉宝石树";
                    break;

                case "amethyst":
                    for (int i = x - 2; i < x + 3; i++)
                    {
                        Main.tile[i, y].active(true);
                        Main.tile[i, y].type = 1;
                        Main.tile[i, y].wall = 0;
                    }
                    Main.tile[x, y - 1].wall = 0;
                    WorldGen.TryGrowingTreeByType(584, x, y);
                    name = "紫晶宝石树";
                    break;

                case "sapphire":
                    for (int i = x - 2; i < x + 3; i++)
                    {
                        Main.tile[i, y].active(true);
                        Main.tile[i, y].type = 1;
                        Main.tile[i, y].wall = 0;
                    }
                    Main.tile[x, y - 1].wall = 0;
                    WorldGen.TryGrowingTreeByType(585, x, y);
                    name = "蓝玉宝石树";
                    break;

                case "emerald":
                    for (int i = x - 2; i < x + 3; i++)
                    {
                        Main.tile[i, y].active(true);
                        Main.tile[i, y].type = 1;
                        Main.tile[i, y].wall = 0;
                    }
                    Main.tile[x, y - 1].wall = 0;
                    WorldGen.TryGrowingTreeByType(586, x, y);
                    name = "翡翠宝石树";
                    break;

                case "ruby":
                    for (int i = x - 2; i < x + 3; i++)
                    {
                        Main.tile[i, y].active(true);
                        Main.tile[i, y].type = 1;
                        Main.tile[i, y].wall = 0;
                    }
                    Main.tile[x, y - 1].wall = 0;
                    WorldGen.TryGrowingTreeByType(587, x, y);
                    name = "红玉宝石树";
                    break;

                case "diamond":
                    for (int i = x - 2; i < x + 3; i++)
                    {
                        Main.tile[i, y].active(true);
                        Main.tile[i, y].type = 1;
                        Main.tile[i, y].wall = 0;
                    }
                    Main.tile[x, y - 1].wall = 0;
                    WorldGen.TryGrowingTreeByType(588, x, y);
                    name = "钻石宝石树";
                    break;

                case "amber":
                    for (int i = x - 2; i < x + 3; i++)
                    {
                        Main.tile[i, y].active(true);
                        Main.tile[i, y].type = 1;
                        Main.tile[i, y].wall = 0;
                    }
                    Main.tile[x, y - 1].wall = 0;
                    WorldGen.TryGrowingTreeByType(589, x, y);
                    name = "琥珀宝石树";
                    break;

                case "cactus":
                    Main.tile[x, y].type = 53;
                    WorldGen.GrowCactus(x, y);
                    name = "仙人掌";
                    break;

                case "herb":
                    Main.tile[x, y].active(true);
                    Main.tile[x, y].frameX = 36;
                    Main.tile[x, y].type = 83;
                    WorldGen.GrowAlch(x, y);
                    name = "草";
                    break;

                case "mushroom":
                    for (int i = x - 2; i < x + 3; i++)
                    {
                        Main.tile[i, y].active(true);
                        Main.tile[i, y].type = 70;
                        Main.tile[i, y].wall = 0;
                    }
                    Main.tile[x, y - 1].wall = 0;
                    WorldGen.GrowShroom(x, y);
                    name = "巨型发光蘑菇";
                    break;

                default:
                    args.Player.SendErrorMessage("未知的植物！");
                    return;
            }
            if (args.Parameters.Count == 1)
            {
                args.Player.SendTileSquare(x - 2, y - 20, 25);
                args.Player.SendSuccessMessage("试图种植一颗 " + name + ".");
            }
        }

        private static void ToggleGodMode(CommandArgs args)
        {
            TSPlayer playerToGod;
            if (args.Parameters.Count > 0)
            {
                if (!args.Player.HasPermission(Permissions.godmodeother))
                {
                    args.Player.SendErrorMessage("你无权以上帝模式对待其他用户。");
                    return;
                }
                string plStr = String.Join(" ", args.Parameters);
                var players = TSPlayer.FindByNameOrID(plStr);
                if (players.Count == 0)
                {
                    args.Player.SendErrorMessage("无效用户!");
                    return;
                }
                else if (players.Count > 1)
                {
                    args.Player.SendMultipleMatchError(players.Select(p => p.Name));
                    return;
                }
                else
                {
                    playerToGod = players[0];
                }
            }
            else if (!args.Player.RealPlayer)
            {
                args.Player.SendErrorMessage("未登录用户不能设置上帝模式!");
                return;
            }
            else
            {
                playerToGod = args.Player;
            }

            playerToGod.GodMode = !playerToGod.GodMode;

            var godPower = CreativePowerManager.Instance.GetPower<CreativePowers.GodmodePower>();

            godPower.SetEnabledState(playerToGod.Index, playerToGod.GodMode);

            if (playerToGod != args.Player)
            {
                args.Player.SendSuccessMessage(string.Format("{0} 是 {1} 上帝模式。", playerToGod.Name, playerToGod.GodMode ? "正处于" : "不再处于"));
            }

            if (!args.Silent || (playerToGod == args.Player))
            {
                playerToGod.SendSuccessMessage(string.Format("你 {0} 上帝模式。", playerToGod.GodMode ? "正处于" : "不再处于"));
            }
        }

        #endregion Game Commands
    }
}
