using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Agents.OpenAI;
using NUnit.Framework;
using SQLMultiAgent;

namespace TestSQLMultiagent
{
    internal class TestSQLServerPlugin
    {
        private const string COMPLEX_QUERY_1 = "What salesperson sold the most of my best-selling product?";
        private const string SIMPLE_QUERY_1 = "What was my best selling product?";

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
            SQLMultiAgent.SQLMultiAgent agent = new SQLMultiAgent.SQLMultiAgent();
            agent.question = SIMPLE_QUERY_1;
            await agent.askSingletonAgent(false);

            Assert.IsNotNull(agent.sqlQuery);
            Assert.IsNotEmpty(agent.sqlQuery);

            Assert.IsNotEmpty(agent.queryResponse);
        }

        [Test]
        public static async Task TestAskSingletonWithFunctions()
        {
            SQLMultiAgent.SQLMultiAgent agent = new SQLMultiAgent.SQLMultiAgent();
            agent.question = SIMPLE_QUERY_1;
            await agent.askSingletonAgent(true);

            Assert.IsNotNull(agent.sqlQuery);
            Assert.IsNotEmpty(agent.sqlQuery);

            Assert.IsNotEmpty(agent.queryResponse);
        }

        [Test]
        public static async Task TestAskSingletonWithFunctionsComplexQuery1()
        {
            SQLMultiAgent.SQLMultiAgent agent = new SQLMultiAgent.SQLMultiAgent();
            agent.question = COMPLEX_QUERY_1;
            await agent.askSingletonAgent(true);

            Assert.IsNotNull(agent.sqlQuery);
            Assert.IsNotEmpty(agent.sqlQuery);

            Assert.IsNotEmpty(agent.queryResponse);
        }

        [Test]
        public static async Task TestAskQuestionWithParameters()
        {
            SQLMultiAgent.SQLMultiAgent agent = new SQLMultiAgent.SQLMultiAgent();
            agent.question = COMPLEX_QUERY_1;
            await agent.askMultiagent();

            Assert.IsNotNull(agent.sqlQuery);
            Assert.IsNotEmpty(agent.sqlQuery);

            Assert.IsNotEmpty(agent.queryResponse);
        }

        [Test]
        public static async Task TestAskQuestionSemiDeterministic()
        {
            SQLMultiAgent.SQLMultiAgent agent = new SQLMultiAgent.SQLMultiAgent();
            agent.question = SIMPLE_QUERY_1;
            await agent.askSemiDeterministic();

            Assert.IsNotNull(agent.sqlQuery);
            Assert.IsNotEmpty(agent.sqlQuery);

            Assert.IsNotEmpty(agent.queryResponse);
        }
    }
}
