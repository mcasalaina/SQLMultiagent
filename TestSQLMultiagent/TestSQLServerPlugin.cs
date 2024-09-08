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
            SQLServerPlugin plugin = new SQLServerPlugin(new SQLMultiAgentRunner());
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
            SQLMultiAgent.SQLMultiAgentRunner agent = new SQLMultiAgent.SQLMultiAgentRunner();
            agent.question = SIMPLE_QUERY_1;
            await agent.AskSingletonAgent();

            Assert.IsNotNull(agent.sqlQuery);
            Assert.IsNotEmpty(agent.sqlQuery);

            Assert.IsNotEmpty(agent.queryResponse);
        }

        [Test]
        public static async Task TestAskSingletonWithFunctions()
        {
            SQLMultiAgent.SQLMultiAgentRunner agent = new SQLMultiAgent.SQLMultiAgentRunner();
            agent.question = SIMPLE_QUERY_1;
            await agent.AskSingletonAgentWithFunctions();

            Assert.IsNotEmpty(agent.queryResponse);
        }

        [Test]
        public static async Task TestAskSingletonWithFunctionsComplexQuery1()
        {
            SQLMultiAgent.SQLMultiAgentRunner agent = new SQLMultiAgent.SQLMultiAgentRunner();
            agent.question = COMPLEX_QUERY_1;
            await agent.AskSingletonAgentWithFunctions();

            Assert.IsNotEmpty(agent.queryResponse);
        }

        [Test]
        public static async Task TestAskMultiAgent()
        {
            SQLMultiAgent.SQLMultiAgentRunner agent = new SQLMultiAgent.SQLMultiAgentRunner();
            agent.question = COMPLEX_QUERY_1;
            await agent.AskMultiAgent();

            Assert.IsNotEmpty(agent.queryResponse);
        }

        [Test]
        public static async Task TestAskQuestionSemiDeterministic()
        {
            SQLMultiAgent.SQLMultiAgentRunner agent = new SQLMultiAgent.SQLMultiAgentRunner();
            agent.question = SIMPLE_QUERY_1;
            await agent.AskSemiDeterministic();

            Assert.IsNotNull(agent.sqlQuery);
            Assert.IsNotEmpty(agent.sqlQuery);

            Assert.IsNotEmpty(agent.queryResponse);
        }
    }
}
