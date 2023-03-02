[HttpGet("{tableName}")]
public async Task<IActionResult> Get(string tableName, [FromQuery] string filters)
{
    var stopwatch = Stopwatch.StartNew();

    using var connection = new OracleConnection(_connectionString);
    await connection.OpenAsync();

    var whereClause = string.IsNullOrEmpty(filters) ? "" : $" WHERE {filters}";
    var rowCountSql = $"SELECT COUNT(*) FROM {tableName}{whereClause}";
    var dataSql = $"SELECT * FROM {tableName}{whereClause}";

    using var rowCountCommand = new OracleCommand(rowCountSql, connection);
    var totalCount = (int)await rowCountCommand.ExecuteScalarAsync();

    var workbook = new Workbook();
    var worksheet = workbook.Worksheets.Add("Data");
    var headerRow = worksheet.Rows[0];

    // Add headers to worksheet
    using var headerCommand = new OracleCommand($"{dataSql} WHERE 1=0", connection);
    using var headerReader = await headerCommand.ExecuteReaderAsync();
    for (int i = 0; i < headerReader.FieldCount; i++)
    {
        headerRow.Cells[i].SetValue(headerReader.GetName(i));
    }

    var pageSize = 1000;
    var batchCount = (int)Math.Ceiling((double)totalCount / pageSize);

    using var stream = new MemoryStream();

    // Fetch data in batches
    for (int i = 0; i < batchCount; i++)
    {
        var offset = i * pageSize;
        var batchSql = new StringBuilder(dataSql)
            .Append($" OFFSET {offset} ROWS")
            .Append($" FETCH NEXT {pageSize} ROWS ONLY")
            .ToString();

        using var command = new OracleCommand(batchSql, connection)
        {
            FetchSize = pageSize
        };

        using var reader = await command.ExecuteReaderAsync();

        var rowBatches = reader.AsEnumerable()
            .AsParallel()
            .WithDegreeOfParallelism(Environment.ProcessorCount)
            .Batch(pageSize)
            .Select(batch => batch.ToArray());

        // Add data to worksheet in parallel
        await Task.WhenAll(rowBatches.Select(batch => AddRowsAsync(batch, worksheet)));

        worksheet.Commit();
    }

    workbook.Save(stream, new XlsxFormatProvider(), new SaveOptions { DisableBuffering = true });

    stream.Position = 0;
    await stream.CopyToAsync(Response.Body);

    Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    Response.Headers.Add("Content-Disposition", $"attachment; filename={tableName}.xlsx");

    stopwatch.Stop();
    var elapsed = stopwatch.ElapsedMilliseconds;

    return new EmptyResult();
}

private static async Task AddRowsAsync(IEnumerable<object[]> rows, Worksheet worksheet)
{
    foreach (var row in rows)
    {
        var newRow = worksheet.Rows.Add();
        for (int i = 0; i < row.Length; i++)
        {
            newRow.Cells[i].SetValue(row[i]);
        }
    }
}
