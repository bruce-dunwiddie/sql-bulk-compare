SELECT
	s.[name] AS [schema],
	o.[name] AS [object_name], 
	po.[name] AS [parent],
	RTRIM(LTRIM(o.[type])) AS [type], 
	RTRIM(LTRIM(OBJECT_DEFINITION(o.[object_id]))) AS [definition],
	COALESCE(c.is_system_named, d.is_system_named, CAST(0 AS bit)) AS is_system_named
FROM            
	sys.objects AS o
		INNER JOIN sys.schemas AS s ON
			s.[schema_id] = o.[schema_id]
		LEFT OUTER JOIN sys.objects po ON
			po.[object_id] = o.parent_object_id
		LEFT OUTER JOIN sys.check_constraints c ON
			c.[object_id] = o.[object_id]
		LEFT OUTER JOIN sys.default_constraints d ON
			d.[object_id] = o.[object_id]
WHERE
	-- excluding unique constraints because they will also show up in indexes and make more sense there
	-- excluding fk's because they have their own section
	RTRIM(LTRIM(o.[type])) NOT IN ('UQ', 'PK', 'F') AND
	o.is_ms_shipped = 0 AND 
	NOT EXISTS
	(
		SELECT *
		FROM
			sys.extended_properties AS ep
		WHERE        
			major_id = o.[object_id] AND 
			minor_id = 0 AND 
			class = 1 AND 
			[name] = 'microsoft_database_tools_support'
	)
ORDER BY
	o.[type], 
	s.[name],
	po.[name],
	o.[name];
