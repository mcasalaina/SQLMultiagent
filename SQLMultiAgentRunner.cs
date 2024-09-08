using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Reflection.Metadata;
using System.Text.Json;
using System.IO;
using System.Diagnostics.Eventing.Reader;

#pragma warning disable SKEXP0110, SKEXP0001, SKEXP0050, CS8600, CS8604

namespace SQLMultiAgent
{
    public class SQLMultiAgentRunner
    {
        public const string SQL_WRITER_ASSISTANT_ID = "asst_nbUQUshgy8xkhlVNJodYwp0w";
        string? DEPLOYMENT_NAME = Environment.GetEnvironmentVariable("AZURE_OPENAI_MODEL_DEPLOYMENT");
        string? ENDPOINT = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        string? API_KEY = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");

        string Context = "AdventureWorks"; 
        public SQLMultiAgentRunner()
        {
            UpdatePrompts();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _question = "What was my best selling product?";
        public string question
        {
            get { return _question; }
            set
            {
                if (_question != value)
                {
                    _question = value;
                    OnPropertyChanged("Question");
                }
            }
        }

        private string _sqlQuery = "";
        public string sqlQuery
        {
            get { return _sqlQuery; }
            set
            {
                if (_sqlQuery != value)
                {
                    _sqlQuery = value;
                    OnPropertyChanged("Answer");
                }
            }
        }

        private string _queryResponse = "";
        public string queryResponse
        {
            get { return _queryResponse; }
            set
            {
                if (_queryResponse != value)
                {
                    _queryResponse = value;
                    //OnPropertyChanged("QueryResponse");
                }
            }
        }

        public event EventHandler? AgentResponded;
        public void EmitResponse(string agentName, string response)
        {
            String formattedResponse = $"Agent {agentName}: {response}";
            Console.WriteLine(formattedResponse);
            //Add the formattedResponse to queryResponse with a newline
            queryResponse += formattedResponse + "\n";

            AgentResponded?.Invoke(this, new AgentRespondedEventArgs(agentName,response));
        }

        public void EmitResponse(ChatMessageContent content)
        {
            string contentRole="";
            if (content.Role == AuthorRole.Tool)
            {
                contentRole = "Tool";
            }
            else if (content.Role == AuthorRole.Assistant) 
            { 
                contentRole = "Assistant";
            } 
            else if (content.Role == AuthorRole.User)
            {
                contentRole = "User";
            }
            else
            {
                contentRole = "Agent";
            }
            
            EmitResponse(contentRole,content.Content);
        }

        string? QueryWriterPrompt;
        string? QueryCheckerPrompt;
        string? ManagerPrompt;
        string? ExplainerPrompt;
        protected Kernel CreateKernelWithChatCompletion(bool addSqlTool)
        {
            var builder = Kernel.CreateBuilder();
            
            builder.AddAzureOpenAIChatCompletion(
                DEPLOYMENT_NAME,
                ENDPOINT,
                API_KEY);

            if (addSqlTool)
            {
                builder.Plugins.AddFromType<SQLServerPlugin>();
            }

            return builder.Build();
        }


        /// <summary>
        /// Asks the question with a semi-deterministic pattern, first asking the SQL Assistant as a singleton agent,
        /// then running the query and the answer by the checker multiagent system, which can approve or reject the answer.
        /// A rejection triggers a rewrite cycle.
        /// </summary>
        /// <returns></returns>
        public async Task AskSemiDeterministic()
        {
            await AskSingletonAgent();

            //Make a prompt for the multiagent that contains the SQL query and the response from the SQL query
            QueryCheckerPrompt = $"""
                You are an agent that checks queries on the {Context} database to ensure 
                that they are correct and consistent with what the user requested.
                The user's natural language question has been converted by a different agent 
                into a SQL query that retrieves the answer from the {Context} database.

                The user's request was:
                {question}

                The SQL query the other agent generated is: 
                {sqlQuery}

                The response is: 
                {queryResponse}

                If this query is consistent with the user's response, you approve the query by replying only "Approve" with no further text.

                If the query is incorrect, you reject the query by replying "Reject - " with text indicating the reason for rejection.
            """;

            ChatCompletionAgent queryCheckerAgent =
            new()
            {
                Instructions = QueryCheckerPrompt,
                Name = "QueryCheckerAgent",
                Kernel = this.CreateKernelWithChatCompletion(true),
                Arguments = this.MakeKernelArguments()
            };

            ChatCompletionAgent managerAgent =
                new()
                {
                    Instructions = ManagerPrompt,
                    Name = "ManagerAgent",
                    Kernel = this.CreateKernelWithChatCompletion(true),
                    Arguments = MakeKernelArguments()
                };

            ChatCompletionAgent explainerAgent =
                new()
                {
                    Instructions = ExplainerPrompt,
                    Name = "ExplainerAgent",
                    Kernel = this.CreateKernelWithChatCompletion(true),
                    Arguments = MakeKernelArguments()
                };

            // Create a chat for agent interaction.
            AgentGroupChat chat =
                new(queryCheckerAgent, explainerAgent, managerAgent)
                {
                    ExecutionSettings =
                        new()
                        {
                            // Here a TerminationStrategy subclass is used that will just terminate when the manager comes up.
                            TerminationStrategy =
                                new SequentialTerminationStrategy()
                                {
                                    Agents = [managerAgent],
                                }
                        }
                };

            // Invoke chat and display messages.
            chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, question));
            Console.WriteLine($"# {AuthorRole.User}: '{question}'");

