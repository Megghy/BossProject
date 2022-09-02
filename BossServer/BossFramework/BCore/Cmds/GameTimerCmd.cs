using BossFramework.BInterfaces;
using BossFramework.BModels;
using FreeSql.DataAnnotations;
using System.Linq;
using Terraria;
using TShockAPI;

namespace BossFramework.BCore.Cmds
{
    public class GameTimerCmd : BaseCommand
    {
        [Index("gameTimer_PlayerIndex", nameof(PlayerId))]
        public class GameTimerHistory : DB.DBStructBase<GameTimerHistory>
        {
            public string PlayerName { get; set; }
            public int PlayerId { get; set; }
            public string Key { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public TimeSpan Time
                => EndDate - StartDate;
        }
        public const string GAMETIMER_PREFIX = "boss.player.gametimer";
        public override string[] Names { get; } = new[] { "gametimer", "gt" };

        public async static void Start(SubCommandArgs args)
        {
            if (args.Any())
            {
                var name = $"{GAMETIMER_PREFIX}.{args.First()}";
                var id = args.TsPlayer.Account?.ID ?? -1;
                var key = args.First();
                var min = await DB.DBTools.SQL.Select<GameTimerHistory>().Where(g => g.PlayerId == id && g.Key == key).OrderBy(g => g.EndDate - g.StartDate).FirstAsync();
                string text = $"已{(args.TsPlayer.ContainsData(name) ? "重新" : "")}开始计时 [{args.First().Color("ffffff")}] !";
                if (min != null)
                {
                    var minTime = min.Time;
                    text += $"\r\n你最好的成绩为: [{TimeString(minTime).Color("ffffff")}]";
                }
                args.Player.RegistePlayerStatus(GetStatus);
                args.TsPlayer.SetData(name, DateTime.Now);
                args.SendSuccessMsg(text);
            }
            else
            {
                args.SendErrorMsg($"语法错误. /gt start 名称");
            }
        }
        public async static void Stop(SubCommandArgs args)
        {
            args.Player.DeregistePlayerStatus(GetStatus);
            if (args.Any())
            {
                var name = $"{GAMETIMER_PREFIX}.{args.First()}";
                if (args.TsPlayer.ContainsData(name))
                {
                    var startTime = args.TsPlayer.GetData<DateTime>(name);
                    args.TsPlayer.RemoveData(name);
                    var id = args.TsPlayer.Account?.ID ?? -1;
                    var history = new GameTimerHistory()
                    {
                        PlayerId = id,
                        PlayerName = args.Player.Name,
                        StartDate = startTime,
                        EndDate = DateTime.Now,
                        Key = args.First(),
                        CreateTime = DateTime.Now
                    };
                    var key = args.First();
                    var min = await DB.DBTools.SQL.Select<GameTimerHistory>().Where(g => g.PlayerId == id && g.Key == key).OrderBy(g => g.EndDate - g.StartDate).FirstAsync();
                    DB.DBTools.Insert(history);
                    var text = $"[{args.First().Color("ffffff")}] 计时结束. 时间: " + TimeString(history.Time).Color("ffffff");
                    if (min != null)
                    {
                        var minTime = min.Time;
                        if (history.Time < minTime)
                        {
                            text += $"\r\n{"自己的新纪录!".Color("ED817E")} [{TimeString(minTime).Color("ffffff")} => {TimeString(history.Time).Color("ED817E")}]";
                            int p = Projectile.NewProjectile(Projectile.GetNoneSource(), args.TrPlayer.position.X, args.TrPlayer.position.Y - 64f, 0f, -8f, Terraria.ID.ProjectileID.RocketFireworkRed, 0, 0);
                            Main.projectile[p].Kill();
                        }
                    }
                    args.SendSuccessMsg(text);
                }
                else
                    args.SendErrorMsg($"尚未开始计时 [{args.First()}]");
            }
            else
            {
                args.SendErrorMsg($"语法错误. /gt start 名称");
            }
        }
        public static string GetStatus(BEventArgs.BaseEventArgs args)
        {
            if (args.Handled)
                return string.Empty;
            var timers = args.Player.TsPlayer.data.Where(k => k.Key.StartsWith(GAMETIMER_PREFIX));
            var text = "";
            timers.ForEach(t =>
            {
                var time = DateTime.Now - (DateTime)t.Value;
                text += $"{t.Key.Replace($"{GAMETIMER_PREFIX}.", "")} [{time.Hours} : {time.Minutes} : {time.Seconds}]\r\n";
            });
            return text;
        }

        public static string TimeString(TimeSpan time)
            => $"{time.Hours} 时 {time.Minutes} 分 {time.Seconds}.{time.Milliseconds / 10} 秒";
    }
}
