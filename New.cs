using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        string rawQuery = "SELECT * FROM your_table ORDER BY your_column"; // Replace with your raw SQL query
        int pageSize = 100; // Number of records per page
        List<string> queries = GeneratePaginationQueries(rawQuery, pageSize);
        
        // Print the generated queries
        foreach (string query in queries)
        {
            Console.WriteLine(query);
        }
    }
    
    static List<string> GeneratePaginationQueries(string rawQuery, int pageSize)
    {
        List<string> queries = new List<string>();
        
        int currentPage = 1;
        int startRow = 1;
        int endRow = pageSize;
        
        while (true)
        {
            string paginationQuery = $"SELECT * FROM ({rawQuery}) WHERE ROWNUM BETWEEN {startRow} AND {endRow}";
            queries.Add(paginationQuery);
            
            currentPage++;
            startRow = endRow + 1;
            endRow = currentPage * pageSize;
            
            if (startRow > endRow)
            {
                break;
            }
        }
        
        return queries;
    }
}

