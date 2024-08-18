using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Agents.OpenAI;
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

        [Test]
        public static async Task TestAskQuestion()
        {
            SQLMultiAgent agent = new SQLMultiAgent();
            agent.question = "What was my best selling product?";
            await agent.askQuestionSingletonAgent();

            Assert.IsNotNull(agent.sqlQuery);
            Assert.IsNotEmpty(agent.sqlQuery);

            Assert.IsNotEmpty(agent.queryResponse);
        }

        [Test]
        public static async Task TestAskQuestionWithParameters()
        {
            SQLMultiAgent agent = new SQLMultiAgent();
            agent.question = "What salesperson sold the most of my best-selling product?";
            await agent.askQuestionMultiAgent();

            Assert.IsNotNull(agent.sqlQuery);
            Assert.IsNotEmpty(agent.sqlQuery);

            Assert.IsNotEmpty(agent.queryResponse);
        }

        [Test]
        public static async Task TestAskQuestionSemiDeterministic()
        {
            SQLMultiAgent agent = new SQLMultiAgent();
            agent.question = "What was my best selling product?";
            await agent.askQuestionSemiDeterministic();

            Assert.IsNotNull(agent.sqlQuery);
            Assert.IsNotEmpty(agent.sqlQuery);

            Assert.IsNotEmpty(agent.queryResponse);
        }
    }
}
