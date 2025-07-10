using BossFramework.BModels;
using BossFramework.DB;
using TShockAPI;

namespace BossFramework.BCore
{
    public static class BPointSystem
    {
        public static bool ChangePoint(int targetId, int num, string from = "未知")
        {
            try
            {
                DBTools.SQL.Transaction(() =>
                {
                    if (DBTools.SQL.Update<BPlayer>(targetId).
                    Set(p => new BPlayer
                    {
                        Point = p.Point + num,
                    }).ExecuteAffrows() < 1)
                        throw new Exception($"玩家 {targetId} 积分修改失败");
                    if (DBTools.SQL.Insert<BPointInfo>(new BPointInfo
                    {
                        Point = num,
                        FromReason = from,
                        TargetId = targetId
                    }).ExecuteAffrows() < 1)
                        throw new Exception($"未能插入积分记录");
                });
                var plrName = targetId.ToString();
                if (BInfo.OnlinePlayers.FirstOrDefault(p => p.Id == targetId) is { } plr)
                {
                    plr.SendCombatMessage(num.ToString(), num > 0 ? Microsoft.Xna.Framework.Color.Green : Microsoft.Xna.Framework.Color.PaleVioletRed);
                    plr.Point += num;
                    plrName = plr.Name;
                }
                BLog.Log($"[{plrName}] {(num < 0 ? "失去" : "获得")}了 {num.Color("7FDFDE")} 积分, 来自: {from}");
                return true;
            }
            catch (Exception ex)
            {
                BLog.Warn(ex);
                return false;
            }
        }
    }
}
