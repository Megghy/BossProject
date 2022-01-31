using Terraria;
using Terraria.GameContent.Tile_Entities;
using Terraria.ID;
using TShockAPI;

namespace WorldEdit.Commands
{
	public class Activate : WECommand
	{
		private readonly int _action;
		public Activate(int x, int y, int x2, int y2, TSPlayer plr, byte action)
			: base(x, y, x2, y2, null, plr)
		{
			_action = action;
		}

		public override void Execute()
		{
            if (!CanUseCommand()) { return; }
            Tools.PrepareUndo(x, y, x2, y2, plr);
            int noMsg = 0;
            #region Signs

            if ((_action == 255) || (_action == 0))
            {
                int success = 0, failure = 0;
                for (int i = x; i <= x2; i++)
                {
                    for (int j = y; j <= y2; j++)
                    {
                        if ((Main.tile[i, j].type == TileID.Signs
                            || Main.tile[i, j].type == TileID.Tombstones
                            || Main.tile[i, j].type == TileID.AnnouncementBox)
                            && Main.tile[i, j].frameX % 36 == 0
                            && Main.tile[i, j].frameY == 0
                            && Sign.ReadSign(i, j, false) == -1)
                        {
                            int sign = Sign.ReadSign(i, j);
                            if (sign == -1) failure++;
                            else success++;
                        }
                    }
                }
                if (success > 0 || failure > 0)
                    plr.SendSuccessMessage("Activated signs. ({0}){1}", success,
                        failure > 0 ? " Failed to activate signs. (" + failure + ")" : "");
                else noMsg++;
            }

            #endregion
            #region Chests

            if ((_action == 255) || (_action == 1))
            {
                int success = 0, failure = 0;
                for (int i = x; i <= x2; i++)
                {
                    for (int j = y; j <= y2; j++)
                    {
                        if ((Main.tile[i, j].type == TileID.Containers
                            || Main.tile[i, j].type == TileID.Containers2
                            || Main.tile[i, j].type == TileID.Dressers)
                            && Main.tile[i, j].frameX % 36 == 0
                            && Main.tile[i, j].frameY == 0
                            && Chest.FindChest(i, j) == -1)
                        {
                            int chest = Chest.CreateChest(i, j);
                            if (chest == -1) failure++;
                            else success++;
                        }
                    }
                }
                if (success > 0 || failure > 0)
                    plr.SendSuccessMessage("Activated chests. ({0}){1}", success,
                        failure > 0 ? " Failed to activate chests. (" + failure + ")" : "");
                else noMsg++;
            }

            #endregion
            #region ItemFrames

            if ((_action == 255) || (_action == 2))
            {
                int success = 0, failure = 0;
                for (int i = x; i <= x2; i++)
                {
                    for (int j = y; j <= y2; j++)
                    {
                        if (Main.tile[i, j].type == TileID.ItemFrame
                            && Main.tile[i, j].frameX % 36 == 0
                            && Main.tile[i, j].frameY == 0
                            && TEItemFrame.Find(i, j) == -1)
                        {
                            int frame = TEItemFrame.Place(i, j);
                            if (frame == -1) failure++;
                            else success++;
                        }
                    }
                }
                if (success > 0 || failure > 0)
                    plr.SendSuccessMessage("Activated item frames. ({0}){1}", success,
                        failure > 0 ? " Failed to activate item frames. (" + failure + ")" : "");
                else noMsg++;
            }

            #endregion
            #region LogicSensors

            if ((_action == 255) || (_action == 3))
            {
                int success = 0, failure = 0;
                for (int i = x; i <= x2; i++)
                {
                    for (int j = y; j <= y2; j++)
                    {
                        if (Main.tile[i, j].type == TileID.LogicSensor
                            && TELogicSensor.Find(i, j) == -1)
                        {
                            int sensor = TELogicSensor.Place(i, j);
                            if (sensor == -1) failure++;
                            else
                            {
                                ((TELogicSensor)TELogicSensor.ByID[sensor]).logicCheck =
                                    (TELogicSensor.LogicCheckType)((Main.tile[i, j].frameY / 18) + 1);
                                success++;
                            }
                        }
                    }
                }
                if (success > 0 || failure > 0)
                    plr.SendSuccessMessage("Activated logic sensors. ({0}){1}", success,
                        failure > 0 ? " Failed to activate logic sensors. (" + failure + ")" : "");
                else noMsg++;
            }

            #endregion
            #region TargetDummies

            if ((_action == 255) || (_action == 4))
            {
                int success = 0, failure = 0;
                for (int i = x; i <= x2; i++)
                {
                    for (int j = y; j <= y2; j++)
                    {
                        if (Main.tile[i, j].type == TileID.TargetDummy
                            && Main.tile[i, j].frameX % 36 == 0
                            && Main.tile[i, j].frameY == 0
                            && TETrainingDummy.Find(i, j) == -1)
                        {
                            int dummy = TETrainingDummy.Place(i, j);
                            if (dummy == -1) failure++;
                            else success++;
                        }
                    }
                }
                if (success > 0 || failure > 0)
                    plr.SendSuccessMessage("Activated target dummies. ({0}){1}", success,
                        failure > 0 ? " Failed to activate target dummies. (" + failure + ")" : "");
                else noMsg++;
            }

            #endregion            
            #region WeaponRacks

            if ((_action == 255) || (_action == 5))
            {
                int success = 0, failure = 0;
                for (int i = x; i <= x2; i++)
                {
                    for (int j = y; j <= y2; j++)
                    {
                        if (Main.tile[i, j].type == TileID.WeaponsRack2
                            && Main.tile[i, j].frameX % 54 == 0
                            && Main.tile[i, j].frameY == 0
                            && TEWeaponsRack.Find(i, j) == -1)
                        {
                            if (TEWeaponsRack.Place(i, j) == -1) failure++;
                            else success++;
                        }
                    }
                }
                if (success > 0 || failure > 0)
                    plr.SendSuccessMessage("Activated weapon racks. ({0}){1}", success,
                        failure > 0 ? " Failed to activate weapon racks. (" + failure + ")" : "");
                else noMsg++;
            }

            #endregion
            #region Pylons

            if ((_action == 255) || (_action == 6))
            {
                int success = 0, failure = 0;
                for (int i = x; i <= x2; i++)
                {
                    for (int j = y; j <= y2; j++)
                    {
                        if (Main.tile[i, j].type == TileID.TeleportationPylon
                            && Main.tile[i, j].frameX % 54 == 0
                            && Main.tile[i, j].frameY == 0
                            && TETeleportationPylon.Find(i, j) == -1)
                        {
                            if (TETeleportationPylon.Place(i, j) == -1) failure++;
                            else success++;
                        }
                    }
                }
                if (success > 0 || failure > 0)
                    plr.SendSuccessMessage("Activated pylons. ({0}){1}", success,
                        failure > 0 ? " Failed to activate pylons. (" + failure + ")" : "");
                else noMsg++;
            }

            #endregion
            #region Mannequins

            if ((_action == 255) || (_action == 7))
            {
                int success = 0, failure = 0;
                for (int i = x; i <= x2; i++)
                {
                    for (int j = y; j <= y2; j++)
                    {
                        if (Main.tile[i, j].type == TileID.DisplayDoll
                            && Main.tile[i, j].frameX % 36 == 0
                            && Main.tile[i, j].frameY == 0
                            && TEDisplayDoll.Find(i, j) == -1)
                        {
                            if (TEDisplayDoll.Place(i, j) == -1) failure++;
                            else success++;
                        }
                    }
                }
                if (success > 0 || failure > 0)
                    plr.SendSuccessMessage("Activated mannequins. ({0}){1}", success,
                        failure > 0 ? " Failed to activate mannequins. (" + failure + ")" : "");
                else noMsg++;
            }

            #endregion
            #region HatRacks

            if ((_action == 255) || (_action == 8))
            {
                int success = 0, failure = 0;
                for (int i = x; i <= x2; i++)
                {
                    for (int j = y; j <= y2; j++)
                    {
                        if (Main.tile[i, j].type == TileID.HatRack
                            && Main.tile[i, j].frameX % 54 == 0
                            && Main.tile[i, j].frameY == 0
                            && TEHatRack.Find(i, j) == -1)
                        {
                            if (TEHatRack.Place(i, j) == -1) failure++;
                            else success++;
                        }
                    }
                }
                if (success > 0 || failure > 0)
                    plr.SendSuccessMessage("Activated hat racks. ({0}){1}", success,
                        failure > 0 ? " Failed to activate hat racks. (" + failure + ")" : "");
                else noMsg++;
            }

            #endregion
            #region FoodPlates

            if ((_action == 255) || (_action == 9))
            {
                int success = 0, failure = 0;
                for (int i = x; i <= x2; i++)
                {
                    for (int j = y; j <= y2; j++)
                    {
                        if (Main.tile[i, j].type == TileID.FoodPlatter
                            && TEFoodPlatter.Find(i, j) == -1)
                        {
                            if (TEFoodPlatter.Place(i, j) == -1) failure++;
                            else success++;
                        }
                    }
                }
                if (success > 0 || failure > 0)
                    plr.SendSuccessMessage("Activated food plates. ({0}){1}", success,
                        failure > 0 ? " Failed to activate food plates. (" + failure + ")" : "");
                else noMsg++;
            }

            #endregion
            if (noMsg == 10)
            { plr.SendSuccessMessage("There are no objects to activate in this area."); }
            ResetSection();
		}
	}
}