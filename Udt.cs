using System;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;

// ...

public List<string> GetClobsFromOracleProcedure()
{
    List<string> clobsList = new List<string>();

    // Your Oracle connection string
    string connectionString = "Your_Oracle_Connection_String";

    using (var connection = new OracleConnection(connectionString))
    {
        // Open the connection
        connection.Open();

        // Create a command for calling the Oracle procedure
        using (var command = connection.CreateCommand())
        {
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "GET_CLOBS";

            // Add the out parameter for the VARRAY
            var outParameter = new OracleParameter
            {
                ParameterName = "p_clobs",
                OracleDbType = OracleDbType.Clob,
                Direction = ParameterDirection.Output
            };

            command.Parameters.Add(outParameter);

            // Execute the procedure
            command.ExecuteNonQuery();

            // Retrieve the result from the out parameter
            if (outParameter.Value is OracleClob[] resultArray)
            {
                foreach (var clob in resultArray)
                {
                    // Process each CLOB value
                    clobsList.Add(clob.Value);
                }
            }
        }
    }

    return clobsList;
}
