using System.Text;
using TShockAPI;

namespace BadgeSystem
{
	public sealed class PlayerData
	{
		public const string Key = "badgesys";

		private readonly TSPlayer _player;

		private string _prefix;

		private readonly List<Badge> _total;

		private readonly List<Badge> _current;

		public string Prefix
		{
			get
			{
				if (_prefix == null)
				{
					BuildPrefix();
				}
				return _prefix;
			}
		}

		public IReadOnlyList<Badge> Total => _total;

		public IReadOnlyList<Badge> Current => _current;

		public bool Any => _current.Any();

		public static PlayerData GetPlayerData(TSPlayer player)
		{
			if (player == null)
			{
				throw new ArgumentNullException("player");
			}
			PlayerData playerData = player.GetData<PlayerData>("badgesys");
			if (playerData == null)
			{
				Tuple<IEnumerable<string>, IEnumerable<string>> tuple = BadgeSystem.Badges.Load(player.Account.ID);
				IEnumerable<Badge> total = LoadBadges(tuple.Item1);
				IEnumerable<Badge> current = LoadBadges(tuple.Item2);
				playerData = new PlayerData(player, total, current);
				player.SetData("badgesys", playerData);
			}
			return playerData;
		}

		internal PlayerData(TSPlayer player, IEnumerable<Badge> total, IEnumerable<Badge> current)
		{
			_player = player;
			_total = new List<Badge>(total);
			_current = new List<Badge>(current);
		}

		public void Add(Badge b)
		{
			_total.RemoveAll((Badge i) => i.Identifier == b.Identifier);
			_current.RemoveAll((Badge i) => i.Identifier == b.Identifier);
			_current.Add(b);
			_total.Add(b);
			BadgeSystem.Badges.Update(_player.Account.ID, this);
			_prefix = null;
		}

		public void Remove(Badge b)
		{
			int num = _total.RemoveAll((Badge i) => i.Identifier == b.Identifier);
			int num2 = _current.RemoveAll((Badge i) => i.Identifier == b.Identifier);
			if (num != 0 || num2 != 0)
			{
				BadgeSystem.Badges.Update(_player.Account.ID, this);
				_prefix = null;
			}
		}

		public void RemoveCurrent(Badge b)
		{
			if (_current.RemoveAll((Badge i) => i.Identifier == b.Identifier) != 0)
			{
				BadgeSystem.Badges.Update(_player.Account.ID, this);
				_prefix = null;
			}
		}

		private void BuildPrefix()
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (Badge item in _current)
			{
				stringBuilder.AppendFormat("[c/{0}:{1}]", item.ColorHex, item.Content);
			}
			_prefix = stringBuilder.ToString();
		}

		private static IEnumerable<Badge> LoadBadges(IEnumerable<string> strs)
		{
			return (from str in strs
				select BadgeSystem.Config.Badges.FirstOrDefault((Badge x) => string.Equals(str, x?.Identifier, StringComparison.Ordinal)) into b
				where b != null
				select b).ToList();
		}
	}
}
