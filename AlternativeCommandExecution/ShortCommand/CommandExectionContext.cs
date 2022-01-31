using TShockAPI;

namespace AlternativeCommandExecution.ShortCommand
{
	public sealed class CommandExectionContext
	{
		public TSPlayer Player { get; }

		public CommandExectionContext(TSPlayer player)
		{
			Player = player;
		}
	}
}
