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

        var memoryStream = new MemoryStream();
        await using (var clobStream = oracleClob.GetStream())
        {
            await clobStream.CopyToAsync(memoryStream);
        }

        memoryStream.Position = 0;

        return new FileStreamResult(memoryStream, "text/csv")
        {
            FileDownloadName = "yourfile.csv"
        };
    }
}
