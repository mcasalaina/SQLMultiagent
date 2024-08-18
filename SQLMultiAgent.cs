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

#pragma warning disable SKEXP0110, SKEXP0001, SKEXP0050, CS8600, CS8604

namespace SQLMultiagent
{
    public class SQLMultiAgent
    {
        public const string SQL_WRITER_ASSISTANT_ID = "asst_nbUQUshgy8xkhlVNJodYwp0w";
        string? DEPLOYMENT_NAME = Environment.GetEnvironmentVariable("AZURE_OPENAI_MODEL_DEPLOYMENT");
        string? ENDPOINT = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        string? API_KEY = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");

        string Context = "AdventureWorks"; 
        public SQLMultiAgent()
        {
            updatePrompts();
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
                    OnPropertyChanged("QueryResponse");
                }
            }
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

        //Asks the question using the four agents, including the SQL Assistant, with the SQLServerPlugin as a tool
        public async Task askQuestionMultiAgent()
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

        //Asks the question using only the singleton Assistant and then runs the SQL query directly with no further interaction
        public async Task askQuestionSingletonAgent()
        {
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

            Console.WriteLine($"# {AuthorRole.User}: '{question}'");

            string jsonResponse = "";

            await foreach (ChatMessageContent content in sqlAssistant.InvokeAsync(threadId))
            {
                if (content.Role != AuthorRole.Tool)
                {
                    Console.WriteLine($"# {content.Role} - {content.AuthorName ?? "*"}: '{content.Content}'");
                }
                jsonResponse += content.Content;
            }

            // Inside the askQuestion method, replace the problematic line with:
            using (JsonDocument jsonDoc = JsonDocument.Parse(jsonResponse))
            {
                sqlQuery = jsonDoc.RootElement.GetProperty("query").GetString();
            }

            //Run the SQL query in the variable called sqlQuery
            SQLServerPlugin plugin = new SQLServerPlugin();
            queryResponse = await plugin.ExecuteSqlQuery(sqlQuery);

            Console.WriteLine($"# {queryResponse}");
        }

        public void updatePrompts()
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
