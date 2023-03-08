using System.IO;
using Microsoft.AspNetCore.Mvc;
using Telerik.Documents.SpreadsheetStreaming;
using Oracle.ManagedDataAccess.Client;

[Route("api/[controller]")]
[ApiController]
public class DataController : ControllerBase
{
    [HttpGet]
    [Route("download")]
    public IActionResult DownloadData()
    {
        var connectionString = "your connection string here";
        var query = "SELECT * FROM your_table";

        using var connection = new OracleConnection(connectionString);
        using var command = new OracleCommand(query, connection);
        connection.Open();

        using var reader = command.ExecuteReader();
        var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();

        var buffer = new MemoryStream();
        var writer = new StreamWriter(buffer);
        var options = new CsvStreamingExportOptions { UseCellDataTypeDetection = true };

        // Write column headers
        for (var col = 0; col < columns.Count; col++)
        {
            writer.Write(columns[col]);
            if (col < columns.Count - 1)
            {
                writer.Write(",");
            }
        }
        writer.WriteLine();

        // Write data rows
        while (reader.Read())
        {
            for (var col = 0; col < columns.Count; col++)
            {
                var value = reader.GetValue(col);
                writer.Write(value);
                if (col < columns.Count - 1)
                {
                    writer.Write(",");
                }
            }
            writer.WriteLine();
        }

        writer.Flush();
        buffer.Position = 0;
        var fileName = "data.csv";
        return File(buffer, "text/csv", fileName);
    }
}
