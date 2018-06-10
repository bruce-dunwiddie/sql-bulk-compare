SELECT 
	s.[name] AS [schema],
	t.[name] AS [table],
	i.[name] AS [index],
	c.[name] AS [column],
	ic.is_descending_key,
	ic.is_included_column,
	ic.key_ordinal,
	ic.index_column_id
FROM 
	sys.schemas s
		INNER JOIN sys.tables t ON
			s.[schema_id] = t.[schema_id]					
		INNER JOIN sys.indexes i ON
			i.[object_id] = t.[object_id]
		INNER JOIN sys.index_columns ic ON
			ic.index_id = i.index_id AND
			ic.[object_id] = t.[object_id]
		INNER JOIN sys.columns c ON
			c.column_id = ic.column_id AND
			c.[object_id] = t.[object_id]
WHERE
	i.is_hypothetical = 0 AND
	t.is_ms_shipped = 0 AND
	NOT EXISTS
	(
		SELECT *
		FROM
			sys.extended_properties ep
		WHERE
			ep.major_id = t.[object_id] AND
			ep.minor_id = 0 AND
			ep.class = 1 AND
			ep.[name] = 'microsoft_database_tools_support'
	)
ORDER BY
	s.[name],
	t.[name],
	i.[name],
	CASE
       WHEN ic.is_included_column = 1 THEN 2
       ELSE 1
	END,
	ic.key_ordinal,
	ic.index_column_id;
