using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Tile_Entities;
using Terraria.ID;
using TShockAPI;

namespace WorldEdit.Commands
{
	public class FixGhosts : WECommand
	{
		public FixGhosts(int x, int y, int x2, int y2, TSPlayer plr)
			: base(x, y, x2, y2, plr)
		{
		}

		public override void Execute()
        {
            if (!CanUseCommand()) { return; }
            Tools.PrepareUndo(x, y, x2, y2, plr);
            int signs = 0, frames = 0, chests = 0, sensors = 0, dummies = 0;
            int weaponRacks = 0, pylons = 0, mannequins = 0, hatRacks = 0, foodPlates = 0;
			foreach (Sign sign in Main.sign)
			{
				if (sign == null) continue;
				ushort type = Main.tile[sign.x, sign.y].type;
				if (!Main.tile[sign.x, sign.y].active()
                    || ((type != TileID.Signs)
					&& (type != TileID.Tombstones)
					&& (type != TileID.AnnouncementBox)))
				{
					Sign.KillSign(sign.x, sign.y);
					signs++;
				}
            }
            foreach (Chest chest in Main.chest)
            {
                if (chest == null) continue;
                ushort type = Main.tile[chest.x, chest.y].type;
                if (!Main.tile[chest.x, chest.y].active()
                    || ((type != TileID.Containers)
                    && (type != TileID.Containers2)
                    && (type != TileID.Dressers)))
                {
                    Chest.DestroyChest(chest.x, chest.y);
                    chests++;
                }
            }
            for (int i = x; i <= x2; i++)
			{
				for (int j = y; j <= y2; j++)
				{
                    if (TEItemFrame.Find(i, j) != -1)
                    {
                        if (!Main.tile[i, j].active() || (Main.tile[i, j].type != TileID.ItemFrame))
                        {
                            TEItemFrame.Kill(i, j);
                            frames++;
                        }
                    }
                    else if (TELogicSensor.Find(i, j) != -1)
                    {
                        if (!Main.tile[i, j].active() || (Main.tile[i, j].type != TileID.LogicSensor))
                        {
                            TELogicSensor.Kill(i, j);
                            sensors++;
                        }
                    }
                    else if (TETrainingDummy.Find(i, j) != -1)
                    {
                        if (!Main.tile[i, j].active() || (Main.tile[i, j].type != TileID.TargetDummy))
                        {
                            TETrainingDummy.Kill(i, j);
                            dummies++;
                        }
                    }
                    else if (TEWeaponsRack.Find(i, j) != -1)
                    {
                        if (!Main.tile[i, j].active() || (Main.tile[i, j].type != TileID.WeaponsRack2))
                        {
                            TEWeaponsRack.Kill(i, j);
                            weaponRacks++;
                        }
                    }
                    else if (TETeleportationPylon.Find(i, j) != -1)
                    {
                        if (!Main.tile[i, j].active() || (Main.tile[i, j].type != TileID.TeleportationPylon))
                        {
                            TETeleportationPylon.Kill(i, j);
                            pylons++;
                        }
                    }
                    else if (TEDisplayDoll.Find(i, j) != -1)
                    {
                        if (!Main.tile[i, j].active() || (Main.tile[i, j].type != TileID.DisplayDoll))
                        {
                            TEDisplayDoll.Kill(i, j);
                            mannequins++;
                        }
                    }
                    else if (TEHatRack.Find(i, j) != -1)
                    {
                        if (!Main.tile[i, j].active() || (Main.tile[i, j].type != TileID.HatRack))
                        {
                            TEHatRack.Kill(i, j);
                            hatRacks++;
                        }
                    }
                    else if (TEFoodPlatter.Find(i, j) != -1)
                    {
                        if (!Main.tile[i, j].active() || (Main.tile[i, j].type != TileID.FoodPlatter))
                        {
                            TEFoodPlatter.Kill(i, j);
                            foodPlates++;
                        }
                    }
                }
			}
            ResetSection();

            List<string> ghosts = new List<string>();
            if (signs > 0) { ghosts.Add($"signs ({signs})"); }
            if (frames > 0) { ghosts.Add($"item frames ({frames})"); }
            if (chests > 0) { ghosts.Add($"chests ({chests})"); }
            if (sensors > 0) { ghosts.Add($"logic sensors ({sensors})"); }
            if (dummies > 0) { ghosts.Add($"target dummies ({dummies})"); }
            if (weaponRacks > 0) { ghosts.Add($"weapon racks ({weaponRacks})"); }
            if (pylons > 0) { ghosts.Add($"pylons ({pylons})"); }
            if (mannequins > 0) { ghosts.Add($"mannequins ({mannequins})"); }
            if (hatRacks > 0) { ghosts.Add($"hat racks ({hatRacks})"); }
            if (foodPlates > 0) { ghosts.Add($"food plates ({foodPlates})"); }

            if (ghosts.Count > 0)
            { plr.SendSuccessMessage($"Fixed ghost {string.Join(", ", ghosts)}."); }
            else { plr.SendSuccessMessage("There are no ghost objects in this area."); }
        }
	}
}