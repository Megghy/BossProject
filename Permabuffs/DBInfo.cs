using System;
using System.Collections.Generic;

namespace Permabuffs
{
	public class DBInfo
	{
		public List<int> bufflist;

		public DBInfo(string activeBuffs)
		{
			bufflist = new List<int>();

			string[] buffstring = null;

			if (activeBuffs != "")
			{
				buffstring = activeBuffs.Split(',');
				foreach (string buff in buffstring)
				{
					if (Int32.TryParse(buff, out int tempbuff))
						bufflist.Add(tempbuff);
				}
			}
		}
	}
}
