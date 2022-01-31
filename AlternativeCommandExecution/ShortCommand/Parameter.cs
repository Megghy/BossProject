using System;
using System.Runtime.InteropServices;

namespace AlternativeCommandExecution.ShortCommand
{
	[StructLayout(LayoutKind.Auto)]
	public struct Parameter
	{
		public Parameter(ParameterType type, string name, string defaultValue)
		{
			Type = type;
			DefaultValue = defaultValue;
			Name = name;
		}

		public ParameterType Type { get; }

		public string DefaultValue { get; }

		public string Name { get; }

		public override string ToString()
		{
			return string.Format("Name: {0}, Type: {1}, Default: {2}",
										Name,
												Type,
									string.IsNullOrWhiteSpace(DefaultValue) ? "null" : DefaultValue);
		}

		public string ToString(CommandExectionContext context, string argument = null)
		{
			switch (Type)
			{
				case ParameterType.DefaultValue:
				case ParameterType.NotRequired:
					{
						return (argument ?? DefaultValue) ?? string.Empty;
					}
				case ParameterType.Required:
					{
						if (string.IsNullOrWhiteSpace(argument))
						{
							throw new ArgumentNullException(nameof(argument));
						}

						return argument;
					}
				case ParameterType.PlayerName:
					{
						return context?.Player?.Name ?? "None";
					}
				default:
					throw new ArgumentOutOfRangeException(nameof(Type));
			}
		}
	}
}