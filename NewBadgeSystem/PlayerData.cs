using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;

namespace BadgeSystem
{
	public sealed class PlayerData
	{
		public const string Key = "badgesys";
		private readonly TSPlayer _player;
		private string _prefix;
		private readonly List<Content> _totalBrackets;
		private readonly List<Content> _currentBrackets;
		private readonly List<Content> _totalPrefix;
		private readonly List<Content> _currentPrefix;
		private readonly List<Content> _totalSuffix;
		private readonly List<Content> _currentSuffix;

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

		public IReadOnlyList<Content> TotalBrackets => _totalBrackets;
		public IReadOnlyList<Content> CurrentBrackets => _currentBrackets;

		public IReadOnlyList<Content> TotalPrefix => _totalPrefix;
		public IReadOnlyList<Content> CurrentPrefix => _currentPrefix;

		public IReadOnlyList<Content> TotalSuffix => _totalSuffix;
		public IReadOnlyList<Content> CurrentSuffix => _currentSuffix;


		public bool Any => _currentPrefix.Any()|| _currentPrefix.Any()|| _currentBrackets.Any();
		public static PlayerData GetPlayerData(TSPlayer player)
		{
			if (player == null)
			{
				throw new ArgumentNullException("player");
			}
			PlayerData playerData = player.GetData<PlayerData>("badgesys");
			if (playerData == null)
			{

				Tuple<IEnumerable<string>, IEnumerable<string>>[] tuples = BadgeSystem.Badges.Load(player.Account.ID);
				TShock.Log.ConsoleInfo(tuples.Count().ToString());
				IEnumerable<Content> TotalBrackets = LoadContent(tuples[0].Item1);
				IEnumerable<Content> CurrentBrackets = LoadContent(tuples[0].Item2);
				IEnumerable<Content> TotalPrefix = LoadContent(tuples[1].Item1);
				IEnumerable<Content> CurrentPrefix = LoadContent(tuples[1].Item2);
				IEnumerable<Content> TotalSuffix = LoadContent(tuples[2].Item1);
				IEnumerable<Content> CurrentSuffix = LoadContent(tuples[2].Item2);
				playerData = new PlayerData(player, TotalBrackets, CurrentBrackets, TotalPrefix, CurrentPrefix, TotalSuffix, CurrentSuffix);
				player.SetData("badgesys", playerData);
			}
			return playerData;
		}
		internal PlayerData(TSPlayer player, IEnumerable<Content> TotalBrackets, IEnumerable<Content> CurrentBrackets, IEnumerable<Content> TotalPrefix, IEnumerable<Content> CurrentPrefix, IEnumerable<Content> TotalSuffix, IEnumerable<Content> CurrentSuffix)
		{
			_player = player;
			_totalBrackets = new List<Content>(TotalBrackets);
			_currentBrackets = new List<Content>(CurrentBrackets);
			_totalPrefix = new List<Content>(TotalPrefix);
			_currentPrefix = new List<Content>(CurrentPrefix);
			_totalSuffix = new List<Content>(TotalSuffix);
			_currentSuffix = new List<Content>(CurrentSuffix);
		}
		public void AddContent(Content b, String type)
		{
			if (type == "prefix")
			{
				_totalPrefix.RemoveAll((Content i) => i.Identifier == b.Identifier);
				_totalPrefix.Add(b);
			}
			else if (type == "suffix")
			{
				_totalSuffix.RemoveAll((Content i) => i.Identifier == b.Identifier);
				_totalSuffix.Add(b);
			}else if (type == "brackets")
			{
				_totalBrackets.RemoveAll((Content i) => i.Identifier == b.Identifier);
				_totalBrackets.Add(b);
			}
		   BadgeSystem.Badges.Update(_player.Account.ID, this);
			_prefix = null;
		}
		public void AddCurrentContent(Content b, String type)
		{
			if (type == "prefix")
			{
				_totalPrefix.RemoveAll((Content i) => i.Identifier == b.Identifier);
				_currentPrefix.RemoveAll((Content i) => i.Identifier == b.Identifier);
				_totalPrefix.Add(b);
				_currentPrefix.Add(b);
			}
			else if (type == "suffix")
			{
				_totalSuffix.RemoveAll((Content i) => i.Identifier == b.Identifier);
				_currentSuffix.RemoveAll((Content i) => i.Identifier == b.Identifier);
				_totalSuffix.Add(b);
				_currentSuffix.Add(b);
			}
			else if (type == "brackets")
			{
				_totalBrackets.RemoveAll((Content i) => i.Identifier == b.Identifier);
				_currentBrackets.RemoveAll((Content i) => i.Identifier == b.Identifier);
				_totalBrackets.Add(b);
				_currentBrackets.Add(b);
			}
			BadgeSystem.Badges.Update(_player.Account.ID, this);
			_prefix = null;
		}
		public void RemoveContent(Content a, String type)
		{
			int num = 0, num2 = 0;
			if (type == "prefix")
			{
				num = _totalPrefix.RemoveAll((Content i) => i.Identifier == a.Identifier);
				num2 = _currentPrefix.RemoveAll((Content i) => i.Identifier == a.Identifier);
				
			}
			else if (type == "suffix")
			{
				num = _totalSuffix.RemoveAll((Content i) => i.Identifier == a.Identifier);
				num2 = _currentSuffix.RemoveAll((Content i) => i.Identifier == a.Identifier);

			}
			else if (type == "brackets")
			{
				num = _totalBrackets.RemoveAll((Content i) => i.Identifier == a.Identifier);
				num2 = _currentBrackets.RemoveAll((Content i) => i.Identifier == a.Identifier);
			}
			if (num != 0 || num2 != 0)
			{
				BadgeSystem.Badges.Update(_player.Account.ID, this);
				_prefix = null;
			}
		}
		public void RemoveCurrentContent(Content b ,string type)
        {
            if (type == "prefix")
            {
				if (_currentPrefix.RemoveAll((Content i) => i.Identifier == b.Identifier) != 0)
				{
					BadgeSystem.Badges.Update(_player.Account.ID, this);
					_prefix = null;
				}
            }
            else if (type == "suffix")
			{
				if (_currentSuffix.RemoveAll((Content i) => i.Identifier == b.Identifier) != 0)
				{
					BadgeSystem.Badges.Update(_player.Account.ID, this);
					_prefix = null;
				}
			}else if(type == "brackets")
            {
				if (_currentBrackets.RemoveAll((Content i) => i.Identifier == b.Identifier) != 0)
				{
					BadgeSystem.Badges.Update(_player.Account.ID, this);
					_prefix = null;
				}
			}
            
        }
		public void RemoveCurrentAll(string type)
		{
			if (type == "prefix")
			{
				_currentPrefix.Clear();
					BadgeSystem.Badges.Update(_player.Account.ID, this);
					_prefix = null;
			}
			else if (type == "suffix")
			{
				_currentSuffix.Clear();
				BadgeSystem.Badges.Update(_player.Account.ID, this);
				_prefix = null;
			}
			else if (type == "brackets")
			{
				_currentBrackets.Clear();
				BadgeSystem.Badges.Update(_player.Account.ID, this);
				_prefix = null;
			}
		}
		private void BuildPrefix()
		{
			Content bracketsContent = new Content("[,]", "", "ffffff", "brackets"); ;
			Content prefixContent =  new Content("", "-1", "ffffff", "prefix"); ;
			Content suffixContent = new Content("Íæ¼Ò", "-1", "ffffff", "suffix"); ;
			StringBuilder stringBuilder = new StringBuilder();
            if (_currentBrackets.Count() > 0)
            {
				bracketsContent = _currentBrackets.First();
			}
			if (_currentPrefix.Count() > 0)
			{
				prefixContent = _currentPrefix.First();
			}
			if (_currentSuffix.Count() > 0)
			{
				suffixContent = _currentSuffix.First();
			}
      
			string[] bracket = bracketsContent.ContentValue.Split(',');
			TShock.Log.ConsoleInfo("count: "+_currentPrefix.Count().ToString() +"   "+ prefixContent.ContentValue);
			TShock.Log.ConsoleInfo("count: "+_currentSuffix.Count().ToString() + "   " + suffixContent.ContentValue);
			string startBracket =  "[c/" + bracketsContent.ColorHex + ":" + bracket[0] + "]";
			string endBracket =		"[c/" + bracketsContent.ColorHex + ":" + bracket[1] + "]";
			string prefix = prefixContent.ContentValue;
			string subffix = suffixContent.ContentValue;
			stringBuilder.AppendFormat(startBracket+ prefix+ subffix+ endBracket);

            _prefix = stringBuilder.ToString();
		}
		private static IEnumerable<Content> LoadContent(IEnumerable<string> strs)
		{
			return (from str in strs
					select BadgeSystem.ContentConfig.Content.FirstOrDefault((Content x) => string.Equals(str, x?.Identifier, StringComparison.Ordinal)) into b
					where b != null
					select b).ToList();
		}
	}
}
