using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public class FileController : Controller
{
    private readonly string _connectionString = "Your Oracle Connection String Here";

    [HttpGet("download-clob")]
    public async Task<IActionResult> DownloadClob()
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new OracleCommand("GetClobData", connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };

        command.Parameters.Add("p_clob", OracleDbType.Clob).Direction = System.Data.ParameterDirection.Output;

        await command.ExecuteNonQueryAsync();

        var oracleClob = command.Parameters["p_clob"].Value as OracleClob;

        if (oracleClob == null)
        {
            return NotFound();
        }

        // Create a memory stream for the response
        var memoryStream = new MemoryStream();
        await using (var writer = new StreamWriter(memoryStream, Encoding.UTF8, leaveOpen: true))
        {
            char[] buffer = new char[8192];
            long position = 0;
            long length = oracleClob.Length;
            while (position < length)
            {
                int charsRead = oracleClob.Read(buffer, 0, buffer.Length, position);
                if (charsRead > 0)
                {
                    await writer.WriteAsync(buffer, 0, charsRead);
                    position += charsRead;
                }
                else
                {
                    break;
                }
            }
        }

        memoryStream.Position = 0; // Reset the position for reading by FileStreamResult

        return new FileStreamResult(memoryStream, "text/csv")
        {
            FileDownloadName = "yourfile.csv"
        };
    }
}
