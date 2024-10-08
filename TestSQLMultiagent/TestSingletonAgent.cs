﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SQLMultiAgent;

namespace TestSQLMultiagent
{
    internal class TestSingletonAgent
    {
        private const string COMPLEX_QUERY_1 = "What salesperson sold the most of my best-selling product?";
        private const string SIMPLE_QUERY_1 = "What was my best selling product?";

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
    }
}
