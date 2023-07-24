using System;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using System.IO;

[ApiController]
[Route("api/[controller]")]
public class OracleClobController : ControllerBase
{
    private readonly string connectionString = "YOUR_ORACLE_CONNECTION_STRING";
    private readonly string selectQuery = "SELECT your_clob_column FROM your_table WHERE your_condition";

    [HttpGet]
    public IActionResult GetClobAsStream()
    {
        try
        {
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                connection.Open();
                using (OracleCommand command = new OracleCommand(selectQuery, connection))
                {
                    using (OracleDataReader reader = command.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        if (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                // Assuming the CLOB column is the first column (index 0)
                                OracleClob clobData = reader.GetOracleClob(0);

                                // Set the HTTP response headers
                                Response.ContentType = "application/octet-stream";
                                Response.Headers.Add("Content-Disposition", "attachment; filename=\"clob_data.txt\"");

                                // Stream the CLOB data as a file
                                using (Stream outputStream = Response.Body)
                                {
                                    byte[] buffer = new byte[1024]; // You can adjust the buffer size as needed
                                    long bytesRead;
                                    long offset = 0;
                                    long clobLength = clobData.Length;
                                    while (offset < clobLength && (bytesRead = clobData.Read(offset, buffer, 0, buffer.Length)) > 0)
                                    {
                                        outputStream.Write(buffer, 0, (int)bytesRead);
                                        offset += bytesRead;
                                    }
                                }

                                return new EmptyResult();
                            }
                        }
                    }
                }
            }

            return NotFound(); // CLOB data not found or empty
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }
}


using System;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using System.IO;

[ApiController]
[Route("api/[controller]")]
public class OracleClobController : ControllerBase
{
    private readonly string connectionString = "YOUR_ORACLE_CONNECTION_STRING";
    private readonly string selectQuery = "SELECT your_clob_column FROM your_table WHERE your_condition";

    [HttpGet]
    public IActionResult GetClobAsStream()
    {
        try
        {
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                connection.Open();
                using (OracleCommand command = new OracleCommand(selectQuery, connection))
                {
                    using (OracleDataReader reader = command.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        if (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                // Assuming the CLOB column is the first column (index 0)
                                OracleClob clobData = reader.GetOracleClob(0);

                                // Set the HTTP response headers
                                Response.ContentType = "application/octet-stream";
                                Response.Headers.Add("Content-Disposition", "attachment; filename=\"clob_data.txt\"");

                                // Stream the CLOB data as a file
                                using (Stream outputStream = Response.Body)
                                {
                                    using (Stream clobStream = clobData.GetStream())
                                    {
                                        clobStream.CopyTo(outputStream);
                                    }
                                }

                                return new EmptyResult();
                            }
                        }
                    }
                }
            }

            return NotFound(); // CLOB data not found or empty
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }
}


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
