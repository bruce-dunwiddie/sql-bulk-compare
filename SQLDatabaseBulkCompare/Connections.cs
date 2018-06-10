using System;
using System.Collections.Generic;
using System.Text;

namespace SQLDatabaseBulkCompare
{
    public class Connections
    {
		public string SourceConnectionString { get; set; }

		public string[] TargetConnectionStrings { get; set; }
    }
}
