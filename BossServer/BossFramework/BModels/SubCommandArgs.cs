using BossFramework.BInterfaces;
using Microsoft.Xna.Framework;
using System.Collections;
using System.Collections.Generic;
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
            SubCommandName = args.Parameters[0];
            args.Parameters.RemoveAt(0); //第一个已经被默认读取为子命令名字
            Param = args.Parameters.ToArray();
            Player = args.Player.GetBPlayer();
        }
        public CommandArgs OriginArg { get; private set; }
        public string CommandName { get; private set; }
        public string SubCommandName { get; private set; }
        public BPlayer Player { get; private set; }
        public string FullCommand => OriginArg.Message;
        public TSPlayer TsPlayer => Player?.TsPlayer;
        public string this[int index] => Param.Length > index && index >= 0 ? Param[index] : null;
        public string[] Param { get; internal set; }

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
    }
}
