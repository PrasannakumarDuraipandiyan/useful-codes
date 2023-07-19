using System;
using System.Text.RegularExpressions;

public class OracleQuerySplitter
{
    public static string[] SplitIntoBatches(string oracleQuery, int batchSize)
    {
        // Add rownum/row_number to the query to enable pagination
        string paginatedQuery = AddPaginationToQuery(oracleQuery);

        // Split the query into batches with pagination
        string[] batches = SplitQueryIntoBatches(paginatedQuery, batchSize);

        return batches;
    }

    private static string AddPaginationToQuery(string oracleQuery)
    {
        // Check if the query already contains pagination
        if (Regex.IsMatch(oracleQuery, @"\b(ROWNUM|ROW_NUMBER\s*\(\s*\))\b", RegexOptions.IgnoreCase))
        {
            return oracleQuery;
        }

        // Append rownum/row_number to enable pagination
        return $"SELECT inner_query.*, ROWNUM AS rn FROM ({oracleQuery}) inner_query";
    }

    private static string[] SplitQueryIntoBatches(string paginatedQuery, int batchSize)
    {
        int pageCount = (int)Math.Ceiling((double)GetTotalRecordCount(paginatedQuery) / batchSize);
        string[] batches = new string[pageCount];

        for (int i = 0; i < pageCount; i++)
        {
            int offset = i * batchSize;
            int limit = batchSize;

            // Construct batch query with pagination
            string batchQuery = $"SELECT * FROM ({paginatedQuery}) WHERE rn > {offset} AND rn <= {offset + limit}";

            batches[i] = batchQuery;
        }

        return batches;
    }

    private static int GetTotalRecordCount(string paginatedQuery)
    {
        // Replace SELECT columns with COUNT(*)
        string countQuery = Regex.Replace(paginatedQuery, @"SELECT\s+(.*?)\s+FROM", "SELECT COUNT(*) FROM");

        // Execute count query to get the total record count
        int totalRecordCount = 1000000; // Replace this with the actual count from the database

        return totalRecordCount;
    }
}
