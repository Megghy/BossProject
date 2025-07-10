using System.Text.RegularExpressions;
using BossFramework.BAttributes;
using BossFramework.BModels;
using BossFramework.DB;
using Microsoft.Xna.Framework;
using TrProtocol.Packets;
using TShockAPI;

namespace BossFramework.BCore
{
    public static partial class SignRedirector
    {
        public static List<BSign> Signs { get; set; }
        private static List<BSign> _overrideSign { get; set; } = [];

        [AutoPostInit]
        internal static void InitSign()
        {
            BLog.DEBUG("初始化标牌重定向");

            Signs = DBTools.GetAll<BSign>().Where(r => r.WorldId == Terraria.Main.worldID).ToList();

            Terraria.Main.sign.Where(s => s != null).ForEach(s =>
            {
                if (!Signs.Exists(sign => sign.X == s.x && sign.Y == s.y))
                {
                    CreateSign(s.x, s.y, s.text); //不存在则新建
                }
            });

            var invalidCount = 0;
            lock (Signs)
            {
                for (int i = 0; i < Signs.Count; i++)
                {
                    BSign s = Signs[i];
                    if (!Terraria.Main.tileSign[Terraria.Main.tile[s.X, s.Y].type])
                    {
                        RemoveSign(s);
                        invalidCount++;
                    }

                    //检查是否为特殊标牌
                    CheckIsSpecialSign(s);
                    //添加到网格网络
                    GridSystem.AddObject(s, new Point(s.X, s.Y));
                }
            }

            BLog.Success($"共加载 {Signs.Count} 个标牌, 其中 {Signs.Count(s => s.IsSpecialSign)} 个为特殊标牌");
            if (invalidCount > 0)
                BLog.Success($"移除 {invalidCount} 个无效标牌");
        }
        static int _updateTime = 0;
        [SimpleTimer(Time = 1)]
        internal static void UpdateClientSign()
        {
            BInfo.OnlinePlayers.ForEach(plr =>
            {
                if (plr.WatchingSign?.sign is { } sign
                && !new Rectangle(sign.X, sign.Y, 2, 2).Intersects(new Rectangle(plr.TileX - 5, plr.TileY - 5, 14, 12)))
                    plr.WatchingSign = null; //超出范围则未在编辑标牌.

                plr.LastWatchingSignIndex = (short)(plr.WatchingSign == null ? -1 : 0); //从第一个开始, 第零个一般是当前正在看的
            });
            foreach (var plr in BInfo.OnlinePlayers)
            {
                var s = plr.GetNearbyObjects<BSign>().Where(s => s != plr.WatchingSign?.sign);
                var newSigns = s.Except(plr.SendedSigns).ToList();
                var specialSigns = s.Where(s => s.IsSpecialSign).ToList();
                plr.SendedSigns = [.. s];
                newSigns.ForEach(s => SendSign(s, plr, watch: false));

                specialSigns.Where(s => s.Info.UpdateInterval.HasValue && _updateTime - s.Info.LastUpdate >= s.Info.UpdateInterval.Value)
                .ForEach(s =>
                {
                    s.Info.LastUpdate = _updateTime;
                    SendSign(s, plr, watch: false);
                });
            }
            _updateTime++;
        }

        #region 事件
        public delegate void OnSignRead(BEventArgs.SignReadEventArgs args);
        public static event OnSignRead SignRead;
        public delegate void OnSignCreate(BEventArgs.SignCreateEventArgs args);
        public static event OnSignCreate SignCreate;
        public delegate void OnSignUpdate(BEventArgs.SignUpdateEventArgs args);
        public static event OnSignUpdate SignUpdate;
        public delegate void OnSignRemove(BEventArgs.SignRmoveEventArgs args);
        public static event OnSignRemove SignRemove;
        internal static void OnSyncSign(BPlayer plr, ReadSign sign)
        {
            if (FindBSignFromPos(sign.Position.X, sign.Position.Y) is { } s)
            {
                if (sign.Text != s.Text)
                {
                    var args = new BEventArgs.SignUpdateEventArgs(plr, sign);
                    SignUpdate?.Invoke(args);
                    if (!args.Handled)
                    {
                        if (plr.TSPlayer.HasPermission("boss.player.sign.update"))
                        {
                            UpdateSignDirect(plr, s, sign.Text);
                            plr?.SendSuccessMsg($"已更新标牌");
                        }
                        else
                        {
                            plr.SendInfoMsg($"你没有权限修改这个标牌");
                        }
                    }
                }
            }
            else
            {
                var args = new BEventArgs.SignCreateEventArgs(plr, sign.Position.ToPoint());
                SignCreate?.Invoke(args);
                if (!args.Handled)
                {
                    CreateSign(sign.Position.X, sign.Position.Y, sign.Text, plr);
                    plr?.SendSuccessMsg($"已创建标牌");
                }
            }
        }
        internal static void OnOpenSign(BPlayer plr, RequestReadSign readSign)
        {
            var args = new BEventArgs.SignReadEventArgs(plr, readSign.Position.ToPoint());
            SignRead?.Invoke(args);
            if (!args.Handled)
            {
                if (FindBSignFromPos(readSign.Position.X, readSign.Position.Y) is { } s)
                {
                    s.SendSign(plr, true, format: !plr.TSPlayer.HasPermission("boss.player.sign.update")); //如果没有修改权限就格式化
                }
                else //不确定要不要生成, 要是有人一直代码发包就能一直创建了
                {
                    CreateSign(readSign.Position.X, readSign.Position.Y, "", plr)
                        .SendSign(plr, true, format: false);
                }
            }
        }
        #endregion

