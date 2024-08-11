using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable SKEXP0110, SKEXP0001, SKEXP0050, CS8600, CS8604

namespace SQLMultiagent
{
    class SQLMultiAgent
    {
        string? DEPLOYMENT_NAME = Environment.GetEnvironmentVariable("AZURE_OPENAI_MODEL_DEPLOYMENT");
        string? ENDPOINT = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        string? API_KEY = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");

        const string Context = "AdventureWorks"; 
        public SQLMultiAgent()
        {
            updatePrompts();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _Question = "What was my best selling product?";
        public string Question
        {
            get { return _Question; }
            set
            {
                if (_Question != value)
                {
                    _Question = value;
                    OnPropertyChanged("Question");
                }
            }
        }

        string? QueryWriterPrompt;
        string? QueryCheckerPrompt;
        string? ManagerPrompt;
        string? ExplainerPrompt;

        public async Task askQuestion()
        {
            IKernelBuilder builder = Kernel.CreateBuilder();
            Kernel kernel = builder.AddAzureOpenAIChatCompletion(
                            deploymentName: DEPLOYMENT_NAME,
                            endpoint: ENDPOINT,
                            apiKey: API_KEY)
                        .Build();

            ChatCompletionAgent QueryWriterAgent =
                new()
                {
                    Instructions = QueryWriterPrompt,
                    Name = "QuestionAnswererAgent",
                    Kernel = kernel,
                    ExecutionSettings = new OpenAIPromptExecutionSettings
                    {
                        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                    }
                };

            ChatCompletionAgent QueryCheckerAgent =
                new()
                {
                    Instructions = QueryCheckerPrompt,
                    Name = "QueryCheckerAgent",
                    Kernel = kernel,
                    ExecutionSettings = new OpenAIPromptExecutionSettings
                    {
                        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                    }
                };

            ChatCompletionAgent ManagerAgent =
                new()
                {
                    Instructions = ManagerPrompt,
                    Name = "ManagerAgent",
                    Kernel = kernel,
                    ExecutionSettings = new OpenAIPromptExecutionSettings
                    {
                        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                    }
                };

            ChatCompletionAgent ExplainerAgent =
                new()
                {
                    Instructions = ExplainerPrompt,
                    Name = "ExplainerAgent",
                    Kernel = kernel,
                    ExecutionSettings = new OpenAIPromptExecutionSettings
                    {
                        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                    }
                };

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
