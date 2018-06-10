SELECT 
	s.[name] AS [schema],
	t.[name] AS [table],
	fk.[name] AS [foreign_key],
	rs.[name] AS referenced_schema,
	rt.[name] AS referenced_table,
	fk.is_disabled,
	fk.is_system_named
FROM
	sys.foreign_keys fk
		INNER JOIN sys.tables t ON
			t.[object_id] = fk.parent_object_id
		INNER JOIN sys.schemas s ON
			s.[schema_id] = t.[schema_id]
		INNER JOIN sys.tables rt ON
			rt.[object_id] = fk.referenced_object_id
		INNER JOIN sys.schemas rs ON
			rs.[schema_id] = rt.[schema_id]
ORDER BY
	s.[name],
	t.[name],
	fk.[name];
