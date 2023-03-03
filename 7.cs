using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Oracle.ManagedDataAccess.Client;

namespace YourNamespace
{
    public class YourController : ApiController
    {
        private const int ChunkSize = 10000;

        [HttpGet]
        [Route("api/getdata")]
        public async Task<HttpResponseMessage> GetData()
        {
            // Create a connection to the database
            var connectionString = "your-connection-string-here";
            using (var connection = new OracleConnection(connectionString))
            {
                await connection.OpenAsync();

                // Get the total number of records in the table or view
                var totalRecords = await GetTotalRecords(connection);

                // Stream the compressed data to the client
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new PushStreamContent(async (stream, content, context) =>
                {
                    using (var writer = new StreamWriter(new GZipStream(stream, CompressionLevel.Optimal)))
                    {
                        // Write the column names as the first row
                        var columnNames = await GetColumnNames(connection);
                        await writer.WriteLineAsync(string.Join(",", columnNames));

                        // Retrieve the data from the table or view in chunks
                        for (int i = 0; i < totalRecords; i += ChunkSize)
                        {
                            var dataChunk = await GetDataChunk(connection, i, ChunkSize);
                            foreach (var row in dataChunk)
                            {
                                await writer.WriteLineAsync(string.Join(",", row));
                            }
                            await writer.FlushAsync();
                        }
                    }
                }, new MediaTypeHeaderValue("text/csv"));
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = "data.csv.gz"
                };
                response.Content.Headers.Add("Content-Encoding", "gzip");
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
                return response;
            }
        }

        [NonAction]
        private async Task<int> GetTotalRecords(OracleConnection connection)
        {
            var commandText = "SELECT COUNT(*) FROM YourTableOrView";
            using (var command = new OracleCommand(commandText, connection))
            {
                return Convert.ToInt32(await command.ExecuteScalarAsync());
            }
        }

        [NonAction]
        private async Task<List<string>> GetColumnNames(OracleConnection connection)
        {
            var commandText = "SELECT COLUMN_NAME FROM ALL_TAB_COLUMNS WHERE TABLE_NAME = 'YourTableOrView'";
            using (var command = new OracleCommand(commandText, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                var columnNames = new List<string>();
                while (await reader.ReadAsync())
                {
                    columnNames.Add(reader.GetString(0));
                }
                return columnNames;
            }
        }

       [NonAction]
private async Task<List<string[]>> GetDataChunk(OracleConnection connection, int offset, int limit)
{
    var commandText = $@"
        SELECT * FROM (
            SELECT /*+ FIRST_ROWS({limit}) */ 
                YourTableOrView.*, ROWNUM AS rn 
            FROM YourTableOrView 
            WHERE ROWNUM <= {offset + limit}
        ) 
        WHERE rn > {offset}";

    using (var command = new OracleCommand(commandText, connection))
    {
        var data = new List<string[]>();
        using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess))
        {
            while (await reader.ReadAsync())
            {
                var row = new string[reader.FieldCount];
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    if (await reader.IsDBNullAsync(i))
                    {
                        row[i] = "";
                    }
                    else
                    {
                        row[i] = await reader.GetFieldValueAsync<string>(i);
                    }
                }
                data.Add(row);
            }
        }
        return data;
    }
}
 
