using System;
using System.Collections.Generic;
using System.Text;

namespace SQLDatabaseBulkCompare
{
    public class ConnectionStringParser
    {
		public Dictionary<string, string> GetKeyValuePairs(string connectionString)
		{
			Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();

			connectionString = connectionString.Trim();
			string[] parts = connectionString.Split(';');
			foreach (string part in parts)
			{
				if (part != "")
				{
					string[] pieces = part.Split('=');

					string name = pieces[0].Trim().ToUpper();
					string value = pieces[1].Trim();

					keyValuePairs[name] = value;
				}
			}

			return keyValuePairs;
		}
    }
}
