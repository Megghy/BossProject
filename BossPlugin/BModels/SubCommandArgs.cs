using System.Collections;
using System.Collections.Generic;
using TShockAPI;

namespace BossPlugin.BModels
{
    /// <summary>
    /// 可以直接用下标来取命令参数
    /// </summary>
    public class SubCommandArgs : IEnumerable<string>
    {
        public SubCommandArgs(CommandArgs args, string cmdName)
        {
            OriginArg = args;
            CommandName = cmdName;
            SubCommandName = args.Parameters[0];
            args.Parameters.RemoveAt(0); //第一个已经被默认读取为子命令名字
            Param = args.Parameters.ToArray();
            BPlayer = args.Player.GetBPlayer();
        }
        public CommandArgs OriginArg { get; private set; }
        public string CommandName { get; private set; }
        public string SubCommandName { get; private set; }
        public BPlayer BPlayer { get; private set; }
        public string FullCommand => OriginArg.Message;
        public TSPlayer? TsPlayer => BPlayer?.TsPlayer;
        public string? this[int index] => Param.Length > index && index >= 0 ? Param[index] : null;
        public string[] Param { get; internal set; }

        public IEnumerator<string> GetEnumerator()
        {
            return ((IEnumerable<string>)Param).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Param.GetEnumerator();
        }
    }
}
