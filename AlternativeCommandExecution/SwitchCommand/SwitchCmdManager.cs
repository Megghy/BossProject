using System.Data;
using Terraria;
using Terraria.ID;
using TShockAPI;

namespace AlternativeCommandExecution.SwitchCommand
{
    internal sealed class SwitchCmdManager
    {

        public List<SwitchCmd> SwitchCmds { get; private set; } = new List<SwitchCmd>();

        public void UpdateSwitchCommands()
        {
            SwitchCmds.Clear();

            SwitchCmds = BossFramework.DB.DBTools.SQL.Select<SwitchCmd>().Where(c => c.worldId == Main.worldID).ToList();

            TShock.Log.ConsoleInfo("共载入{0}个指令开关。", SwitchCmds.Count);
        }

        public void ClearNonexistents()
        {
            var total = 0;
            var list = (from sc in SwitchCmds
                        let tile = Main.tile[sc.X, sc.Y]
                        where tile == null ||
                                            tile.type != TileID.Switches &&
                                            tile.type != TileID.Lever &&
                                            tile.type != TileID.PressurePlates
                        select sc).ToList();

            foreach (var sc in list)
            {
                Del(sc.X, sc.Y);
                total++;
            }

            TShock.Log.ConsoleInfo("移除了{0}个无效开关指令数据。", total);
        }

        public int getWaitTime(int x, int y)
        {
            return BossFramework.DB.DBTools.SQL.Select<SwitchCmd>().Where(c => c.X == x && c.Y == y).First()?.WaitTime ?? -1;
        }

        public void Add(int x, int y, string command)
        {
            var ex = SwitchCmds.FirstOrDefault(sc => sc.X == x && sc.Y == y);

            if (ex != null)
            {
                ex.Command = command;
                Update(ex);
            }
            else
            {
                ex = new SwitchCmd
                {
                    Command = command,
                    X = x,
                    Y = y,
                    AllPlayerCdSecond = 0,
                    IgnorePermission = true,
                    WaitTime = 0,
                    worldId = Main.worldID
                };
                Insert(ex);
                SwitchCmds.Add(ex);
            }
        }

        public void wait(int x, int y, string sec)
        {
            var sc = SwitchCmds.FirstOrDefault(s => s.X == x && s.Y == y);
            try
            {
                sc.WaitTime = int.Parse(sec);
                Update(sc);
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
            }
            Update(sc);
        }

        public void Del(int x, int y)
        {
            SwitchCmds.RemoveAll(sc => sc.X == x && sc.Y == y);

            try
            {
                BossFramework.DB.DBTools.SQL.Delete<SwitchCmd>().Where(c => c.X == x && c.Y == y && c.worldId == Main.worldID).ExecuteAffrows();
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
            }
        }

        public bool SetAllPlyCd(int x, int y, int allPlyCd)
        {
            var sc = SwitchCmds.FirstOrDefault(s => s.X == x && s.Y == y);
            if (sc == null)
            {
                return false;
            }

            if (sc.AllPlayerCdSecond == allPlyCd)
                return true;

            sc.AllPlayerCdSecond = allPlyCd;
            Update(sc);

            return true;
        }

        private void Insert(SwitchCmd cmd)
        {
            try
            {
                BossFramework.DB.DBTools.SQL.Insert(cmd).ExecuteAffrows();
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError(ex.ToString());
            }
        }
        public bool SetIgnoreStatus(int x, int y, bool ignore)
        {
            var sc = SwitchCmds.FirstOrDefault(s => s.X == x && s.Y == y);
            if (sc == null)
            {
                return false;
            }

            if (sc.IgnorePermission == ignore)
                return true;

            sc.IgnorePermission = ignore;
            Update(sc);

            return true;
        }

        private void Update(SwitchCmd cmd)
        {
            try
            {
                BossFramework.DB.DBTools.SQL.Update<SwitchCmd>().Where(c => c.X == cmd.X && c.Y == cmd.Y && c.worldId == cmd.worldId)
                    .Set(c => c.Command, cmd.Command)
                    .Set(c => c.IgnorePermission, cmd.IgnorePermission)
                    .Set(c => c.AllPlayerCdSecond, cmd.AllPlayerCdSecond)
                    .Set(c => c.WaitTime, cmd.WaitTime)
                    .ExecuteAffrows();
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError(ex.ToString());
            }
        }
    }
}
