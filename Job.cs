static async Task Main(string[] args)
{
    if (args.Length == 0)
    {
        Console.WriteLine("Usage: OracleBatchJob <procedure_name>");
        return;
    }

    string procedureName = args[0];

    string connectionString = "Data Source=myOracleDB;User Id=myUsername;Password=myPassword;";

    using (var connection = new OracleConnection(connectionString))
    {
        await connection.OpenAsync();

        using (var command = connection.CreateCommand())
        {
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = procedureName;

            // Add any input parameters to the command here
            // command.Parameters.Add(new OracleParameter("param_name", OracleType.VarChar)).Value = "param_value";

            await command.ExecuteNonQueryAsync();
        }
    }
}
