using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLMultiAgent
{
    /// <summary>
    /// Event arguments for when an agent has responded.
    /// </summary>
    public class AgentRespondedEventArgs : EventArgs
    {
        /// <summary>
        /// The response from the agent.
        /// </summary>
        public string Response { get; private set; }

        /// <summary>
        /// The agent that responded.
        /// </summary>
        public string AgentName { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="response">The response from the agent.</param>
        public AgentRespondedEventArgs(string agentName, string response)
        {
            AgentName = agentName;
            Response = response;
        }
    }
}
