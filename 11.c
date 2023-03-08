using System.IO;
using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;

[Route("api/[controller]")]
[ApiController]
public class OracleController : ControllerBase
{
    [HttpGet("{tableName}")]
    public IActionResult ExportToCSV(string tableName)
    {
        var connectionString = "Data Source=<your data source>;User ID=<your user id>;Password=<your password>";

        var progress = new Progress<int>(percent => Console.WriteLine($"Download progress: {percent}%"));

        using (var connection = new OracleConnection(connectionString))
        {
            connection.Open();

            // Get the total row count
            var rowCountCommand = new OracleCommand($"SELECT COUNT(*) FROM {tableName}", connection);
            var totalRowCount = Convert.ToInt32(rowCountCommand.ExecuteScalar());

            // Set up the data reader
            var selectCommand = new OracleCommand($"SELECT * FROM {tableName}", connection);
            var reader = selectCommand.ExecuteReader();

            // Set up the CSV writer
            var csvStream = new MemoryStream();
            var csvWriter = new StreamWriter(csvStream);

            // Write the header row
            for (int i = 0; i < reader.FieldCount; i++)
            {
                csvWriter.Write(reader.GetName(i));
                if (i < reader.FieldCount - 1)
                {
                    csvWriter.Write(",");
                }
            }
            csvWriter.WriteLine();

            // Write the data rows
            var rowCount = 0;
            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    csvWriter.Write(reader.GetValue(i));
                    if (i < reader.FieldCount - 1)
                    {
                        csvWriter.Write(",");
                    }
                }
                csvWriter.WriteLine();

                rowCount++;
                if (rowCount % 1000 == 0)
                {
                    // Report the progress every 1000 rows
                    progress.Report(rowCount * 100 / totalRowCount);
                }
            }

            // Flush the CSV writer
            csvWriter.Flush();

            // Compress the CSV stream
            var compressedStream = new MemoryStream();
            using (var gzip = new GZipStream(compressedStream, CompressionMode.Compress, true))
            {
                csvStream.Position = 0;
                csvStream.CopyTo(gzip);
            }

            // Return the compressed stream as a file download
            compressedStream.Position = 0;
            return File(compressedStream, "application/gzip", $"{tableName}.csv.gz");
        }
    }
}
