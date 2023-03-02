[HttpGet]
public async Task<IActionResult> GenerateCsvFile(string tableName, string filter = "")
{
    var connectionString = "Your Oracle DB Connection String"; // replace with your Oracle DB connection string

    using (var connection = new OracleConnection(connectionString))
    {
        await connection.OpenAsync();

        // create a command object with the provided table name and filter
        var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM {tableName} WHERE {filter}";

        // execute the command and create a data reader with streaming support
        using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow))
        {
            var result = new StringBuilder();

            // write column names as headers
            var headers = Enumerable.Range(0, reader.FieldCount)
                                     .Select(reader.GetName)
                                     .ToArray();
            result.AppendLine(string.Join(',', headers));

            // read data rows in batches and write to the output stream in parallel
            var batchSize = 5000;
            var buffer = new List<string>(batchSize);
            var readCount = 0;
            var tasks = new List<Task>();
            while (await reader.ReadAsync())
            {
                var row = new StringBuilder();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row.Append(reader.GetValue(i));
                    if (i < reader.FieldCount - 1)
                    {
                        row.Append(",");
                    }
                }

                buffer.Add(row.ToString());

                readCount++;
                if (readCount % batchSize == 0)
                {
                    var batch = new List<string>(buffer);
                    tasks.Add(Task.Run(async () =>
                    {
                        await WriteBatchToStreamAsync(batch);
                    }));

                    buffer.Clear();
                }
            }

            // write any remaining rows to the output stream
            if (buffer.Count > 0)
            {
                var batch = new List<string>(buffer);
                tasks.Add(Task.Run(async () =>
                {
                    await WriteBatchToStreamAsync(batch);
                }));
            }

            // wait for all tasks to complete
            await Task.WhenAll(tasks);

            // return the CSV file as a stream
            Response.Headers.Add("Content-Disposition", $"attachment; filename={tableName}.csv");
            Response.Headers.Add("X-Content-Type-Options", "nosniff");
            return File(stream, "text/csv");
        }
    }

    async Task WriteBatchToStreamAsync(List<string> data)
    {
        var buffer = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, data));
        await stream.WriteAsync(buffer);
        await stream.FlushAsync();
    }
}
