using System;

namespace AlternativeCommandExecution.ShortCommand
{
	public sealed class LackOfArgumentException : Exception
	{
		public LackOfArgumentException(string msg) : base(msg) { }
	}
}
