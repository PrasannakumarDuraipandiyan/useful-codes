[HttpGet("download-csv/{viewName}")]
public IActionResult DownloadOracleViewAsCSV(string viewName)
{
    var connectionString = "your connection string here";
    var fileName = $"{viewName}.csv";

    using (var connection = new OracleConnection(connectionString))
    {
        connection.Open();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT * FROM " + viewName;

            using (var reader = command.ExecuteReader())
            {
                var sb = new StringBuilder();

                // Write the header row
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    sb.Append(reader.GetName(i));
                    if (i < reader.FieldCount - 1) sb.Append(",");
                }
                sb.AppendLine();

                var rowCount = 0;
                var batchSize = 10000;

                while (reader.Read())
                {
                    // Write the data row
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        var value = reader.IsDBNull(i) ? "" : reader.GetValue(i).ToString();
                        sb.Append(value);
                        if (i < reader.FieldCount - 1) sb.Append(",");
                    }
                    sb.AppendLine();

                    rowCount++;

                    // Flush the stream every batchSize rows to avoid blocking
                    if (rowCount % batchSize == 0)
                    {
                        Response.Body.Write(Encoding.UTF8.GetBytes(sb.ToString()));
                        Response.Body.Flush();
                        sb.Clear();
                    }
                }

                // Write any remaining data to the stream
                if (sb.Length > 0)
                {
                    Response.Body.Write(Encoding.UTF8.GetBytes(sb.ToString()));
                    Response.Body.Flush();
                }
            }
        }
    }

    // Reset the response content type and headers
    Response.ContentType = "text/csv";
    Response.Headers.Add("Content-Disposition", $"attachment; filename={fileName}");

    return new EmptyResult();
}
