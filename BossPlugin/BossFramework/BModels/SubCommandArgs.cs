using System.Collections;
using BossFramework.BInterfaces;
using Microsoft.Xna.Framework;
using Terraria;
using TShockAPI;

namespace BossFramework.BModels
{
    /// <summary>
    /// 可以直接用下标来取命令参数
    /// </summary>
    public class SubCommandArgs : IEnumerable<string>, ISendMsg
    {
        public SubCommandArgs(CommandArgs args, string cmdName)
        {
            OriginArg = args;
            CommandName = cmdName;
            if (args.Parameters.Any())
            {
                SubCommandName = args.Parameters.FirstOrDefault();
                Param = args.Parameters.Skip(1)?.ToArray() ?? Array.Empty<string>();//第一个已经被默认读取为子命令名字
            }
            Player = args.Player.GetBPlayer();
        }
        public CommandArgs OriginArg { get; private set; }
        public string CommandName { get; private set; }
        public string SubCommandName { get; private set; }
        public BPlayer Player { get; private set; }
        public string FullCommand => OriginArg.Message;
        public TSPlayer TsPlayer => Player?.TSPlayer;
        public Player TrPlayer => Player?.TRPlayer;
        public string this[int index] => Param.Length > index && index >= 0 ? Param[index] : null;
        public string[] Param { get; internal set; } = Array.Empty<string>();

        public IEnumerator<string> GetEnumerator()
        {
            return ((IEnumerable<string>)Param).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Param.GetEnumerator();
        }

        public void SendSuccessMsg(object text)
        {
            Player?.SendSuccessMsg(text);
        }

        public void SendInfoMsg(object text)
        {
            Player?.SendInfoMsg(text);
        }

        public void SendErrorMsg(object text)
        {
            Player?.SendErrorMsg(text);
        }

        public void SendMsg(object text, Color color = default)
        {
            Player?.SendMsg(text, color);
        }
        public static TSPlayer? GetTSPlayerFromArg(SubCommandArgs args, int index)
            => GetPlayerFromArg(args, index)?.TSPlayer;
        public static BPlayer? GetPlayerFromArg(SubCommandArgs args, int index)
        {
            BPlayer? player = null;
            if (!args.Any() || args.Count() < index + 1)
            {
                args.Player.SendErrorMsg("参数数量错误");
            }
            else
            {
                var results = BInfo.OnlinePlayers.Where(p => p.Name == args[index] || p.TSPlayer.Index.ToString() == args[index]);
                if (results.Count() == 1) return results.FirstOrDefault();
                args.Player.SendMultipleMatchError(results.Select(p => p.Name));
            }

            if (player is null)
            {
                args.Player.SendErrorMsg("找不到指定玩家");
                return null;
            }

            return player;
        }
    }
}
