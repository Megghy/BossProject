// CustomWeaponPlugin.CustomWeaponPlugin
using CustomWeaponAPI;
using Microsoft.Xna.Framework;
using System.Globalization;
using System.Runtime.CompilerServices;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace CustomWeaponPlugin
{
    [ApiVersion(2, 1)]
    public class CustomWeaponPlugin : TerrariaPlugin
    {
        public override string Name => ((object)this).GetType().Namespace;

        public override string Author => "Axeel";

        public override Version Version => ((object)this).GetType().Assembly.GetName().Version;

        public static Configuration Config { get; private set; }

        public CustomWeaponPlugin(Main game)
            : base(game)
        {
        }

        public override void Initialize()
        {
            _003CInitialize_003Eg__ReloadConfig_007C11_0();
            Commands.ChatCommands.Add(new Command("cw.admin.test", Test, "cwtest")
            {
                HelpText = "自定义武器测试"
            });
            Commands.ChatCommands.Add(new Command("cw.admin.list", List, "cwlist")
            {
                HelpText = "列出自定义武器"
            });
            Commands.ChatCommands.Add(new Command("cw.admin.give", Give, "cwgive")
            {
                HelpText = "给予自定义武器"
            });
            Commands.ChatCommands.Add(new Command("cw.admin.get", Get, "cwget")
            {
                HelpText = "获取自定义武器"
            });
            Commands.ChatCommands.Add(new Command("cw.admin.get", GetDelay, "cwgetdelay")
            {
                HelpText = "延时获取自定义武器"
            });
            Commands.ChatCommands.Add(new Command("cw.admin.add", Add, "cwadd")
            {
                HelpText = "添加自定义武器"
            });
            Commands.ChatCommands.Add(new Command("cw.admin.del", Del, "cwdel")
            {
                HelpText = "删除自定义武器"
            });
            Commands.ChatCommands.Add(new Command("cw.admin.help", Help, "cw", "cwhelp")
            {
                HelpText = "自定义武器帮助"
            });
            GeneralHooks.ReloadEvent += delegate
            {
                _003CInitialize_003Eg__ReloadConfig_007C11_0();
            };
        }

        protected override void Dispose(bool disposing)
        {
        }

        private static void Help(CommandArgs args)
        {
            if (args.Parameters.Count > 0 && args.Parameters[0] == "add")
            {
                args.Player.SendInfoMessage("可选参数:\n-pre -prefix short(整数)类型 ——武器前缀\n-color 十六进制颜色代码 ——颜色\n-stack short(整数)类型 ——堆叠数\n-d -damage ushort(整数)类型 ——伤害\n-k -knock -knockback float(浮点数)类型 ——击退\n-anim -animation ushort(整数)类型 ——动画id\n-time -usetime float(浮点数)类型 ——使用时间\n-proj -shoot -shootproj short(整数)类型 ——弹幕id\n-speed -shootspeed float(浮点数)类型 ——射击速度\n-scale -size float(浮点数)类型 ——大小\n-ammo -ammoid short(整数)类型 ——弹药id\n-useammo -useammoid short(整数)类型 ——使用弹药id\n-notammo -nammo bool(true/false)类型 ——是否使用弹药\n");
            }
            else
            {
                args.Player.SendInfoMessage("添加武器(cwadd):\n  cwadd -name [武器名字] -id [基础物品id] <可选参数>\n  查看可选参数说明请使用cwhelp add\n删除武器(cwdel):\n  cwdel [武器名字]\n给予武器(cwgive):\n  cwgive [玩家] [武器名字]\n获取武器(cwget):\n  cwget [武器名字]\n列出所有武器(cwlist):\n  cwlist");
            }
        }

        private static void List(CommandArgs args)
        {
            foreach (KeyValuePair<string, CustomWeapon> weapon in Config.weapons)
            {
                args.Player.SendInfoMessage(weapon.Key);
            }
        }

        private static void Get(CommandArgs args)
        {
            CustomWeapon value;
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("缺少参数!");
            }
            else if (Config.weapons.TryGetValue(args.Parameters[0], out value))
            {
                CustomWeaponDropper.DropItem(args.Player, value);
            }
            else
            {
                args.Player.SendErrorMessage("找不到指定武器");
            }
        }

        private static void GetDelay(CommandArgs args)
        {
            CustomWeapon w;
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("缺少参数!");
            }
            else if (Config.weapons.TryGetValue(args.Parameters[0], out w))
            {
                if (args.Parameters.Count <= 1)
                {
                    return;
                }
                int x = 300;
                int y = 0;
                if (!int.TryParse(args.Parameters[1], out var d))
                {
                    args.Player.SendErrorMessage("延迟参数错误");
                }
                else if (args.Parameters.Count <= 4 || (int.TryParse(args.Parameters[2], out x) && int.TryParse(args.Parameters[3], out y)))
                {
                    Task task = Task.Run(async delegate
                    {
                        await Task.Delay(d);
                        CustomWeaponDropper.DropItemSafely(args.Player, w, x, y);
                    });
                }
            }
            else
            {
                args.Player.SendErrorMessage("找不到指定武器");
            }
        }

        private static void Give(CommandArgs args)
        {
            List<TSPlayer> list = TSPlayer.FindByNameOrID(args.Parameters[0]);
            if (Config.weapons.TryGetValue(args.Parameters[1], out var value))
            {
                if (list != null && list.Count > 0)
                {
                    CustomWeaponDropper.DropItem(list[0], value);
                    args.Player.SendSuccessMessage("给予自定义武器成功!");
                }
                else
                {
                    args.Player.SendErrorMessage("找不到指定玩家");
                }
            }
            else
            {
                args.Player.SendErrorMessage("找不到指定武器");
            }
        }

        private static void Add(CommandArgs args)
        {
            if (args.Parameters.Count % 2 != 0)
            {
                args.Player.SendErrorMessage("参数数量错误!");
                return;
            }
            string text = "";
            CustomWeapon customWeapon = new CustomWeapon();
            for (int i = 0; i < args.Parameters.Count / 2; i++)
            {
                switch (args.Parameters[i * 2])
                {
                    case "-name":
                        text = args.Parameters[i * 2 + 1];
                        break;
                    case "-pre":
                    case "-prefix":
                        {
                            if (!byte.TryParse(args.Parameters[i * 2 + 1], out var result10))
                            {
                                args.Player.SendErrorMessage(args.Parameters[i * 2] + " 参数错误！");
                                return;
                            }
                            customWeapon.Prefix = result10;
                            break;
                        }
                    case "-id":
                    case "-net":
                    case "-netid":
                        {
                            if (!short.TryParse(args.Parameters[i * 2 + 1], out var result4))
                            {
                                args.Player.SendErrorMessage(args.Parameters[i * 2] + " 参数错误！");
                                return;
                            }
                            customWeapon.ItemNetId = result4;
                            break;
                        }
                    case "-color":
                        {
                            if (!TryParseColor(args.Parameters[i * 2 + 1], out var color))
                            {
                                args.Player.SendErrorMessage(args.Parameters[i * 2] + " 参数错误！");
                                return;
                            }
                            customWeapon.Color = color;
                            break;
                        }
                    case "-stack":
                        {
                            if (!short.TryParse(args.Parameters[i * 2 + 1], out var result12))
                            {
                                args.Player.SendErrorMessage(args.Parameters[i * 2] + " 参数错误！");
                                return;
                            }
                            customWeapon.Stack = result12;
                            break;
                        }
                    case "-d":
                    case "-damage":
                        {
                            if (!ushort.TryParse(args.Parameters[i * 2 + 1], out var result7))
                            {
                                args.Player.SendErrorMessage(args.Parameters[i * 2] + " 参数错误！");
                                return;
                            }
                            customWeapon.Damage = result7;
                            break;
                        }
                    case "-k":
                    case "-knock":
                    case "-knockback":
                        {
                            if (!float.TryParse(args.Parameters[i * 2 + 1], out var result13))
                            {
                                args.Player.SendErrorMessage(args.Parameters[i * 2] + " 参数错误！");
                                return;
                            }
                            customWeapon.Knockback = result13;
                            break;
                        }
                    case "-anim":
                    case "-animation":
                        {
                            if (!ushort.TryParse(args.Parameters[i * 2 + 1], out var result9))
                            {
                                args.Player.SendErrorMessage(args.Parameters[i * 2] + " 参数错误！");
                                return;
                            }
                            customWeapon.UseAnimation = result9;
                            break;
                        }
                    case "-time":
                    case "-usetime":
                        {
                            if (!ushort.TryParse(args.Parameters[i * 2 + 1], out var result5))
                            {
                                args.Player.SendErrorMessage(args.Parameters[i * 2] + " 参数错误！");
                                return;
                            }
                            customWeapon.UseTime = result5;
                            break;
                        }
                    case "-proj":
                    case "-shoot":
                    case "-shootproj":
                        {
                            if (!short.TryParse(args.Parameters[i * 2 + 1], out var result2))
                            {
                                args.Player.SendErrorMessage(args.Parameters[i * 2] + " 参数错误！");
                                return;
                            }
                            customWeapon.ShootProjectileId = result2;
                            break;
                        }
                    case "-speed":
                    case "-shootspeed":
                        {
                            if (!float.TryParse(args.Parameters[i * 2 + 1], out var result11))
                            {
                                args.Player.SendErrorMessage(args.Parameters[i * 2] + " 参数错误！");
                                return;
                            }
                            customWeapon.ShootSpeed = result11;
                            break;
                        }
                    case "-scale":
                    case "-size":
                        {
                            if (!float.TryParse(args.Parameters[i * 2 + 1], out var result8))
                            {
                                args.Player.SendErrorMessage(args.Parameters[i * 2] + " 参数错误！");
                                return;
                            }
                            customWeapon.Scale = result8;
                            break;
                        }
                    case "-ammo":
                    case "-ammoid":
                        {
                            if (!short.TryParse(args.Parameters[i * 2 + 1], out var result6))
                            {
                                args.Player.SendErrorMessage(args.Parameters[i * 2] + " 参数错误！");
                                return;
                            }
                            customWeapon.AmmoIdentifier = result6;
                            break;
                        }
                    case "-useammo":
                    case "-useammoid":
                        {
                            if (!short.TryParse(args.Parameters[i * 2 + 1], out var result3))
                            {
                                args.Player.SendErrorMessage(args.Parameters[i * 2] + " 参数错误！");
                                return;
                            }
                            customWeapon.UseAmmoIdentifier = result3;
                            break;
                        }
                    case "-notammo":
                    case "-nammo":
                        {
                            if (!bool.TryParse(args.Parameters[i * 2 + 1], out var result))
                            {
                                args.Player.SendErrorMessage(args.Parameters[i * 2] + " 参数错误！");
                                return;
                            }
                            customWeapon.NotAmmo = result;
                            break;
                        }
                    default:
                        args.Player.SendErrorMessage("无效属性:" + args.Parameters[i * 2]);
                        return;
                }
            }
            if (string.IsNullOrEmpty(text))
            {
                args.Player.SendErrorMessage("未给武器命名或名字为空");
                return;
            }
            Config.Add(text, customWeapon);
            args.Player.SendSuccessMessage("添加自定义武器成功!");
        }

        private static void Del(CommandArgs args)
        {
            if (Config.weapons.TryGetValue(args.Parameters[0], out var _))
            {
                Config.Del(args.Parameters[0]);
                args.Player.SendSuccessMessage("删除自定义武器成功!");
            }
            else
            {
                args.Player.SendErrorMessage("找不到指定武器");
            }
        }

        private static void Test(CommandArgs args)
        {
            using Dictionary<string, CustomWeapon>.Enumerator enumerator = Config.weapons.GetEnumerator();
            while (enumerator.MoveNext())
            {
                CustomWeaponDropper.DropItem(weapon: enumerator.Current.Value, player: args.Player);
            }
        }

        public static bool TryParseColor(string value, out Color color)
        {
            color = default(Color);
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            if (value.Length != 6)
            {
                return false;
            }
            byte[] array = new byte[3];
            for (int i = 0; i < value.Length; i += 2)
            {
                if (!byte.TryParse(value[i].ToString() + value[i + 1], NumberStyles.HexNumber, null, out array[i / 2]))
                {
                    return false;
                }
            }
            color = new Color(array[0], array[1], array[2]);
            return true;
        }

        [CompilerGenerated]
        internal static void _003CInitialize_003Eg__ReloadConfig_007C11_0()
        {
            Config = Configuration.Read();
            Config.Write();
        }
    }
}

