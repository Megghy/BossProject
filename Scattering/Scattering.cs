using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;

namespace Scattering
{
    [ApiVersion(2, 1)]
	public class Scattering : TerrariaPlugin
	{
		internal HomeManager Hm;

		public override string Name => GetType().Name;

		public override string Author => "MistZZT";

		public override string Description => "散花";

		public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

		public Scattering(Main game)
			: base(game)
		{
		}

		public override void Initialize()
		{
			ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInit);
			Commands.ChatCommands.Add(new Command("scattering.tphome", TpHome, "tph", "tphome")
			{
				AllowServer = false
			});
			Commands.ChatCommands.Add(new Command("scattering.sethome", SetHome, "sh", "sethome")
			{
				AllowServer = false
			});
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInit);
			}
			base.Dispose(disposing);
		}

		private void OnPostInit(EventArgs args)
		{
			Hm = new HomeManager(TShock.DB);
			Hm.Reload();
		}

		private void SetHome(CommandArgs args)
		{
			if (args.Parameters.Count == 0)
			{
				args.Player.SendErrorMessage("语法无效! 正确语法: /sethome <名>{0}", args.Player.HasPermission("scattering.setother") ? " [玩家名]" : string.Empty);
				return;
			}
			string text = args.Parameters[0];
			if (string.IsNullOrWhiteSpace(text))
			{
				args.Player.SendErrorMessage("存档点名称无效!");
				return;
			}
			if (args.Parameters.Count == 1)
			{
				Hm.Add(args.Player.Account.ID, text, (int)args.Player.X, (int)args.Player.Y);
				args.Player.SendSuccessMessage("成功保存{0}存档点.", text);
				return;
			}
			string text2 = string.Join(" ", args.Parameters.Skip(1));
			if (string.IsNullOrWhiteSpace(text2))
			{
				args.Player.SendErrorMessage("玩家名无效!");
				return;
			}
			UserAccount userAccount = null;
			List<TSPlayer> list = TSPlayer.FindByNameOrID(text2);
			if (list.Count > 1)
			{
				args.Player.SendMultipleMatchError(list.Select((TSPlayer p) => p.Name));
				return;
			}
			if (list.Count == 1)
			{
				userAccount = list.Single().Account;
			}
			if (list.Count == 0)
			{
				userAccount = TShock.UserAccounts.GetUserAccountByName(text2);
			}
			if (userAccount == null)
			{
				args.Player.SendErrorMessage("玩家名无效!");
				return;
			}
			Hm.Add(userAccount.ID, text, (int)args.Player.X, (int)args.Player.Y);
			args.Player.SendSuccessMessage("成功保存{0}的{1}存档点.", userAccount.Name, text);
		}

		private void TpHome(CommandArgs args)
		{
			if (args.Parameters.Count == 0)
			{
				args.Player.SendErrorMessage("语法无效! 正确语法: /tphome <名>");
				return;
			}
			string name = string.Join(" ", args.Parameters);
			PlayerHome playerHome = Hm.Find(args.Player.Account.ID, name);
			if (playerHome == null)
			{
				args.Player.SendErrorMessage("未找到存档点!");
			}
			else if (args.Player.Teleport(playerHome.X, playerHome.Y, 1))
			{
				args.Player.SendSuccessMessage("成功回到{0}存档点.", playerHome.Name);
			}
		}
	}
}
