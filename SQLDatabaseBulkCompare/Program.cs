using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

using Microsoft.Extensions.Configuration;

using Dapper;

namespace SQLDatabaseBulkCompare
{
	class Program
	{
		static void Main(string[] args)
		{
			var config = new ConfigurationBuilder()
				.AddJsonFile("appsettings.json", optional: false)
				.Build();

			var connections = new Connections();
			config.GetSection("Connections").Bind(connections);

			var objectQuery = File.ReadAllText("./SQL/GetObjectList.sql");

			var tableColumnsQuery = File.ReadAllText("./SQL/GetTableColumns.sql");

			var indexQuery = File.ReadAllText("./SQL/GetIndexList.sql");

			var indexColumnsQuery = File.ReadAllText("./SQL/GetIndexColumns.sql");

			if (File.Exists("./differences.csv"))
			{
				File.Delete("./differences.csv");
			}

			List<dynamic> sourceObjects = null;
			List<dynamic> sourceTableColumns = null;
			List<dynamic> sourceIndexes = null;
			List<dynamic> sourceIndexColumns = null;
			Dictionary<dynamic, dynamic> sourceObjectLookup = null;
			ILookup<dynamic, dynamic> sourceTableColumnsLookup = null;
			Dictionary<dynamic, dynamic> sourceTableColumnLookup = null;
			Dictionary<dynamic, dynamic> sourceIndexLookup = null;
			ILookup<dynamic, dynamic> sourceIndexColumnsLookup = null;

			Dictionary<string, string> sourceConnectionValues = new ConnectionStringParser()
				.GetKeyValuePairs(
					connections.SourceConnectionString);

			using (var source = new SqlConnection(connections.SourceConnectionString))
			{
				Console.WriteLine(
					"Getting source schema from " + 
					sourceConnectionValues["SERVER"] + "/" + 
					sourceConnectionValues["DATABASE"]);

				// lookups

				sourceObjects = source.Query<dynamic>(objectQuery).ToList();

				sourceObjectLookup = sourceObjects.ToDictionary(
					o => Tuple.Create<string, string, string>(
						o.schema,
						o.object_name,
						o.parent));

				sourceTableColumns = source.Query<dynamic>(tableColumnsQuery).ToList();

				sourceTableColumnsLookup = sourceTableColumns.ToLookup(
					tc => Tuple.Create<string, string>(
						tc.schema,
						tc.table));

				sourceTableColumnLookup = sourceTableColumns.ToDictionary(
					tc => Tuple.Create<string, string, string>(
						tc.schema,
						tc.table,
						tc.column));

				sourceIndexes = source.Query<dynamic>(indexQuery).ToList();

				sourceIndexLookup = sourceIndexes.ToDictionary(
					i => Tuple.Create<string, string, string>(
						i.schema,
						i.table,
						i.index));

				sourceIndexColumns = source.Query<dynamic>(indexColumnsQuery).ToList();

				sourceIndexColumnsLookup = sourceIndexColumns.ToLookup(
					ic => Tuple.Create<string, string, string>(
						ic.schema,
						ic.table,
						ic.index));
			}

			foreach (var targetConnection in connections.TargetConnectionStrings)
			{
				Dictionary<string, string> targetConnectionValues = new ConnectionStringParser()
					.GetKeyValuePairs(
						targetConnection);

				string targetServer = targetConnectionValues["SERVER"];
				string targetDatabase = targetConnectionValues["DATABASE"];

				using (var target = new SqlConnection(targetConnection))
				{
					Console.WriteLine(
						"Getting target schema from " + 
						targetConnectionValues["SERVER"] + "/" + 
						targetConnectionValues["DATABASE"]);

					// lookups

					var targetObjects = target.Query<dynamic>(objectQuery).ToList();

					var targetObjectLookup = targetObjects.ToDictionary(
						o => Tuple.Create<string, string, string>(
							o.schema,
							o.object_name,
							o.parent));

					var targetTableColumns = target.Query<dynamic>(tableColumnsQuery).ToList();

					var targetTableColumnsLookup = targetTableColumns.ToLookup(
						tc => Tuple.Create<string, string>(
							tc.schema,
							tc.table));

					var targetTableColumnLookup = targetTableColumns.ToDictionary(
						tc => Tuple.Create<string, string, string>(
							tc.schema,
							tc.table,
							tc.column));

					var targetIndexes = target.Query<dynamic>(indexQuery).ToList();

					var targetIndexLookup = targetIndexes.ToDictionary(
						i => Tuple.Create<string, string, string>(
							i.schema,
							i.table,
							i.index));

					var targetIndexColumns = target.Query<dynamic>(indexColumnsQuery).ToList();

					var targetIndexColumnsLookup = targetIndexColumns.ToLookup(
						ic => Tuple.Create<string, string, string>(
							ic.schema,
							ic.table,
							ic.index));

					var targetIndexColumnLookup = targetIndexColumns.ToDictionary(
						ic => Tuple.Create<string, string, string, string>(
							ic.schema,
							ic.table,
							ic.index,
							ic.column));

					// results

					// TODO: handle system named default and check constraints

					var missingObjects = sourceObjects
						.Where(o => 
							!o.is_system_named &&
							!targetObjectLookup.ContainsKey(
								Tuple.Create<string, string, string>(
									o.schema,
									o.object_name,
									o.parent))).ToList();

					var extraObjects = targetObjects
						.Where(o =>
							!o.is_system_named &&
							!sourceObjectLookup.ContainsKey(
								Tuple.Create<string, string, string>(
									o.schema,
									o.object_name,
									o.parent))).ToList();

					var modifiedObjects = sourceObjects
						.Select(o => new
						{
							Key = Tuple.Create<string, string, string>(
											o.schema,
											o.object_name,
											o.parent),
							SourceObject = o
						})
						.Where(o => targetObjectLookup.ContainsKey(o.Key))
						.Select(o => new
						{
							SourceObject = o.SourceObject,
							TargetObject = targetObjectLookup[o.Key]
						})
						.Where(o =>
							(
								o.SourceObject.type != o.TargetObject.type ||
								o.SourceObject.definition != o.TargetObject.definition
							)
						)
						.Select(o => o.SourceObject)
						.ToList();

					var missingColumns = sourceTableColumns
						.Where(tc => targetObjectLookup.ContainsKey(
							Tuple.Create<string, string, string>(
								tc.schema,
								tc.table,
								null)))
						.Where(tc => !targetTableColumnLookup.ContainsKey(
							Tuple.Create<string, string, string>(
								tc.schema,
								tc.table,
								tc.column)))
								.ToList();

					var extraColumns = targetTableColumns
						.Where(tc => sourceObjectLookup.ContainsKey(
							Tuple.Create<string, string, string>(
								tc.schema,
								tc.table,
								null)))
						.Where(tc => !sourceTableColumnLookup.ContainsKey(
							Tuple.Create<string, string, string>(
								tc.schema,
								tc.table,
								tc.column))).ToList();

					var modifiedColumns = sourceTableColumns
						.Select(tc => new
						{
							Key = Tuple.Create<string, string, string>(
									tc.schema,
									tc.table,
									tc.column),
							Column = tc
						})
						.Where(tc => targetTableColumnLookup.ContainsKey(tc.Key))
						.Select(tc => new
						{
							Source = tc.Column,
							Target = targetTableColumnLookup[tc.Key]
						})
						.Where(tc =>
							tc.Source.data_type != tc.Target.data_type ||
							tc.Source.is_nullable != tc.Target.is_nullable ||
							tc.Source.is_identity != tc.Target.is_identity ||
							tc.Source.column_id != tc.Target.column_id
						)
						.Select(tc => tc.Source)
						.ToList();

					// TODO: handle system named unique and primary key constraints/indexes

					var missingIndexes = sourceIndexes
						.Where(i =>
							!i.is_system_named &&
							!targetIndexLookup.ContainsKey(
								Tuple.Create<string, string, string>(
									i.schema,
									i.table,
									i.index))).ToList();

					var extraIndexes = targetIndexes
						.Where(i =>
							!i.is_system_named &&
							!sourceIndexLookup.ContainsKey(
								Tuple.Create<string, string, string>(
									i.schema,
									i.table,
									i.index))).ToList();

					var modifiedIndexes = sourceIndexes
						.Select(i => new
						{
							// generate lookup key for column list based on [schema].[table].[index]
							Key = Tuple.Create<string, string, string>(
									i.schema,
									i.table,
									i.index),
							Index = i
						})
						// make sure the index exists on both sides before doing comparisons
						.Where(i => targetIndexLookup.ContainsKey(i.Key))
						.Select(i => new
						{
							Key = i.Key,
							Source = i.Index,
							SourceIndexColumns = ((IEnumerable<dynamic>)sourceIndexColumnsLookup[i.Key]).ToList(),
							Target = targetIndexLookup[i.Key],
							// target column list is only used for comparing counts
							TargetIndexColumns = ((IEnumerable<dynamic>)targetIndexColumnsLookup[i.Key]).ToList()
						})
						.Where(i =>
							// are the index options different?
							(
								i.Source.is_primary_key != i.Target.is_primary_key ||
								i.Source.is_clustered != i.Target.is_clustered ||
								i.Source.is_unique != i.Target.is_unique ||
								i.Source.fill_factor != i.Target.fill_factor ||
								i.Source.is_disabled != i.Target.is_disabled
							) ||
							// is the number of columns in the index different?
							i.SourceIndexColumns.Count != i.TargetIndexColumns.Count ||
							// are any of the column options different?
							i.SourceIndexColumns
								.Select(ic => new
								{
									Key = Tuple.Create<string, string, string, string>(
												ic.schema,
												ic.table,
												ic.index,
												ic.column),
									IndexColumn = ic
								})
								.Select(ic => new
								{
									SourceColumn = ic.IndexColumn,
									TargetColumn = targetIndexColumnLookup[ic.Key]
								})
								.Where(ic => (
										ic.TargetColumn != null &&
										ic.SourceColumn.key_ordinal == ic.TargetColumn.key_ordinal &&
										ic.SourceColumn.index_column_id == ic.TargetColumn.index_column_id &&
										ic.SourceColumn.is_descending_key == ic.TargetColumn.is_descending_key &&
										ic.SourceColumn.is_included_column == ic.TargetColumn.is_included_column
									))
								.Count() != i.TargetIndexColumns.Count
						)
						.Select(i => i.Source)
						.ToList();

					missingObjects.ForEach(o =>	{
						LogDifference(
							targetServer,
							targetDatabase,
							o.schema,
							o.object_name,
							o.parent,
							o.type,
							Difference.Missing);
					});
					extraObjects.ForEach(o => {
						LogDifference(
							targetServer,
							targetDatabase,
							o.schema,
							o.object_name,
							o.parent,
							o.type,
							Difference.Extra);
					});
					modifiedObjects.ForEach(o => {
						LogDifference(
							targetServer,
							targetDatabase,
							o.schema,
							o.object_name,
							o.parent,
							o.type,
							Difference.Modified);
					});

					missingColumns.ForEach(c => {
						LogDifference(
							targetServer,
							targetDatabase,
							c.schema,
							c.column,
							c.table,
							"Column",
							Difference.Missing);
					});
					extraColumns.ForEach(c => {
						LogDifference(
							targetServer,
							targetDatabase,
							c.schema,
							c.column,
							c.table,
							"Column",
							Difference.Extra);
					});
					modifiedColumns.ForEach(c => {
						LogDifference(
							targetServer,
							targetDatabase,
							c.schema,
							c.column,
							c.table,
							"Column",
							Difference.Modified);
					});

					missingIndexes.ForEach(i => {
						LogDifference(
							targetServer,
							targetDatabase,
							i.schema,
							i.index,
							i.table,
							"Index",
							Difference.Missing);
					});
					extraIndexes.ForEach(i => {
						LogDifference(
							targetServer,
							targetDatabase,
							i.schema,
							i.index,
							i.table,
							"Index",
							Difference.Extra);
					});
					modifiedIndexes.ForEach(i => {
						LogDifference(
							targetServer,
							targetDatabase,
							i.schema,
							i.index,
							i.table,
							"Index",
							Difference.Modified);
					});
				}
			}
		}

