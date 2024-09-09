using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SQLMultiAgent;

namespace TestSQLMultiagent
{
    internal class TestMultiAgent
    {
        private const string COMPLEX_QUERY_1 = "What salesperson sold the most of my best-selling product?";
        private const string SIMPLE_QUERY_1 = "What was my best selling product?";

        [Test]
        public static async Task TestAskMultiAgent()
        {
            SQLMultiAgent.SQLMultiAgentRunner agent = new SQLMultiAgent.SQLMultiAgentRunner();
            agent.question = COMPLEX_QUERY_1;
            await agent.AskMultiAgent();

            Assert.IsNotEmpty(agent.queryResponse);
        }
    }
}
