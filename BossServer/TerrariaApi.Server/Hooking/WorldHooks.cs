using OTAPI;
using Terraria;

namespace TerrariaApi.Server.Hooking
{
    public static class WorldHooks
    {
        public static HookManager _hookManager;

        /// <summary>
        /// Attaches any of the OTAPI World hooks to the existing <see cref="HookManager"/> implementation
        /// </summary>
        /// <param name="hookManager">HookManager instance which will receive the events</param>
        public static void AttachTo(HookManager hookManager)
        {
            _hookManager = hookManager;

            On.Terraria.IO.WorldFile.LoadWorld += OnLoadWorld;
            On.Terraria.IO.WorldFile.SaveWorld += WorldFile_SaveWorld;
            On.Terraria.IO.WorldFile.SaveWorld_bool_bool += WorldFile_SaveWorld2;
            On.Terraria.WorldGen.StartHardmode += WorldGen_StartHardmode;
            On.Terraria.WorldGen.SpreadGrass += WorldGen_SpreadGrass;
            On.Terraria.Main.checkXMas += Main_checkXMas;
            On.Terraria.Main.checkHalloween += Main_checkHalloween;

            Hooks.Collision.PressurePlate += OnPressurePlate;
            Hooks.WorldGen.Meteor += OnDropMeteor;
        }

        public static void OnLoadWorld(On.Terraria.IO.WorldFile.orig_LoadWorld orig, bool loadFromCloud)
        {
            if (_hookManager.InvokeWorldLoad())
                return;

            orig(loadFromCloud);

            _hookManager.InvokePostWorldLoad();
        }

        public static void OnPressurePlate(object sender, Hooks.Collision.PressurePlateEventArgs e)
        {
            if (e.Entity is NPC npc)
            {
                if (_hookManager.InvokeNpcTriggerPressurePlate(npc, e.X, e.Y))
                    e.Result = HookResult.Cancel;
            }
            else if (e.Entity is Player player)
            {
                if (_hookManager.InvokePlayerTriggerPressurePlate(player, e.X, e.Y))
                    e.Result = HookResult.Cancel;
            }
            else if (e.Entity is Projectile projectile)
            {
                if (_hookManager.InvokeProjectileTriggerPressurePlate(projectile, e.X, e.Y))
                    e.Result = HookResult.Cancel;
            }
        }
        public static void WorldFile_SaveWorld(On.Terraria.IO.WorldFile.orig_SaveWorld orig)
        {
            if (_hookManager.InvokeWorldSave(false))
                return;

            orig();

            _hookManager.InvokePostWorldSave(false);
        }
        public static void WorldFile_SaveWorld2(On.Terraria.IO.WorldFile.orig_SaveWorld_bool_bool orig, bool useCloudSaving, bool resetTime)
        {
            if (_hookManager.InvokeWorldSave(resetTime))
                return;

            orig(useCloudSaving, resetTime);

            _hookManager.InvokePostWorldSave(resetTime);
        }

        public static void WorldGen_StartHardmode(On.Terraria.WorldGen.orig_StartHardmode orig)
        {
            if (_hookManager.InvokeWorldStartHardMode())
                return;

            orig();
        }

        public static void OnDropMeteor(object sender, Hooks.WorldGen.MeteorEventArgs e)
        {
            if (_hookManager.InvokeWorldMeteorDrop(e.X, e.Y))
            {
                e.Result = HookResult.Cancel;
            }
        }

        public static void Main_checkXMas(On.Terraria.Main.orig_checkXMas orig)
        {
            if (_hookManager.InvokeWorldChristmasCheck(ref Terraria.Main.xMas))
                return;

            orig();
        }

        public static void Main_checkHalloween(On.Terraria.Main.orig_checkHalloween orig)
        {
            if (_hookManager.InvokeWorldHalloweenCheck(ref Main.halloween))
                return;

            orig();
        }

        public static void WorldGen_SpreadGrass(On.Terraria.WorldGen.orig_SpreadGrass orig, int i, int j, int dirt, int grass, bool repeat, byte color)
        {
            if (_hookManager.InvokeWorldGrassSpread(i, j, dirt, grass, repeat, color))
                return;

            orig(i, j, dirt, grass, repeat, color);
        }
    }
}
