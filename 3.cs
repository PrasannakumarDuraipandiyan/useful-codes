using CsvHelper;
using Oracle.ManagedDataAccess.Client;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            // create a CSV writer and write column names as headers
            using (var writer = new CsvWriter(new StreamWriter(Response.Body), CultureInfo.InvariantCulture))
            {
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    writer.WriteField(reader.GetName(i));
                }

                await writer.NextRecordAsync();

                // read data rows in batches of 1000 and write to the CSV file in parallel
                var batchSize = 1000;
                var buffer = new object[batchSize];
                var readCount = 0;
                var tasks = new List<Task>();
                while (await reader.ReadAsync())
                {
                    reader.GetValues(buffer);
                    var record = Enumerable.Range(0, reader.FieldCount)
                                            .Select(i => buffer[i])
                                            .ToArray();

                    tasks.Add(Task.Run(async () =>
                    {
                        await writer.WriteRecordAsync(record);
                        await writer.NextRecordAsync();
                    }));

                    readCount++;
                    if (readCount % batchSize == 0)
                    {
                        await Task.WhenAll(tasks);
                        tasks.Clear();
                    }
                }

                // wait for any remaining tasks to complete
                if (tasks.Count > 0)
                {
                    await Task.WhenAll(tasks);
                }

                // flush the CSV writer to ensure that all records are written to the output stream
                await writer.FlushAsync();
            }

            // return the CSV file as a stream
            Response.ContentType = "text/csv";
            Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{tableName}.csv\"");
            Response.Body.Position = 0;
            await Response.Body.CopyToAsync(HttpContext.Response.Body);
            return new EmptyResult();
        }
    }
}
