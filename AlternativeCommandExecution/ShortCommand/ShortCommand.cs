using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AlternativeCommandExecution.ShortCommand
{
	public sealed class ShortCommand
	{
		public static ShortCommand Create(string desc, string[] commandLines, params string[] names)
		{
			if (names.Length == 0)
			{
				throw new ArgumentException("需要至少一个参数名。", nameof(names));
			}

			var sc = new ShortCommand(desc, commandLines, names);

			sc.InitializeParameters();
			sc.InitializeCommandLines();
			sc.InitializeHelpText();

			return sc;
		}

		private ShortCommand(string desc, string[] lines, string[] names)
		{
			_parameterDescription = desc;
			_commandLines = (string[])lines.Clone();

			Names = (string[])names.Clone();
		}

		public string[] Names { get; }

		public Parameter[] Parameters { get; private set; }

		public string[] FormatLines { get; private set; }

		public string ParameterHelpText { get; private set; }

		private byte _fewestParamCount;

		public string[] Convert(CommandExectionContext ctx, string[] args)
		{
			if (args.Length < _fewestParamCount)
			{
				throw new LackOfArgumentException("语法无效！正确语法：" + TShockAPI.Commands.Specifier + ParameterHelpText);
			}

			var values = new string[Parameters.Length];
			for (var index = 0; index < values.Length; index++)
			{
				values[index] = Parameters[index].ToString(ctx, args.ElementAtOrDefault(index));
			}

			return FormatLines.Select(x => string.Format(x, values/*.Select(v => (object)v).ToArray()*/)).ToArray();
		}

		public bool HasName(string name) => Names.Any(x => x.Equals(name, StringComparison.Ordinal));

		private void InitializeParameters()
		{
			var args = new List<Parameter>();

			var state = ParseState.Normal;

			var argName = new StringBuilder();
			var defaultValue = new StringBuilder();
			var kind = ParameterType.Required;

			void InternalReset()
			{
				state = ParseState.Normal;
				argName.Clear();
				defaultValue.Clear();
				kind = ParameterType.Required;
			}

			for (var index = 0; index < _parameterDescription.Length; index++)
			{
				var c = _parameterDescription[index];

				switch (c)
				{
					case LeftBracket:
						if (state != ParseState.Normal)
						{
							throw new CommandParseException($"invalid {c} here", _parameterDescription, index);
						}
						state = ParseState.InsideBracket;
						continue;
					case RightBracket:
						if (state != ParseState.InsideBracket)
						{
							throw new CommandParseException($"invalid {c} here", _parameterDescription, index);
						}
						if (argName.Length == 0)
						{
							throw new CommandParseException("Argument must have a name", _parameterDescription, index);
						}
						args.Add(new Parameter(kind, argName.ToString(), defaultValue.ToString()));
						InternalReset();
						state = ParseState.Normal;
						continue;
				}

				switch (state)
				{
					case ParseState.Normal:
						// do nothing
						break;
					case ParseState.InsideBracket:
						if (VanRegex.IsMatch(c.ToString()))
						{
							argName.Append(c);
						}
						else
						{
							switch (c)
							{
								case DefaultValueRepresentation:
									while ((c = _parameterDescription[++index]) != RightBracket)
									{
										defaultValue.Append(c);
									}
									index--;
									kind = ParameterType.DefaultValue;
									continue;
								case SpecialValueRepresentation: // special value
									var special = new StringBuilder();
									while ((c = _parameterDescription[++index]) != RightBracket)
									{
										special.Append(c);
									}
									index--;
									argName = special;
									switch (special.ToString().Trim())
									{
										case "Player":
											kind = ParameterType.PlayerName;
											continue;
									}
									continue;
								case OptionalRepresentation: // optional
									if (argName.Length != 0)
									{
										throw new CommandParseException("Wrong position for " + OptionalRepresentation, _parameterDescription, index);
									}
									kind = ParameterType.NotRequired;
									continue;
								default:
									throw new CommandParseException("Invalid character " + c, _parameterDescription, index);
							}
						}
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			if (state != ParseState.Normal)
			{
				throw new CommandParseException("Unexpected ending", _parameterDescription, _parameterDescription.Length - 1);
			}

			Parameters = args.ToArray();
			_fewestParamCount = (byte)Parameters.Count(x => x.Type == ParameterType.Required);
		}

		private void InitializeCommandLines()
		{
			var lines = new List<string>();

			foreach (var line in _commandLines)
			{
				var state = ParseState.Normal;
				var argName = new StringBuilder();

				var format = new StringBuilder();

				foreach (var c in line)
				{
					switch (c)
					{
						case LeftBracket:
							if (state != ParseState.Normal)
							{
								throw new CommandParseException($"invalid {c} here");
							}
							state = ParseState.InsideBracket;
							continue;
						case RightBracket:
							if (state != ParseState.InsideBracket)
							{
								throw new CommandParseException($"invalid {c} here");
							}
							var name = argName.ToString();
							if (name.Length == 0)
							{
								throw new CommandParseException("Parameter must have a name");
							}

							var formatIndex = Array.FindIndex(Parameters, x => x.Name.Equals(name, StringComparison.Ordinal));
							if (formatIndex == -1)
							{
								throw new CommandParseException("Undeclared parameter: " + name);
							}
							format.AppendFormat("{{{0}}}", formatIndex);

							argName.Clear();
							state = ParseState.Normal;
							continue;
					}

					switch (state)
					{
						case ParseState.Normal:
							format.Append(c);
							break;
						case ParseState.InsideBracket:
							if (VanRegex.IsMatch(c.ToString()))
							{
								argName.Append(c);
							}
							break;
					}
				}

				lines.Add(format.ToString());
			}

			FormatLines = lines.ToArray();
		}

		private void InitializeHelpText()
		{
			var sb = new StringBuilder(Names[0]);

			const string requiredFormat = " <{0}>";
			const string notRequiredFormat = " [{0}]";

			foreach (var p in Parameters)
			{
				sb.AppendFormat(p.Type == ParameterType.Required ? requiredFormat : notRequiredFormat, p.Name);
			}

			ParameterHelpText = sb.ToString();
		}

		private readonly string _parameterDescription;

		private readonly string[] _commandLines;

		private static readonly Regex VanRegex = new Regex(ValidateParameterNameRegex, RegexOptions.Compiled);

		private const char OptionalRepresentation = '%';

		private const char LeftBracket = '{';

		private const char RightBracket = '}';

		private const char DefaultValueRepresentation = '|';

		private const char SpecialValueRepresentation = '$';

		private const string ValidateParameterNameRegex = @"[\w\-]";

		private enum ParseState : byte
		{
			Normal,
			InsideBracket
		}
	}
}
