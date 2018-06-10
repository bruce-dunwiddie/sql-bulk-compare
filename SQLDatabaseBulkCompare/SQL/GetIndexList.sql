SELECT
	s.[name] AS [schema],
	t.[name] AS [table],
	i.[name] AS [index],
	i.is_primary_key,
	CAST(CASE
		WHEN i.[type_desc] = 'CLUSTERED' THEN 1
		ELSE 0
	END AS bit) AS is_clustered,
	i.is_unique,
	CAST(CASE
		WHEN i.is_padded = 1 THEN i.fill_factor
		ELSE 0
	END AS tinyint) AS fill_factor,
	i.is_disabled,
	ISNULL(k.is_system_named, CAST(0 AS bit)) AS is_system_named
FROM
	sys.schemas s
		INNER JOIN sys.tables t ON
			s.[schema_id] = t.[schema_id]					
		INNER JOIN sys.indexes i ON
			i.[object_id] = t.[object_id]
		LEFT OUTER JOIN sys.key_constraints k ON
			k.parent_object_id = i.[object_id] AND
			k.unique_index_id = i.index_id
WHERE
	i.is_hypothetical = 0 AND
	i.[type_desc] <> 'HEAP' AND
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
	i.[name];