		public static void LogDifference(
			string server,
			string database,
			string schema, 
			string objectName, 
			string parent, 
			string objectType,
			Difference diff)
		{
			if (objectTypeFriendlyNameMappings.ContainsKey(objectType))
			{
				objectType = objectTypeFriendlyNameMappings[objectType];
			}

			if (!File.Exists("./differences.csv"))
			{
				using (var differences = File.CreateText("./differences.csv"))
				{
					differences.WriteLine("server,database,type,schema,parent,name,difference");
				}
			}

			using (var differences = File.AppendText("./differences.csv"))
			{
				differences.WriteLine(
					"{0},{1},{2},{3},{4},{5},{6}",
					server,
					database,
					objectType,
					schema,
					parent,
					objectName,
					diff.ToString());
			}
		}

		public enum Difference
		{
			Missing,
			Extra,
			Modified
		}

		private static Dictionary<string, string> objectTypeFriendlyNameMappings = new Dictionary<string, string>()
			{
				{"AF", "Function"},
				{"C", "Check Constraint"},
				{"D", "Default Constraint"},
				{"F", "Foreign Key"},
				{"FN", "Function"},
				{"FS", "Function"},
				{"FT", "Function"},
				{"IF", "Function"},
				{"IT", "Table"},
				{"P", "Procedure"},
				{"PC", "Procedure"},
				{"PG", "Plan Guide"},
				{"PK", "Primary Key"},
				{"R", "Rule"},
				{"RF", "Replication Filter"},
				{"S", "Table"},
				{"SN", "Synonym"},
				{"SO", "Sequence"},
				{"SQ", "Service Queue"},
				{"TA", "Trigger"},
				{"TF", "Function"},
				{"TR", "Trigger"},
				{"TT", "Table Type"},
				{"U", "Table"},
				{"UQ", "Unique Constraint"},
				{"V", "View"},
				{"X", "Procedure"},
				{"SP", "Security Policy"}
			};
	}
}
