using Microsoft.Xna.Framework;
using System.Diagnostics.CodeAnalysis;
using System.IO.Streams;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace RegionTrigger
{
    [ApiVersion(2, 1)]
    public sealed class RegionTrigger : TerrariaPlugin
    {
        internal RtRegionManager RtRegions;

        public override string Name => "RegionTrigger";

        public override string Author => "MistZZT";

        public override Version Version => GetType().Assembly.GetName().Version;

        public override string Description => "Perform actions in regions where players are active.";

        public RegionTrigger(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize, -10);
            ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInit, -10);
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreetPlayer);
            ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
            ServerApi.Hooks.NetGetData.Register(this, OnGetData, 2000);

            GetDataHandlers.TogglePvp += OnTogglePvp;
            GetDataHandlers.TileEdit += OnTileEdit;
            GetDataHandlers.NewProjectile += OnNewProjectile;
            GetDataHandlers.PlayerUpdate += OnPlayerUpdate;
            RegionHooks.RegionDeleted += OnRegionDeleted;
            PlayerHooks.PlayerPermission += OnPlayerPermission;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInit);
                ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreetPlayer);
                ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
                ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);

                GetDataHandlers.TogglePvp -= OnTogglePvp;
                GetDataHandlers.TileEdit -= OnTileEdit;
                GetDataHandlers.NewProjectile -= OnNewProjectile;
                GetDataHandlers.PlayerUpdate -= OnPlayerUpdate;
                RegionHooks.RegionDeleted -= OnRegionDeleted;
                PlayerHooks.PlayerPermission -= OnPlayerPermission;
            }
            base.Dispose(disposing);
        }

        private void OnInitialize(EventArgs args)
        {
            Commands.ChatCommands.Add(new Command("regiontrigger.manage", RegionSetProperties, "rt"));

            RtRegions = new RtRegionManager(TShock.DB);
        }

        private void OnPostInit(EventArgs args)
        {
            RtRegions.Reload();
        }

        private static void OnGreetPlayer(GreetPlayerEventArgs args)
        {
            RtPlayer.GetPlayerInfo(TShock.Players[args.Who]);
        }

        private DateTime _lastCheck = DateTime.UtcNow;

        private void OnUpdate(EventArgs args)
        {
            if ((DateTime.UtcNow - _lastCheck).TotalSeconds >= 1)
            {
                OnSecondUpdate();
                _lastCheck = DateTime.UtcNow;
            }
        }

        private static void OnTogglePvp(object sender, GetDataHandlers.TogglePvpEventArgs args)
        {
            var ply = TShock.Players[args.PlayerId];
            var dt = RtPlayer.GetPlayerInfo(ply);

            if (dt.ForcePvP == true && !args.Pvp ||
                dt.ForcePvP == false && args.Pvp ||
                !dt.CanTogglePvP)
            {
                ply.SendErrorMessage("You can't change your PvP status in this region!");
                ply.SendData(PacketTypes.TogglePvp, "", args.PlayerId);
                args.Handled = true;
            }
        }

        private void OnTileEdit(object sender, GetDataHandlers.TileEditEventArgs args)
        {
            if (args.Action != GetDataHandlers.EditAction.PlaceTile)
                return;

            var rt = RtRegions.GetTopRegion(RtRegions.Regions.Where(r => r.Region.InArea(args.X, args.Y)));

            if (rt?.HasEvent(Event.Tileban) != true)
                return;

            if (rt.TileIsBanned(args.EditData) && !args.Player.HasPermission("regiontrigger.bypass.tileban"))
            {
                args.Player.SendTileSquare(args.X, args.Y, 1);
                args.Player.SendErrorMessage("You do not have permission to place this tile.");
                args.Handled = true;
            }
        }

        private void OnGetData(GetDataEventArgs args)
        {
            if (args.Handled)
                return;

            if (args.MsgID != PacketTypes.ItemDrop && args.MsgID != PacketTypes.UpdateItemDrop)
                return;

            using (var data = new MemoryStream(args.Msg.readBuffer, args.Index, args.Length - 1))
            {
                var id = data.ReadInt16();
                var pos = new Vector2(data.ReadSingle(), data.ReadSingle());

                if (id < 400)
                    return;

                var rt = RtRegions.GetTopRegion(RtRegions.Regions.Where(r => r.Region.InArea((int)(pos.X / 16), (int)(pos.Y / 16))));

                if (rt?.HasEvent(Event.NoItem) != true)
                    return;

                var player = TShock.Players[args.Msg.whoAmI];
                if (player == null || player.HasPermission("regiontrigger.bypass.itemdrop"))
                    return;

                player.SendErrorMessage("You cannot drop item in this region!");
                player.Disable("drop item");
                args.Handled = true;
            }
        }

        private static void OnNewProjectile(object sender, GetDataHandlers.NewProjectileEventArgs args)
        {
            var ply = TShock.Players[args.Owner];
            var rt = RtPlayer.GetPlayerInfo(ply).CurrentRegion;

            if (rt?.HasEvent(Event.Projban) != true)
                return;

            if (rt.ProjectileIsBanned(args.Type) && !ply.HasPermission("regiontrigger.bypass.projban"))
            {
                ply.Disable($"Create banned projectile in region {rt.Region.Name}.", DisableFlags.WriteToLogAndConsole);
                ply.SendErrorMessage("This projectile is banned here.");
                ply.RemoveProjectile(args.Index, args.Owner);
            }
        }

        private static void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs args)
        {
            var ply = TShock.Players[args.PlayerId];
            var rt = RtPlayer.GetPlayerInfo(ply).CurrentRegion;

            if (rt?.HasEvent(Event.Itemban) != true)
                return;

            if (args.Control.IsUsingItem)
            {
                var itemName = ply.TPlayer.inventory[args.SelectedItem].Name;
                if (rt.ItemIsBanned(itemName) && !ply.HasPermission("regiontrigger.bypass.itemban"))
                {
                    var control = args.Control;
                    control.IsUsingItem = false;
                    args.Control = control;
                    ply.Disable($"using a banned item ({itemName})", DisableFlags.WriteToLogAndConsole);
                    ply.SendErrorMessage($"You can't use {itemName} here.");
                }
            }
        }

        private void OnRegionDeleted(RegionHooks.RegionDeletedEventArgs args)
        {
            RtRegions.DeleteRtRegion(args.Region.ID);
        }

        private static void OnPlayerPermission(PlayerPermissionEventArgs args)
        {
            var rt = RtPlayer.GetPlayerInfo(args.Player).CurrentRegion;

            if (rt?.HasEvent(Event.TempPermission) != true)
                return;

            if (rt.HasPermission(args.Permission) && !args.Player.HasPermission("regiontrigger.bypass.tempperm"))
                args.Result = PermissionHookResult.Granted;
        }

        private static void OnRegionLeft(TSPlayer player, RtRegion region, RtPlayer data)
        {
            if (region.HasEvent(Event.LeaveMsg))
            {
                if (string.IsNullOrWhiteSpace(region.LeaveMsg))
                    player.SendInfoMessage("You have left region {0}", region.Region.Name);
                else
                    player.SendMessage(region.LeaveMsg, Color.White);
            }

            if (region.HasEvent(Event.TempGroup) && player.tempGroup == region.TempGroup)
            {
                player.tempGroup = null;
                player.SendInfoMessage("You are no longer in group {0}.", region.TempGroup.Name);
            }

            if (region.HasEvent(Event.Godmode))
            {
                player.GodMode = false;
                player.SendInfoMessage("You are no longer in godmode!");
            }

            if (region.HasEvent(Event.Pvp) || region.HasEvent(Event.NoPvp) || region.HasEvent(Event.InvariantPvp))
            {
                data.ForcePvP = null;
                data.CanTogglePvP = true;
                player.SendInfoMessage("You can toggle your PvP status now.");
            }
        }

        private static void OnRegionEntered(TSPlayer player, RtPlayer data)
        {
            var rt = data.CurrentRegion;

            if (rt.HasEvent(Event.EnterMsg))
            {
                if (string.IsNullOrWhiteSpace(rt.EnterMsg))
                    player.SendInfoMessage("You have entered region {0}", rt.Region.Name);
                else
                    player.SendMessage(rt.EnterMsg, Color.White);
            }

            if (rt.HasEvent(Event.Message) && !string.IsNullOrWhiteSpace(rt.Message))
            {
                player.SendInfoMessage(rt.Message);
            }

            if (rt.HasEvent(Event.TempGroup) && rt.TempGroup != null && !player.HasPermission("regiontrigger.bypass.tempgroup"))
            {
                if (rt.TempGroup == null)
                    TShock.Log.ConsoleError("TempGroup in region '{0}' is not valid!", rt.Region.Name);
                else
                {
                    player.tempGroup = rt.TempGroup;
                    player.SendInfoMessage("Your group has been changed to {0} in this region.", rt.TempGroup.Name);
                }
            }

            if (rt.HasEvent(Event.Kill) && !player.HasPermission("regiontrigger.bypass.kill"))
            {
                player.KillPlayer();
                player.SendInfoMessage("You were killed!");
            }

            if (rt.HasEvent(Event.Godmode))
            {
                player.GodMode = true;
                player.SendInfoMessage("You are now in godmode!");
            }

            if (rt.HasEvent(Event.Pvp) && !player.HasPermission("regiontrigger.bypass.pvp"))
            {
                data.ForcePvP = true;
                if (!player.TPlayer.hostile)
                {
                    player.TPlayer.hostile = true;
                    player.SendData(PacketTypes.TogglePvp, "", player.Index);
                    TSPlayer.All.SendData(PacketTypes.TogglePvp, "", player.Index);
                    player.SendInfoMessage("Your PvP status is forced enabled in this region!");
                }
            }

            if (rt.HasEvent(Event.NoPvp) && !player.HasPermission("regiontrigger.bypass.nopvp"))
            {
                data.ForcePvP = false;
                if (player.TPlayer.hostile)
                {
                    player.TPlayer.hostile = false;
                    player.SendData(PacketTypes.TogglePvp, "", player.Index);
                    TSPlayer.All.SendData(PacketTypes.TogglePvp, "", player.Index);
                    player.SendInfoMessage("You can't enable PvP in this region!");
                }
            }

            if (rt.HasEvent(Event.InvariantPvp) && !player.HasPermission("regiontrigger.bypass.inpvp"))
            {
                data.CanTogglePvP = false;
            }

            if (rt.HasEvent(Event.Private) && !player.HasPermission("regiontrigger.bypass.private"))
            {
                player.Spawn(PlayerSpawnContext.RecallFromItem);
                player.SendErrorMessage("You don't have permission to enter that region.");
            }
        }

        private void OnSecondUpdate()
        {
            foreach (var player in TShock.Players.Where(p => p?.Active == true))
            {
                var dt = RtPlayer.GetPlayerInfo(player);
                var oldRegion = dt.CurrentRegion;
                dt.CurrentRegion = RtRegions.GetCurrentRegion(player);

                if (dt.CurrentRegion != oldRegion)
                {
                    if (oldRegion != null)
                    {
                        OnRegionLeft(player, oldRegion, dt);
                    }

                    if (dt.CurrentRegion != null)
                    {
                        OnRegionEntered(player, dt);
                    }
                }

                if (dt.CurrentRegion == null)
                    continue;

                if (dt.CurrentRegion.HasEvent(Event.Message) && !string.IsNullOrWhiteSpace(dt.CurrentRegion.Message) && dt.CurrentRegion.MsgInterval != 0)
                {
                    if (dt.MsgCd < dt.CurrentRegion.MsgInterval)
                    {
                        dt.MsgCd++;
                    }
                    else
                    {
                        player.SendInfoMessage(dt.CurrentRegion.Message);
                        dt.MsgCd = 0;
                    }
                }
            }
        }

        private static readonly string[] DoNotNeedDelValueProps = {
            "em",
            "lm",
            "mi",
            "tg",
            "msg"
        };

        private static readonly string[][] PropStrings = {
            new[] {"e", "event"},
            new[] {"pb", "proj", "projban"},
            new[] {"ib", "item", "itemban"},
            new[] {"tb", "tile", "tileban"},
            new[] {"em", "entermsg"},
            new[] {"lm", "leavemsg"},
            new[] {"msg", "message"},
            new[] {"mi", "msgitv", "msginterval", "messageinterval"},
            new[] {"tg", "tempgroup"},
            new[] {"tp", "perm", "tempperm", "temppermission"}
        };

        [SuppressMessage("ReSharper", "SwitchStatementMissingSomeCases")]
        private void RegionSetProperties(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("Invalid syntax! Type /rt --help to get instructions.");
                return;
            }

            var cmd = args.Parameters[0].Trim().ToLowerInvariant();
            if (cmd.StartsWith("set-"))
            {
                #region set-prop
                if (args.Parameters.Count < 3)
                {
                    args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /rt set-<prop> <region> [--del] <value>");
                    return;
                }
                var propset = cmd.Substring(4);
                // check the property
                if (!PropStrings.Any(strarray => strarray.Contains(propset)))
                {
                    args.Player.SendErrorMessage("Invalid property!");
                    return;
                }
                // get the shortest representation of property.
                // e.g. event => e, projban => pb
                propset = PropStrings.Single(props => props.Contains(propset))[0];
                // check existance of region
                var region = TShock.Regions.GetRegionByName(args.Parameters[1]);
                if (region == null)
                {
                    args.Player.SendErrorMessage("Invalid region!");
                    return;
                }
                // if region hasn't been added into database
                var rt = RtRegions.GetRtRegionByRegionId(region.ID);
                if (rt == null)
                {
                    RtRegions.AddRtRegion(region.ID);
                    rt = RtRegions.GetRtRegionByRegionId(region.ID);
                }
                // has parameter --del
                var isDel = string.Equals(args.Parameters[2], "--del", StringComparison.OrdinalIgnoreCase);
                // sometimes commands with --del don't need <value> e.g. /rt set-tg <region> --del
                if (isDel && args.Parameters.Count == 3 && !DoNotNeedDelValueProps.Contains(propset))
                {
                    args.Player.SendErrorMessage($"Invalid syntax! Proper syntax: /rt set-{propset} <region> [--del] <value>");
                    return;
                }
                var propValue = isDel && args.Parameters.Count == 3 ? null : isDel
                    ? string.Join(" ", args.Parameters.GetRange(3, args.Parameters.Count - 3))
                    : string.Join(" ", args.Parameters.GetRange(2, args.Parameters.Count - 2));

                try
                {
                    switch (propset)
                    {
                        case "e":
                            var validatedEvents = Events.ValidateEventWhenAdd(propValue, out var invalids);
                            if (!isDel)
                                RtRegions.AddEvents(rt, validatedEvents);
                            else
                                RtRegions.RemoveEvents(rt, validatedEvents);
                            args.Player.SendSuccessMessage("Region {0} has been modified successfully!", region.Name);
                            if (!string.IsNullOrWhiteSpace(invalids))
                                args.Player.SendErrorMessage("Invalid events: {0}", invalids);
                            break;
                        case "pb":
                            if (short.TryParse(propValue, out var id) && id > 0 && id < Main.maxProjectileTypes)
                            {
                                if (!isDel)
                                {
                                    RtRegions.AddProjban(rt, id);
                                    args.Player.SendSuccessMessage("Banned projectile {0} in region {1}.", id, region.Name);
                                }
                                else
                                {
                                    RtRegions.RemoveProjban(rt, id);
                                    args.Player.SendSuccessMessage("Unbanned projectile {0} in region {1}.", id, region.Name);
                                }
                            }
                            else
                                args.Player.SendErrorMessage("Invalid projectile ID!");
                            break;
                        case "ib":
                            var items = TShock.Utils.GetItemByIdOrName(propValue);
                            if (items.Count == 0)
                            {
                                args.Player.SendErrorMessage("Invalid item.");
                            }
                            else if (items.Count > 1)
                            {
                                args.Player.SendMultipleMatchError(items.Select(i => i.Name));
                            }
                            else
                            {
                                if (!isDel)
                                {
                                    RtRegions.AddItemban(rt, items[0].Name);
                                    args.Player.SendSuccessMessage("Banned {0} in region {1}.", items[0].Name, region.Name);
                                }
                                else
                                {
                                    RtRegions.RemoveItemban(rt, items[0].Name);
                                    args.Player.SendSuccessMessage("Unbanned {0} in region {1}.", items[0].Name, region.Name);
                                }
                            }
                            break;
                        case "tb":
                            if (short.TryParse(propValue, out var tileid) && tileid >= 0 && tileid < Main.maxTileSets)
                            {
                                if (!isDel)
                                {
                                    RtRegions.AddTileban(rt, tileid);
                                    args.Player.SendSuccessMessage("Banned tile {0} in region {1}.", tileid, region.Name);
                                }
                                else
                                {
                                    RtRegions.RemoveTileban(rt, tileid);
                                    args.Player.SendSuccessMessage("Unbanned tile {0} in region {1}.", tileid, region.Name);
                                }
                            }
                            else
                                args.Player.SendErrorMessage("Invalid tile ID!");
                            break;
                        case "em":
                            RtRegions.SetEnterMessage(rt, !isDel ? propValue : null);
                            if (!isDel)
                            {
                                args.Player.SendSuccessMessage("Set enter message of region {0} to '{1}'", region.Name, propValue);
                                if (!rt.HasEvent(Event.EnterMsg))
                                    args.Player.SendWarningMessage("Add event ENTERMESSAGE if you want to make it work.");
                            }
                            else
                                args.Player.SendSuccessMessage("Removed enter message of region {0}.", region.Name);
                            break;
                        case "lm":
                            RtRegions.SetLeaveMessage(rt, !isDel ? propValue : null);
                            if (!isDel)
                            {
                                args.Player.SendSuccessMessage("Set leave message of region {0} to '{1}'", region.Name, propValue);
                                if (!rt.HasEvent(Event.LeaveMsg))
                                    args.Player.SendWarningMessage("Add event LEAVEMESSAGE if you want to make it work.");
                            }
                            else
                                args.Player.SendSuccessMessage("Removed leave message of region {0}.", region.Name);
                            break;
                        case "msg":
                            RtRegions.SetMessage(rt, !isDel ? propValue : null);
                            if (!isDel)
                            {
                                args.Player.SendSuccessMessage("Set message of region {0} to '{1}'", region.Name, propValue);
                                if (!rt.HasEvent(Event.Message))
                                    args.Player.SendWarningMessage("Add event MESSAGE if you want to make it work.");
                            }
                            else
                                args.Player.SendSuccessMessage("Removed message of region {0}.", region.Name);
                            break;
                        case "mi":
                            if (isDel)
                                throw new Exception("Invalid usage! Proper usage: /rt set-mi <region> <interval>");
                            if (!int.TryParse(propValue, out var itv) || itv < 0)
                                throw new Exception("Invalid interval. (Interval must be integer >= 0)");
                            RtRegions.SetMsgInterval(rt, itv);
                            args.Player.SendSuccessMessage("Set message interval of region {0} to {1}.", region.Name, itv);
                            if (!rt.HasEvent(Event.Message))
                                args.Player.SendWarningMessage("Add event MESSAGE if you want to make it work.");
                            break;
                        case "tg":
                            if (!isDel && propValue != "null")
                            {
                                RtRegions.SetTempGroup(rt, propValue);
                                args.Player.SendSuccessMessage("Set tempgroup of region {0} to {1}.", region.Name, propValue);
                                if (!rt.HasEvent(Event.TempGroup))
                                    args.Player.SendWarningMessage("Add event TEMPGROUP if you want to make it work.");
                            }
                            else
                            {
                                RtRegions.SetTempGroup(rt, null);
                                args.Player.SendSuccessMessage("Removed tempgroup of region {0}.", region.Name);
                            }
                            break;
                        case "tp":
                            // ReSharper disable once PossibleNullReferenceException
                            var permissions = propValue.ToLower().Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                            if (!isDel)
                            {
                                RtRegions.AddPermissions(rt, permissions);
                                args.Player.SendSuccessMessage("Region {0} has been modified successfully.", region.Name);
                            }
                            else
                            {
                                RtRegions.DeletePermissions(rt, permissions);
                                args.Player.SendSuccessMessage("Region {0} has been modified successfully.", region.Name);
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    args.Player.SendErrorMessage(ex.Message);
                }
                #endregion
            }
            else
                switch (cmd)
                {
                    case "show":
                        #region show
                        {
                            if (args.Parameters.Count != 2)
                            {
                                args.Player.SendErrorMessage("Invalid syntax! Usage: /rt show <region>");
                                return;
                            }

                            var region = TShock.Regions.GetRegionByName(args.Parameters[1]);
                            if (region == null)
                            {
                                args.Player.SendErrorMessage("Invalid region!");
                                return;
                            }
                            var rt = RtRegions.GetRtRegionByRegionId(region.ID);
                            if (rt == null)
                            {
                                args.Player.SendInfoMessage("{0} has not been set up yet. Use: /rt set-<prop> <name> <value>", region.Name);
                                return;
                            }

                            var infos = new List<string> {
                                $"*** Information of region {rt.Region.Name} ***",
                                $" * Events: {rt.Events}",
                                $" * TempGroup: {rt.TempGroup?.Name ?? "None"}",
                                $" * Message & Interval: {rt.Message ?? "None"}({rt.MsgInterval}s)",
                                $" * EnterMessage: {rt.EnterMsg ?? "None"}",
                                $" * LeaveMessage: {rt.LeaveMsg ?? "None"}",
                                $" * Itembans: {(string.IsNullOrWhiteSpace(rt.Itembans) ? "None" : rt.Itembans)}",
                                $" * Projbans: {(string.IsNullOrWhiteSpace(rt.Projbans) ? "None" : rt.Projbans)}",
                                $" * Tilebans: {(string.IsNullOrWhiteSpace(rt.Tilebans) ? "None" : rt.Tilebans)}"
                            };
                            infos.ForEach(args.Player.SendInfoMessage);
                        }
                        #endregion
                        break;
                    case "reload":
                        RtRegions.Reload();
                        args.Player.SendSuccessMessage("Reloaded regions from database successfully.");
                        break;
                    case "--help":
                        #region Help
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out var pageNumber))
                            return;

                        var lines = new List<string>
                        {
                            "*** Usage: /rt set-<prop> <region> [--del] <value>",
                            "           /rt show <region>",
                            "           /rt reload",
                            "           /rt --help [page]",
                            "*** Avaliable properties:"
                        };
                        lines.AddRange(PaginationTools.BuildLinesFromTerms(PropStrings, array =>
                        {
                            var strarray = (string[])array;
                            return $"{strarray[0]}({string.Join("/", strarray.Skip(1))})";
                        }, ",", 75).Select(s => s.Insert(0, "   * ")));
                        lines.Add("*** Available events:");
                        lines.AddRange(Events.EventsDescriptions.Select(pair => $"   * {pair.Key} - {pair.Value}"));

                        PaginationTools.SendPage(args.Player, pageNumber, lines,
                            new PaginationTools.Settings
                            {
                                HeaderFormat = "RegionTrigger Instructions ({0}/{1}):",
                                FooterFormat = "Type {0}rt --help {{0}} for more instructions.".SFormat(Commands.Specifier)
                            }
                        );
                        #endregion
                        break;
                    default:
                        args.Player.SendErrorMessage("Invalid syntax! Type /rt --help for instructions.");
                        return;
                }
        }
    }
}
