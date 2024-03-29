﻿using OTAPI;
using Terraria;

namespace TerrariaApi.Server.Hooking
{
    public static class ItemHooks
    {
        public static HookManager _hookManager;

        /// <summary>
        /// Attaches any of the OTAPI Item hooks to the existing <see cref="HookManager"/> implementation
        /// </summary>
        /// <param name="hookManager">HookManager instance which will receive the events</param>
        public static void AttachTo(HookManager hookManager)
        {
            _hookManager = hookManager;

            On.Terraria.Item.SetDefaults_int_bool += OnSetDefaults;
            On.Terraria.Item.netDefaults += OnNetDefaults;

            Hooks.Chest.QuickStack += OnQuickStack;
        }

        private static void OnNetDefaults(On.Terraria.Item.orig_netDefaults orig, Item item, int type)
        {
            if (_hookManager.InvokeItemNetDefaults(ref type, item))
                return;

            orig(item, type);
        }

        private static void OnSetDefaults(On.Terraria.Item.orig_SetDefaults_int_bool orig, Item item, int type, bool noMatCheck)
        {
            if (_hookManager.InvokeItemSetDefaultsInt(ref type, item))
                return;

            orig(item, type, noMatCheck);
        }

        private static void OnQuickStack(object sender, Hooks.Chest.QuickStackEventArgs e)
        {
            if (_hookManager.InvokeItemForceIntoChest(Main.chest[e.ChestIndex], e.Item, Main.player[e.PlayerId]))
            {
                e.Result = HookResult.Cancel;
            }
        }
    }
}
