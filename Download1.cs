[HttpGet("download-csv/{viewName}")]
public IActionResult DownloadOracleViewAsCSV(string viewName)
{
    var connectionString = "your connection string here";
    var fileName = $"{viewName}.csv";

    // Set up a connection to the Oracle database
    using (var connection = new OracleConnection(connectionString))
    {
        connection.Open();

        // Create a command object to execute the view query
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT * FROM " + viewName;

            // Execute the query and read the results using a data reader with batching
            using (var reader = command.ExecuteReader())
            {
                // Write the data to a stream using a StreamWriter
                var stream = new MemoryStream();
                using (var writer = new StreamWriter(stream))
                {
                    var writeHeader = true;

                    while (reader.Read())
                    {
                        // Add the header row on the first iteration only
                        if (writeHeader)
                        {
                            var header = string.Join(",", Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetName(i)));
                            writer.WriteLine(header);
                            writeHeader = false;
                        }

                        // Add the data rows
                        var values = new string[reader.FieldCount];
                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            values[i] = reader.IsDBNull(i) ? "" : reader.GetValue(i).ToString();
                        }
                        var rowData = string.Join(",", values);
                        writer.WriteLine(rowData);

                        // Flush the stream every 1000 rows to avoid blocking
                        if (reader.RowNumber % 1000 == 0)
                        {
                            writer.Flush();
                            stream.Flush();
                        }
                    }

                    // Flush the stream one last time to ensure that all data is written to memory
                    writer.Flush();
                    stream.Flush();
                }

                // Reset the stream position to the beginning
                stream.Position = 0;

                // Return the CSV file as a response
                return File(stream, "text/csv", fileName);
            }
        }
    }
}
