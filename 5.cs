using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;

namespace MyApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DataController : ControllerBase
    {
        private readonly string _connectionString = "Data Source=MyOracleDB;User Id=myUsername;Password=myPassword;";

        [HttpGet]
        public async Task<IActionResult> DownloadCsv([FromQuery] string filter)
        {
            // Get the total number of records
            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync();
            await using var countCommand = connection.CreateCommand();
            countCommand.CommandText = $"SELECT COUNT(*) FROM myview WHERE {filter}";
            var totalRecords = (int)await countCommand.ExecuteScalarAsync();

            // Set the page size and calculate the total number of pages
            var pageSize = 1000;
            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            // Download the data in parallel
            var tasks = new List<Task<Stream>>();
            for (var i = 0; i < totalPages; i++)
            {
                var offset = i * pageSize;
                tasks.Add(DownloadCsvChunk(filter, offset, pageSize));
            }

            // Wait for all tasks to complete and merge the results
            var streams = await Task.WhenAll(tasks);
            var mergedStream = new MemoryStream();
            foreach (var stream in streams)
            {
                stream.Seek(0, SeekOrigin.Begin);
                await stream.CopyToAsync(mergedStream);
                stream.Dispose();
            }
            mergedStream.Seek(0, SeekOrigin.Begin);

            // Compress the CSV data
            var compressedStream = new MemoryStream();
            await using var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal, true);
            await mergedStream.CopyToAsync(gzipStream);
            await gzipStream.FlushAsync();
            compressedStream.Seek(0, SeekOrigin.Begin);

            // Return the compressed CSV data
            return new FileStreamResult(compressedStream, "application/gzip")
            {
                FileDownloadName = "data.csv.gz"
            };
        }

        private async Task<Stream> DownloadCsvChunk(string filter, int offset, int pageSize)
{
    await using var connection = new OracleConnection(_connectionString);
    await connection.OpenAsync();

    await using var command = connection.CreateCommand();
    command.CommandText = $"SELECT column1, column2, column3 FROM myview WHERE {filter} ORDER BY column1 OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";

    await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

    var stream = new MemoryStream();
    await using var writer = new StreamWriter(stream);

    // Write header row
    for (int i = 0; i < reader.FieldCount; i++)
    {
        writer.Write($"\"{reader.GetName(i)}\"");
        if (i < reader.FieldCount - 1)
        {
            writer.Write(",");
        }
    }
    await writer.WriteLineAsync();

    // Write data rows
    while (await reader.ReadAsync())
    {
        for (int i = 0; i < reader.FieldCount; i++)
        {
            var value = reader.GetValue(i);
            if (value is DBNull)
            {
                writer.Write(",");
            }
            else if (value is string stringValue)
            {
                writer.Write($"\"{stringValue.Replace("\"", "\"\"")}\"");
            }
            else
            {
                writer.Write(value);
            }

            if (i < reader.FieldCount - 1)
            {
                writer.Write(",");
            }
        }
        await writer.WriteLineAsync();
    }

    await writer.FlushAsync();
    stream.Seek(0, SeekOrigin.Begin);
    return stream;
}