        #region 标牌操作

        public static void UpdateSignDirect(BPlayer plr, BSign sign, string text)
        {
            var oldText = sign.Text;
            sign.Text = text;
            CheckIsSpecialSign(sign);

            if (sign.IsSpecialSign && !plr.TSPlayer.HasPermission("boss.player.sign.code"))
            {
                plr.SendInfoMsg($"你没有权限使用此类型的标牌");
                sign.Text = oldText;
                return;
            }

            if (sign.IsSpecialSign)
            {
                try
                {
                    var result = CommandPlaceholder.ReplacePlaceholder(sign.Text, plr);
                    plr.SendSuccessMsg($"格式化标牌成功: \n{result}");
                }
                catch (Exception ex)
                {
                    plr.SendInfoMsg($"格式化标牌失败: \n{ex.Message}");
                    sign.Text = oldText;
                    return;
                }
            }

            sign.Update(s => s.LastUpdateUser, plr.Index);
            sign.Update(s => s.Text, sign.Text);

            BInfo.OnlinePlayers.Where(p => p.WatchingSign?.sign == sign || p.IsNearBy(new(sign.X, sign.Y)))
                .ForEach(p =>
                {
                    sign.SendSign(p, p.WatchingSign?.sign == sign); //同步给其他正在看这个牌子的玩家
                });
        }

        public static BSign CreateSign(int tileX, int tileY, string text, BPlayer plr = null)
        {

            var sign = new BSign()
            {
                X = tileX,
                Y = tileY,
                Text = text,
                Owner = plr?.Index ?? -1,
                LastUpdateUser = plr?.Index ?? -1,
                WorldId = Terraria.Main.worldID
            };
            DBTools.Insert(sign);
            Signs.Add(sign);
            CheckIsSpecialSign(sign);

            GridSystem.AddObject(sign, new Point(tileX, tileY));

            BLog.DEBUG($"创建标牌数据于 {sign.X} - {sign.Y} {(plr is null ? "" : $"来自 {plr}")}");
            return sign;
        }
        public static short GetNextSignSlot(this BPlayer plr)
        {
            if (plr.LastWatchingSignIndex > 998)
                plr.LastWatchingSignIndex = -1;
            plr.LastWatchingSignIndex++;
            if (plr.LastWatchingSignIndex == plr.WatchingChest?.slot)
                plr.LastWatchingSignIndex++;
            return plr.LastWatchingSignIndex;
        }
        public static BSign FindBSignFromPos(int tileX, int tileY)
            => _overrideSign.LastOrDefault(s => s.Contains(tileX, tileY)) ?? Signs.LastOrDefault(s => s.Contains(tileX, tileY));
        public static void SendSign(this BSign sign, BPlayer target, bool watch = false, bool format = true)
        {
            if (watch)
                target.WatchingSign = new(target.LastWatchingSignIndex, sign);
            var signPacket = new ReadSign()
            {
                PlayerSlot = target.Index,
                Position = new((short)sign.X, (short)sign.Y),
                Text = sign.Text,
                SignSlot = target.GetNextSignSlot(),
                Bit1 = new Terraria.BitsByte(!watch)
            };
            var result = format ? TryFormatSignText(sign, target) : sign.Text;
            signPacket.Text = result;
            target.SendPacket(signPacket);
        }
        public static void SendSign(BPlayer plr, short x, short y, string text, bool watch = false, bool format = true)
        {
            var sign = new BSign() { X = x, Y = y, Text = text };
            CheckIsSpecialSign(sign);
            SendSign(sign, plr, watch, format);
        }
        public static bool RemoveSign(int x, int y)
        {
            if (FindBSignFromPos(x, y) is { } sign)
            {
                RemoveSign(sign);
                return true;
            }
            return false;
        }
        public static void RemoveSign(BSign sign)
        {
            BInfo.OnlinePlayers.Where(p => p.WatchingSign?.sign == sign)
                    .ForEach(p => p.WatchingSign = null);
            GridSystem.RemoveObject(sign);
            DeregisterOverrideSign(sign);
            if (Signs.Remove(sign))
                DBTools.Delete(sign);
        }
        public static BSign[] GetSignsInArea(int startX, int startY, int width, int height)
        {
            var result = new List<BSign>();
            var rec = new Rectangle(startX, startY, width, height);
            AllSign().ForEachWithIndex((s, i) =>
            {
                if (!result.Exists(r => r.Contains(s.X, s.Y)) && rec.Contains(s.X, s.Y))
                    result.Add(s);
            });
            return [.. result];
        }
        #endregion

