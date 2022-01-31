using System;

namespace AlternativeCommandExecution.ShortCommand
{
	public sealed class CommandParseException : Exception
	{
		public CommandParseException(string message, string line, int index) : base(message) { }

		public CommandParseException(string message) : base(message) { }
	}
}
