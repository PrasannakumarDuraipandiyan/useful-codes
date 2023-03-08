[HttpGet]
public async Task<IActionResult> DownloadLargeCsvFile()
{
    // Connect to the Oracle database
    using (var connection = new OracleConnection("<connection string>"))
    {
        // Open the database connection
        await connection.OpenAsync();

        // Create a SQL command to select the data from the table
        var command = new OracleCommand("SELECT * FROM <table name>", connection);

        // Use a data reader to read the data from the table
        using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.CloseConnection))
        {
            // Set the response headers for the file download
            Response.Headers.Add("Content-Disposition", "attachment; filename=<file name>.csv");
            Response.ContentType = "text/csv";
            Response.Headers.Add("Content-Encoding", "gzip");

            // Use a GZipStream to compress the file before sending it
            using (var compressionStream = new GZipStream(Response.Body, CompressionLevel.Optimal))
            {
                // Use a StreamWriter and a buffer to write the data to the response stream in chunks
                using (var writer = new StreamWriter(compressionStream, Encoding.UTF8, 4096))
                {
                    // Write the CSV header row with column names
                    var headerRow = new List<string>();
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        headerRow.Add(reader.GetName(i));
                    }
                    await writer.WriteLineAsync(string.Join(",", headerRow));

                    // Initialize the download manager
                    var downloadManager = new DownloadManager(Response.Headers, reader, writer);
                    await downloadManager.StartDownload();
                }
            }
        }
    }

    // Return the response
    return new EmptyResult();
}

public class DownloadManager
{
    private readonly HttpResponseHeaders _headers;
    private readonly OracleDataReader _reader;
    private readonly StreamWriter _writer;

    public DownloadManager(HttpResponseHeaders headers, OracleDataReader reader, StreamWriter writer)
    {
        _headers = headers;
        _reader = reader;
        _writer = writer;
    }

    public async Task StartDownload()
    {
        // Get the total number of rows in the table
        var totalRows = (int)_reader.GetOracleValue(0);

        // Set the response header for the total file size
        _headers.Add("Content-Length", totalRows.ToString());

        // Set the initial download progress to 0%
        _headers.Add("X-Download-Progress", "0");

        // Use a buffer to read the data from the database and write it to the response stream in chunks
        var buffer = new byte[4096];
        var bytesRead = 0;
        var totalBytesRead = 0;
        while ((bytesRead = (int)_reader.GetOracleValue(1, totalBytesRead, buffer, 0, buffer.Length)) > 0)
        {
            await _writer.BaseStream.WriteAsync(buffer, 0, bytesRead);

            totalBytesRead += bytesRead;

            // Update the download progress header
            var progress = (double)totalBytesRead / (double)totalRows;
            _headers.Remove("X-Download-Progress");
            _headers.Add("X-Download-Progress", progress.ToString("P"));

            // Flush the buffer to the response stream
            await _writer.FlushAsync();
        }
    }
}
