SELECT
	s.[name] AS [schema],
	t.[name] AS [table],
	c.[name] AS [column],
	CASE
		WHEN uty.[name] IS NOT NULL THEN uty.[name]
		ELSE ''
	END +
		CASE
			WHEN uty.[name] IS NOT NULL AND sty.[name] IS NOT NULL THEN '('
			ELSE ''
		END +
		CASE
			WHEN sty.[name] IS NOT NULL THEN sty.[name]
			ELSE ''
		END +
		CASE
			WHEN sty.[name] IN ('char', 'nchar', 'varchar', 'nvarchar', 'binary', 'varbinary')
				THEN '(' + 
					CASE
						WHEN c.max_length = -1 THEN 'max'
						ELSE 
							CASE
								WHEN sty.[name] IN ('nchar', 'nvarchar')
									THEN CAST(c.max_length / 2 AS VARCHAR(MAX))
								ELSE
									CAST(c.max_length AS VARCHAR(MAX))
							END
					END
						+ ')'
			WHEN sty.[name] IN ('numeric', 'decimal')
				THEN '(' + 
					CAST(c.precision AS VARCHAR(MAX)) + ', ' + CAST(c.scale AS VARCHAR(MAX))
						+ ')'
			ELSE
				''
		END +
		CASE
			WHEN uty.[name] IS NOT NULL AND sty.[name] IS NOT NULL THEN ')'
			ELSE ''
		END	AS [data_type],
	c.is_nullable,
	c.is_identity,
	c.column_id
FROM
	sys.columns c
		INNER JOIN sys.tables t ON
			t.[object_id] = c.[object_id]

		INNER JOIN sys.schemas s ON
			s.[schema_id] = t.[schema_id]

		-- get name of user data type
		LEFT OUTER JOIN sys.types uty ON
			uty.system_type_id = c.system_type_id AND
			uty.user_type_id = c.user_type_id AND
			c.user_type_id <> c.system_type_id

		-- get name of system data type
		LEFT OUTER JOIN sys.types sty ON
			sty.system_type_id = c.system_type_id AND
			sty.user_type_id = c.system_type_id

WHERE
	t.is_ms_shipped = 0 AND
	NOT EXISTS
	(
		SELECT *
		FROM
			sys.extended_properties ms
		WHERE
			ms.major_id = t.[object_id] AND
			ms.minor_id = 0 AND
			ms.class = 1 AND
			ms.[name] = 'microsoft_database_tools_support'
	)
ORDER BY
	s.[name],
	t.[name],
	c.column_id;
