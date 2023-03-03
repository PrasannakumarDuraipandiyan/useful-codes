using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YourNamespace
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataController : ControllerBase
    {
        private readonly IConfiguration _config;

        public DataController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet("download")]
        public async Task<IActionResult> DownloadCsv(string filter = "", int pageSize = 1000)
        {
            Response.Headers.Add("Content-Encoding", "gzip");
            Response.Headers.Add("Content-Disposition", "attachment; filename=\"data.csv.gz\"");

            var streams = new List<Stream>();

            // Download data in parallel and add streams to list
            int offset = 0;
            while (true)
            {
                var task = DownloadCsvChunk(filter, offset, pageSize);
                var chunk = await task;
                if (chunk.Length == 0)
                {
                    break;
                }
                streams.Add(chunk);
                offset += pageSize;
            }

            // Write streams to response using PushContentStream
            return new PushContentStream(async stream =>
            {
                using (stream)
                {
                    var compressedStream = new GZipStream(stream, CompressionMode.Compress, leaveOpen: true);
                    using (compressedStream)
                    {
                        foreach (var s in streams)
                        {
                            await s.CopyToAsync(compressedStream);
                            await compressedStream.FlushAsync();
                        }
                    }
                }
            }, "text/csv");
        }

        private async Task<MemoryStream> DownloadCsvChunk(string filter, int offset, int limit)
        {
            var connectionString = _config.GetConnectionString("DefaultConnection");
            using var connection = new OracleConnection(connectionString);
            await connection.OpenAsync();

            var commandText = $"SELECT * FROM your_view_or_table WHERE ROWNUM >= :offset AND ROWNUM < :offset + :limit {filter}";
            using var command = new OracleCommand(commandText, connection);
            command.Parameters.Add(new OracleParameter("offset", offset + 1));
            command.Parameters.Add(new OracleParameter("limit", limit));

            using var reader = await command.ExecuteReaderAsync();

            var results = new List<List<object>>();
            while (await reader.ReadAsync())
            {
                var row = new List<object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row.Add(reader.IsDBNull(i) ? null : reader.GetValue(i));
                }
                results.Add(row);
            }

            var stream = new MemoryStream();
            var writer = new StreamWriter(stream, Encoding.UTF8);

            // Write rows to CSV
            foreach (var row in results)
            {
                var csvRow = row.Select(o => o is null ? "" : EscapeCsvValue(o.ToString())).ToList();
                await writer.WriteLineAsync(string.Join(",", csvRow));
            }

            await writer.FlushAsync();
            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        private string EscapeCsvValue(string value)
        {
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                value = "\"" + value
                    .Replace("\"", "\"\"")
                    .Replace("\r", "")
                    .Replace("\n", "") + "\"";
            }
            return value;
        }
    }
}
