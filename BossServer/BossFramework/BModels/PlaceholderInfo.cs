using System.Text.RegularExpressions;
using BossFramework.DB;
using CSScriptLib;
using TShockAPI;

namespace BossFramework.BModels
{
    public class PlaceholderInfo : DBStructBase<PlaceholderInfo>
    {
        public override void Init()
        {
            ResultDelegate = CSScript.Evaluator.CreateDelegate<string>(@"string placeholder(BossFramework.BModels.BEventArgs.BaseEventArgs args){" + EvalString + "}");
            _regex = new Regex(@"\{\s*" + Name + @"\s*\}");
            _nonRegex = new Regex(@"\{\s*" + "!" + Name + @"\s*\}");
        }
        private Regex _regex;
        private Regex _nonRegex;
        public string Name { get; set; }
        public string EvalString { get; set; }
        public MethodDelegate<string> ResultDelegate { get; internal set; }
        public bool Match(string text)
            => _regex is null
            ? text.Contains($"{{{Name}}}") || text.Contains($"{{!{Name}}}")
            : _regex.IsMatch(text) || _nonRegex.IsMatch($"!{text}");
        public string Replace(BEventArgs.BaseEventArgs args, string text)
        {
            if (_nonRegex?.IsMatch(text) == true)
            {
                var match = _nonRegex.Match(text);
                var newText = text;
                match.Groups.Keys.ForEach(k => newText = newText.Replace(match.Groups[k].Value, $"{{{Name}}}"));
                return newText;
            }
            var result = ResultDelegate?.Invoke(new object[] { args });
            if (_regex is null)
                return text.Replace($"{{{Name}}}", result);
            else
            {
                var match = _regex.Match(text);
                var newText = text;
                match.Groups.Keys.ForEach(k => newText = newText.Replace(match.Groups[k].Value, result));
                return newText;
            }
        }
    }
}
