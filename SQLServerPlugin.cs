using System;
using System.ComponentModel;
using Microsoft.Data.SqlClient;
using Microsoft.SemanticKernel;

namespace SQLMultiAgent
{
    public class SQLServerPlugin
    {
        private string _connectionString;
        private SQLMultiAgent? _multiAgent;

        public string ConnectionString
        {
            get { return _connectionString; }
            set { _connectionString = value; }
        }

        //A constructor that takes in no arguments that sets the connection string to the database called AdventureWorks on SQL Server Express on localhost
        public SQLServerPlugin()
        {
            _connectionString = "Server=localhost\\SQLEXPRESS;Database=AdventureWorks;Trusted_Connection=True;TrustServerCertificate=True;";
        }

        public string executeSqlQuerySchema = @"
        {
          ""$schema"": ""http://json-schema.org/draft-07/schema#"",
          ""title"": ""AssistantExecuteSqlQuery"",
          ""description"": ""Executes a SQL query on the attached database."",
          ""type"": ""object"",
          ""properties"": {
            ""query"": {
              ""type"": ""string"",
              ""description"": ""The SQL query to execute""
            }
          },
          ""required"": [""query""],
         ""additionalProperties"": false
        }";

        /// <summary>
        /// An SK function for an Assistant that executes a SQL query on the attached database.
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [KernelFunction("AssistantExecuteSqlQuery")]
        [Description("Executes a SQL query on the attached database.")]
        [return: Description("The result of the SQL query")]
        public string AssistantExecuteSqlQuery(string query)
        {
            return ExecuteSqlQuery(query).Result;
        }

        
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

                            //_multiAgent.EmitResponse("SQL Runner", "Ran query " + query);
                            Console.WriteLine("Ran query " + query);
                            Console.WriteLine(result);
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