            StringWriter queryResponseWriter = new StringWriter();

            await foreach (ChatMessageContent content in chat.InvokeAsync())
            {
                string output = $"# {content.Role} - {content.AuthorName ?? "*"}: '{content.Content}'";
                Console.WriteLine(output);
                queryResponseWriter.WriteLine(output);
            }

            Console.WriteLine($"# IS COMPLETE: {chat.IsComplete}");

            queryResponse = queryResponseWriter.ToString();
        }

        //Asks the question using the four agents, including the SQL Assistant, with the SQLServerPlugin as a tool
        public async Task AskMultiAgent()
        {
            IKernelBuilder builder = Kernel.CreateBuilder();
            Kernel kernel = builder.AddAzureOpenAIChatCompletion(
                            deploymentName: DEPLOYMENT_NAME,
                            endpoint: ENDPOINT,
                            apiKey: API_KEY)
                        .Build();

            OpenAIClientProvider provider = OpenAIClientProvider.ForAzureOpenAI(API_KEY, new Uri(ENDPOINT));

            OpenAIAssistantAgent sqlAssistantAgent = await OpenAIAssistantAgent.RetrieveAsync(
                kernel,
                provider,
                SQL_WRITER_ASSISTANT_ID);

            ChatCompletionAgent queryCheckerAgent =
            new()
            {
                Instructions = QueryCheckerPrompt,
                Name = "QueryCheckerAgent",
                Kernel = this.CreateKernelWithChatCompletion(true),
                Arguments = this.MakeKernelArguments()
            };

            ChatCompletionAgent managerAgent =
                new()
                {
                    Instructions = ManagerPrompt,
                    Name = "ManagerAgent",
                    Kernel = this.CreateKernelWithChatCompletion(true),
                    Arguments = MakeKernelArguments()
                };

            ChatCompletionAgent explainerAgent =
                new()
                {
                    Instructions = ExplainerPrompt,
                    Name = "ExplainerAgent",
                    Kernel = this.CreateKernelWithChatCompletion(true),
                    Arguments = MakeKernelArguments()
                };

            // Create a chat for agent interaction.
            AgentGroupChat chat =
                new(sqlAssistantAgent, queryCheckerAgent, explainerAgent, managerAgent)
                {
                    ExecutionSettings =
                        new()
                        {
                            // Here a TerminationStrategy subclass is used that will terminate when
                            // an assistant message contains the term "approve".
                            TerminationStrategy =
                                new ApprovalTerminationStrategy()
                                {
                                    // Only the manager may approve.
                                    Agents = [managerAgent],
                                    // Limit total number of turns
                                    MaximumIterations = 10,
                                }
                        }
                };

            // Invoke chat and display messages.
            chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, question));
            Console.WriteLine($"# {AuthorRole.User}: '{question}'");

            StringWriter queryResponseWriter = new StringWriter();

            await foreach (ChatMessageContent content in chat.InvokeAsync())
            {
                string output = $"# {content.Role} - {content.AuthorName ?? "*"}: '{content.Content}'";
                Console.WriteLine(output);
                queryResponseWriter.WriteLine(output);
                EmitResponse(content);
            }

            Console.WriteLine($"# IS COMPLETE: {chat.IsComplete}");

            queryResponse = queryResponseWriter.ToString();

        }

        /// <summary>
        /// Convenience method to create a <see cref="KernelArguments"/> object with the appropriate settings for the OpenAI prompt execution.
        /// </summary>
        /// <returns></returns>
        private KernelArguments MakeKernelArguments()
        {
            return new KernelArguments(
                new OpenAIPromptExecutionSettings()
                {
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                }
            );
        }

        public async Task AskSingletonAgentWithFunctions()
        {
            EmitResponse("User", question);

            IKernelBuilder builder = Kernel.CreateBuilder();
            Kernel kernel = builder.AddAzureOpenAIChatCompletion(
                            deploymentName: DEPLOYMENT_NAME,
                            endpoint: ENDPOINT,
                            apiKey: API_KEY)
                        .Build();

            OpenAIClientProvider provider = OpenAIClientProvider.ForAzureOpenAI(API_KEY, new Uri(ENDPOINT));

            // Define the agent
            OpenAIAssistantAgent agent =
                await OpenAIAssistantAgent.CreateAsync(
                    kernel: new(),
                    clientProvider: provider,
                    new(DEPLOYMENT_NAME)
                    {
                        Instructions = 
                            "You are a SQL query generating agent. You generate SQL based on the attached PDF of the AdventureWorks schema." +
                            "You then run that SQL query using the provided function. You evaluate the results, and regenerate the query and re-run the function" +
                            "until the query response satisfies the user's request.",
                        Name = "AdventureWorks SQL Assistant",
                    });

            // Initialize plugin and add to the agent's Kernel (same as direct Kernel usage).
            KernelPlugin plugin = KernelPluginFactory.CreateFromType<SQLServerPlugin>();
            agent.Kernel.Plugins.Add(plugin);

            // Create a thread for the agent conversation.
            string threadId = await agent.CreateThreadAsync(new OpenAIThreadCreationOptions {});

            // Respond to user input
            try
            {
                ChatMessageContent message = new(AuthorRole.User, question);
                await agent.AddChatMessageAsync(threadId, message);
                if (message.Role != AuthorRole.User) { 
                    EmitResponse(message);
                }   

                await foreach (ChatMessageContent response in agent.InvokeAsync(threadId))
                {
                    EmitResponse(response);
                }
            }
            finally
            {
                await agent.DeleteThreadAsync(threadId);
                await agent.DeleteAsync();
            }
        }

        /// <summary>
        /// Asks the question using only the singleton Assistant and then runs the SQL query directly with no further interaction
        /// </summary>
        /// <returns>A Task object, as this runs asynchronously.</returns>
        public async Task AskSingletonAgent()
        {
            EmitResponse("User", question);

            IKernelBuilder builder = Kernel.CreateBuilder();
            Kernel kernel = builder.AddAzureOpenAIChatCompletion(
                            deploymentName: DEPLOYMENT_NAME,
                            endpoint: ENDPOINT,
                            apiKey: API_KEY)
                        .Build();
            

            OpenAIClientProvider provider = OpenAIClientProvider.ForAzureOpenAI(API_KEY, new Uri(ENDPOINT));

            OpenAIAssistantAgent sqlAssistant = await OpenAIAssistantAgent.RetrieveAsync(
                kernel,
                provider,
                SQL_WRITER_ASSISTANT_ID);

            // Create a thread for the agent interaction.
            string threadId = await sqlAssistant.CreateThreadAsync();

            await sqlAssistant.AddChatMessageAsync(threadId, new ChatMessageContent(AuthorRole.User, question));

            string jsonResponse = "";

            await foreach (ChatMessageContent content in sqlAssistant.InvokeAsync(threadId))
            {
                if (content.Role != AuthorRole.Tool)
                {
                    EmitResponse(content.AuthorName, content.Content);
                    Console.WriteLine($"# {content.Role} - {content.AuthorName ?? "*"}: '{content.Content}'");
                }
                jsonResponse += content.Content;
            }

            // Inside the askQuestion method, replace the problematic line with:
            using (JsonDocument jsonDoc = JsonDocument.Parse(jsonResponse))
            {
                sqlQuery = jsonDoc.RootElement.GetProperty("query").GetString();

                //Run the SQL query in the variable called sqlQuery
                SQLServerPlugin plugin = new SQLServerPlugin();
                queryResponse = await plugin.ExecuteSqlQuery(sqlQuery);
            }

            EmitResponse("Query Runner", queryResponse);
        }

        public void UpdatePrompts()
        {
            QueryWriterPrompt = $"""
                You write queries on the {Context} database.
                You take in a natural language question and convert it into a SQL query that retrieves the answer from the {Context} database.
            """;

            QueryCheckerPrompt = $"""
                You are a query checker for the {Context} database. Your responses always start with either the words QUERY CORRECT or QUERY INCORRECT.
                If the query is correct and retrieves the correct answer, you respond "QUERY CORRECT" with no further explanation.
                If the query is incorrect, or produces an incorrect result, you respond "QUERY INCORRECT" and provide your reasoning.
                You do not output anything other than "QUERY CORRECT" or "QUERY INCORRECT - <reasoning>".
            """;

            ExplainerPrompt = """
                You explain the process to arrive at the result using natural language. 
                You examine the SQL queries that the query writer agent generated and the responses from the query checker agent,
                and you explain to the user (without actually outputting SQL) how it arrived at the answer.
            """;

            ManagerPrompt = """
                You are a manager which reviews the original question and the query checker's response.
                If the query checker replies "QUERY INCORRECT", you reply "reject" and ask the query writer agent to correct the query.
                Once the question has been answered properly according to the query checker agent, you can approve the request by just responding "approve".
                You do not output anything other than "reject" or "approve".
            """;
        }


    }
}
