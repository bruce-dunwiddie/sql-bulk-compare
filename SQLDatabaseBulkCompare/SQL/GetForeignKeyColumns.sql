SELECT 
	s.[name] AS [schema],
	t.[name] AS [table],
	fk.[name] AS [foreign_key],
	rs.[name] AS referenced_schema,
	rt.[name] AS referenced_table,
	fk.is_disabled,
	fk.is_system_named,
	c.[name] AS [column],
	rc.[name] AS referenced_column
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
		INNER JOIN sys.foreign_key_columns fkc ON
			fkc.constraint_object_id = fk.[object_id]
		INNER JOIN sys.columns c ON
			c.[object_id] = t.[object_id] AND
			c.column_id = fkc.parent_column_id
		INNER JOIN sys.columns rc ON
			rc.[object_id] = fk.referenced_object_id AND
			rc.column_id =fkc.referenced_column_id
ORDER BY
	s.[name],
	t.[name],
	fk.[name],
	c.[name];
