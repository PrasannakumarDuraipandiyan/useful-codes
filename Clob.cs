using System;
using System.Data;
using System.Data.Common;
using Oracle.ManagedDataAccess.Client;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

var connectionString = builder.Configuration.GetConnectionString("OracleConnection");

var app = builder.Build();

app.MapGet("/api/csv", async (DbConnection connection) =>
{
    var query = "SELECT csv_data FROM your_table WHERE some_condition = :param";

    // Replace ":param" with an appropriate parameter value to retrieve the correct data.

    await connection.OpenAsync();

    using var command = connection.CreateCommand();
    command.CommandText = query;

    using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

    if (await reader.ReadAsync())
    {
        if (!reader.IsDBNull(0))
        {
            var blob = new byte[reader.GetBytes(0, 0, null, 0, int.MaxValue)];
            reader.GetBytes(0, 0, blob, 0, blob.Length);

            var stream = new MemoryStream(blob);
            return new StreamCsvResult(stream, "data.csv");
        }
    }

    return Results.NotFound();
});

app.Services.AddScoped(serviceProvider =>
{
    var connection = new OracleConnection(connectionString);
    return connection;
});

app.Run();

public class StreamCsvResult : IActionResult
{
    private readonly Stream _stream;
    private readonly string _fileName;

    public StreamCsvResult(Stream stream, string fileName)
    {
        _stream = stream;
        _fileName = fileName;
    }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        response.ContentType = "text/csv";
        response.Headers.Add("Content-Disposition", $"attachment; filename={_fileName}");

        try
        {
            await _stream.CopyToAsync(response.Body);
        }
        catch (Exception ex)
        {
            // Log the error or handle it accordingly.
        }
        finally
        {
            _stream.Close();
        }
    }
}


using System;
using System.Data;
using System.Data.Common;
using Oracle.ManagedDataAccess.Client;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

var connectionString = builder.Configuration.GetConnectionString("OracleConnection");

var app = builder.Build();

app.MapGet("/api/csv", async (DbConnection connection) =>
{
    var query = "SELECT csv_data FROM your_table WHERE some_condition = :param";

    // Replace ":param" with an appropriate parameter value to retrieve the correct data.

    await connection.OpenAsync();

    using var command = connection.CreateCommand();
    command.CommandText = query;

    using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

    if (await reader.ReadAsync())
    {
        if (!reader.IsDBNull(0))
        {
            var stream = reader.GetStream(0);
            return new StreamCsvResult(stream, "data.csv");
        }
    }

    return Results.NotFound();
});

app.Services.AddScoped(serviceProvider =>
{
    var connection = new OracleConnection(connectionString);
    return connection;
});

app.Run();

public class StreamCsvResult : IActionResult
{
    private readonly Stream _stream;
    private readonly string _fileName;

    public StreamCsvResult(Stream stream, string fileName)
    {
        _stream = stream;
        _fileName = fileName;
    }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        response.ContentType = "text/csv";
        response.Headers.Add("Content-Disposition", $"attachment; filename={_fileName}");

        try
        {
            await _stream.CopyToAsync(response.Body);
        }
        catch (Exception ex)
        {
            // Log the error or handle it accordingly.
        }
        finally
        {
            _stream.Close();
        }
    }
}


using System;
using System.Data;
using System.Data.Common;
using Oracle.ManagedDataAccess.Client;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

var connectionString = builder.Configuration.GetConnectionString("OracleConnection");

var app = builder.Build();

app.MapGet("/api/csv", async (DbConnection connection) =>
{
    var query = "SELECT csv_data FROM your_table WHERE some_condition = :param";

    // Replace ":param" with an appropriate parameter value to retrieve the correct data.

    await connection.OpenAsync();

    using var command = connection.CreateCommand();
    command.CommandText = query;

    using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

    if (await reader.ReadAsync())
    {
        if (!reader.IsDBNull(0))
        {
            var stream = reader.GetStream(0);
            return StreamCsvData(stream, "data.csv");
        }
    }

    return Results.NotFound();
});

app.Services.AddScoped(serviceProvider =>
{
    var connection = new OracleConnection(connectionString);
    return connection;
});

app.Run();

private IActionResult StreamCsvData(Stream stream, string fileName)
{
    var response = new FileCallbackResult("text/csv", async (responseStream, _) =>
    {
        try
        {
            await stream.CopyToAsync(responseStream);
        }
        catch (Exception ex)
        {
            // Log the error or handle it accordingly.
        }
        finally
        {
            stream.Close();
        }
    });

    response.FileDownloadName = fileName;
    return response;
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
                                    using (StreamReader clobReader = new StreamReader(clobData, true))
                                    {
                                        char[] buffer = new char[1024]; // You can adjust the buffer size as needed
                                        int bytesRead;
                                        while ((bytesRead = clobReader.Read(buffer, 0, buffer.Length)) > 0)
                                        {
                                            char[] data = new char[bytesRead];
                                            Array.Copy(buffer, data, bytesRead);
                                            outputStream.Write(System.Text.Encoding.UTF8.GetBytes(data), 0, bytesRead);
                                        }
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
