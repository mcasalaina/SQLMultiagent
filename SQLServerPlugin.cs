using System;
using System.ComponentModel;
using Microsoft.Data.SqlClient;
using Microsoft.SemanticKernel;

namespace SQLMultiagent
{
    public class SQLServerPlugin
    {
        private readonly string _connectionString;

        public SQLServerPlugin(string connectionString)
        {
            _connectionString = connectionString;
        }

        //A constructor that takes in no arguments that sets the connection string to the database called AdventureWorks on SQL Server Express on localhost
        public SQLServerPlugin()
        {
            _connectionString = "Server=localhost\\SQLEXPRESS;Database=AdventureWorks;Trusted_Connection=True;TrustServerCertificate=True;";
        }

        [KernelFunction("ExecuteSqlQuery")]
        [Description("Executes a SQL query on the attached database.")]
        [return: Description("The result of the SQL query")]
        public async Task<string> ExecuteSqlQuery(string query)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            var result = "";
                            while (reader.Read())
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    result += reader.GetValue(i) + "\t";
                                }
                                result += Environment.NewLine;
                            }
                            return result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}
