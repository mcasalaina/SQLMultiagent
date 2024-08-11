using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SQLMultiagent;

namespace TestSQLMultiagent
{
    internal class TestSQLServerPlugin
    {
        //Declare this as a test
        [Test]
        public static async Task TestExecuteSqlQuery()
        {
            SQLServerPlugin plugin = new SQLServerPlugin();
            string query = "SELECT TOP 10 * FROM Production.Product";
            string result = await plugin.ExecuteSqlQuery(query);
            Console.WriteLine(result);

            Assert.IsNotNull(result);
            Assert.IsNotEmpty(result);
            //Assert that the result does not contain the case insensitive string "error"
            Assert.IsFalse(result.ToLower().Contains("error"));

            //Assert that the result contains the word "Adjustable"
            Assert.IsTrue(result.Contains("Adjustable"));
        }
    }
}
