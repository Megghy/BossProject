using TShockAPI.DB;

namespace Scattering
{
	internal class PlayerHome
	{
		public string Name { get; set; }

		public int UserId { get; set; }

		public int X { get; set; }

		public int Y { get; set; }

		public static PlayerHome FromReader(QueryResult reader)
		{
			return new PlayerHome
			{
				Name = reader.Get<string>("Name"),
				UserId = reader.Get<int>("UserId"),
				X = reader.Get<int>("X"),
				Y = reader.Get<int>("Y")
			};
		}
	}
}