        #region 标牌内容处理

        public static void CheckIsSpecialSign(BSign sign)
        {
            var regex = SignParamsRegex();
            var result = GetSignType(sign.Text);
            if (!string.IsNullOrEmpty(result.type))
            {
                sign.IsSpecialSign = true;
                sign.Info = result.info;
            }
            else
            {
                sign.IsSpecialSign = false;
                sign.Info = null;
            }
        }
        public static (string type, BSign.SignRuntimeInfo info) GetSignType(string text)
        {
            if (string.IsNullOrEmpty(text) || !text.Contains('\n'))
                return (string.Empty, null);
            var lines = text.Split("\n");

            string pattern = @"```\s*(.*?)\s*```";
            Match typeMmatch = Regex.Match(lines.First().Trim().ToLower(), pattern, RegexOptions.Singleline);
            var regex = SignParamsRegex();
            if (typeMmatch.Success)
            {
                string content = typeMmatch.Groups[1].Value.Trim();
                var remainText = new List<string>();
                var signInfo = new BSign.SignRuntimeInfo() { Type = content };
                foreach (var line in lines.Skip(1))
                {
                    var m = regex.Match(line);
                    if (m.Success)
                    {
                        switch (m.Groups["key"].Value.ToLower())
                        {
                            case "updateinterval":
                            case "ui":
                                signInfo.UpdateInterval = int.Parse(m.Groups["value"].Value);
                                break;
                        }
                    }
                    else
                    {
                        remainText.Add(line);
                    }
                }
                signInfo.Conetent = string.Join("\n", remainText);
                return (content, signInfo);
            }
            return (string.Empty, null);
        }
        public static string TryFormatSignText(BSign sign, BPlayer plr)
        {
            if (!sign.IsSpecialSign)
            {
                return sign.Text;
            }
            try
            {
                switch (sign.Info.Type)
                {
                    case "placeholder":
                    case "ph":
                        return sign.Info.Conetent.ReplacePlaceholder(plr);
                    case "code":
                        return RunCode(sign.Info.Conetent, plr);
                    case "fullcode":
                        return "";
                    default:
                        return $"[BOSS Sign Redirector] 不支持的格式: {sign.Info.Type}\n<{sign.X},{sign.Y}>";
                }
            }
            catch (Exception ex)
            {
                return $"[BOSS Sign Redirector] 发生错误\n<{sign.X},{sign.Y}>\n\n{ex.Message}";
            }
        }

        static string RunCode(string code, BPlayer plr, int? signX = null, int? signY = null)
        {
            return "";
        }
        #endregion

        public static void RegisterOverrideSign(int tileX, int tileY, string text)
            => _overrideSign.Add(new()
            {
                X = tileX,
                Y = tileY,
                Text = text,
                Owner = -1,
                LastUpdateUser = -1
            });
        public static bool DeregisterOverrideSign(int tileX, int tileY)
        {
            foreach (var sign in _overrideSign)
            {
                if (sign.X == tileX && sign.Y == tileY)
                    return DeregisterOverrideSign(sign);
            }
            return false;
        }
        public static bool DeregisterOverrideSign(BSign sign)
        {
            GridSystem.RemoveObject(sign);
            return _overrideSign.Remove(sign);
        }
        /// <summary>
        /// 包含注册的牌子在内的所有牌子
        /// </summary>
        /// <returns></returns>
        public static List<BSign> AllSign()
        {
            var result = new List<BSign>();
            result.AddRange(Signs);
            result.AddRange(_overrideSign);
            return result;
        }
        public static List<BSign> RegistedSigns
            => _overrideSign;

        [GeneratedRegex(@"^-(\s*)(?<key>\w+)(\s*)=(\s*)(?<value>[^-]+)")]
        private static partial Regex SignParamsRegex();
    }
}
