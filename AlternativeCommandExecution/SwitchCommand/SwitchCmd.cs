using FreeSql.DataAnnotations;
using TShockAPI;
using TShockAPI.DB;

namespace AlternativeCommandExecution.SwitchCommand
{
    [Table(Name = "switchcommands")]
    internal class SwitchCmd
    {
        public long worldId { get; set; }
        [Column(DbType = "text")]
        public string Command { get; set; }

        public int X { get; set; }

        public int Y { get; set; }

        public int AllPlayerCdSecond { get; set; }

        public bool IgnorePermission { get; set; } = true;

        public int WaitTime { get; set; }

        private int _currentCd;

        public void Tick()
        {
            if (AllPlayerCdSecond <= 0)
                return;

            if (_currentCd > 0)
                _currentCd--;
        }

        public bool TryUse(TSPlayer player)
        {
            if (AllPlayerCdSecond <= 0)
                return true;

            if (_currentCd > 0 && !player.HasPermission("ace.sc.ignorecd"))
                return false;

            _currentCd = AllPlayerCdSecond;
            return true;
        }
    }
}
