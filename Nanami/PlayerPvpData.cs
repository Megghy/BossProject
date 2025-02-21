using Microsoft.Xna.Framework;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace Nanami
{
    internal class PlayerPvpData
    {
        private const string Key = "nanami-pvp";

        /// <summary> 击杀 </summary>
        public int Eliminations { get; private set; }

        /// <summary> 死亡 </summary>
        public int Deaths { get; private set; }

        /// <summary> 伤害量 </summary>
        public int DamageDone { get; private set; }

        /// <summary> 承受伤害量 </summary>
        public int Endurance { get; private set; }

        /// <summary> 连续击杀 </summary>
        public int KillStreak { get; private set; }

        /// <summary> 最高连续击杀 </summary>
        public int BestKillStreak { get; private set; }

        public readonly int PlayerIndex;

        public PlayerPvpData(int index)
        {
            PlayerIndex = index;
        }

        public static PlayerPvpData GetPlayerData(int index)
        {
            return GetPlayerData(TShock.Players[index]);
        }

        public static PlayerPvpData GetPlayerData(TSPlayer player)
        {
            return player?.GetData<PlayerPvpData>(Key);
        }

        public static void LoadPlayerData(TSPlayer player)
        {
            var data = Nanami.PvpDatas.Load(player);
            player.SetData(Key, data);
        }

        /// <summary>
        /// 玩家歼敌事件
        /// </summary>
        public string Kill()
        {
            Eliminations++;
            KillStreak++;

            var player = TShock.Players[PlayerIndex];
            if (player.GetData<bool>("pvp_Heal"))
            {
                player.Heal(player.GetData<int>("LifeHeal"));
            }

            if (KillStreak > BestKillStreak)
                BestKillStreak = KillStreak;

            if (KillStreak >= Nanami.Config.MinKillTime)
            {
                var clrIndex = KillStreak - Nanami.Config.MinKillTime;

                var msg = Nanami.Config.KillTexts.Length > clrIndex ?
                    Nanami.Config.KillTexts[clrIndex].GetColorTag() :
                    TShock.Utils.ColorTag($"连续消灭 {KillStreak} 人!", Color.Yellow);

                return player.Name + msg;
            }
            return null;
        }

        /// <summary>
        /// 玩家死亡事件
        /// </summary>
        /// <param name="dmg">未经计算的攻击数值</param>
        public void Die(int dmg)
        {
            Deaths++;
            Endurance -= (int)Main.CalculateDamagePlayersTakeInPVP(dmg, Main.player[PlayerIndex].statDefense);
            if (KillStreak >= Nanami.Config.MinKillTime)
                TShock.Players[PlayerIndex].SendInfoMessage("你已死亡；临死前最大连续击杀数: {0}。", KillStreak);
            KillStreak = 0;
        }

        /// <summary>
        /// 玩家受伤事件
        /// </summary>
        /// <param name="calculatedDmg">经计算的攻击数值</param>
        public void Hurt(int calculatedDmg)
        {
            Endurance += calculatedDmg;
        }

        /// <summary>
        /// 玩家攻击事件
        /// </summary>
        /// <param name="calculatedDmg">经计算的攻击数值</param>
        public void Damage(int calculatedDmg)
        {
            DamageDone += calculatedDmg;
        }

        public static PlayerPvpData LoadFromDb(int index, QueryResult reader)
        {
            return new PlayerPvpData(index)
            {
                Eliminations = reader.Get<int>("Eliminations"),
                Deaths = reader.Get<int>("Deaths"),
                DamageDone = reader.Get<int>("DamageDone"),
                Endurance = reader.Get<int>("Endurance"),
                KillStreak = reader.Get<int>("KillStreak"),
                BestKillStreak = reader.Get<int>("BestKillStreak")
            };

        }
    }
}
