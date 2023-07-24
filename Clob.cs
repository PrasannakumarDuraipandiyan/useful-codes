using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Threading.Tasks;

[ApiController]
[Route("api/clob")]
public class ClobController : ControllerBase
{
    private readonly string _connectionString;

    public ClobController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("OracleConnection");
    }

    [HttpGet("stream/{id}")]
    public async Task<IActionResult> StreamClobData(int id)
    {
        using (var connection = new OracleConnection(_connectionString))
        {
            await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = "SELECT clob_data FROM your_table WHERE id = :id";
                command.Parameters.Add(new OracleParameter("id", OracleDbType.Int32)).Value = id;

                using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess))
                {
                    if (await reader.ReadAsync())
                    {
                        if (!(await reader.IsDBNullAsync(0)))
                        {
                            var clob = reader.GetOracleClob(0);

                            var response = File(clob, "text/plain", enableRangeProcessing: true);

                            return response;
                        }
                    }
                }
            }
        }

        return NotFound();
    }
}
