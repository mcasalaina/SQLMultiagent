using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SQLMultiAgent
{
    /// <summary>
    /// Interaction logic for SideBySideWindow.xaml
    /// </summary>
    public partial class SideBySideWindow : Window
    {
        private SQLMultiAgentRunner rawRunner;
        private SQLMultiAgentRunner agentRunner;

        public SideBySideWindow()
        {
            InitializeComponent();
            rawRunner = new SQLMultiAgentRunner();
            agentRunner = new SQLMultiAgentRunner();
            
            rawRunner.AgentResponded += SQLMultiAgent_RawResponded;
            agentRunner.AgentResponded += SQLMultiAgent_AgentResponded;
        }

        private void SQLMultiAgent_RawResponded(object? sender, EventArgs e)
        {
            if (e is AgentRespondedEventArgs args)
            {
                Color color = Colors.Black;
                if (args.AgentName.Contains("Query"))
                {
                    color = Colors.DarkGray;
                }
                else if (args.AgentName.Contains("Assistant"))
                {
                    color = Colors.DarkOliveGreen;
                }

                UpdateResponseBox(RawResponse,args.AgentName, args.Response, color);
            }
        }

        private void SQLMultiAgent_AgentResponded(object? sender, EventArgs e)
        {
            if (e is AgentRespondedEventArgs args)
            {
                Color color = Colors.Black;
                if (args.AgentName.Contains("Query"))
                {
                    color = Colors.DarkGray;
                }
                else if (args.AgentName.Contains("Assistant"))
                {
                    color = Colors.DarkOliveGreen;
                }

                UpdateResponseBox(AgentResponse, args.AgentName, args.Response, color);
            }
        }

        public void ClearResponseBoxes()
        {
            //Clear out the response boxes
            RawResponse.Document.Blocks.Clear();
            AgentResponse.Document.Blocks.Clear();
        }

        public void UpdateResponseBox(RichTextBox box, string sender, string response, Color color)
        {
            //Update mainWindow.ResponseBox to add the sender in bold, a colon, a space, and the response in normal text
            Paragraph paragraph = new Paragraph();
            Bold bold = new Bold(new Run(sender + ": "));

            bold.Foreground = new SolidColorBrush(color);

            paragraph.Inlines.Add(bold);
            Run run = new Run(response);
            paragraph.Inlines.Add(run);
            box.Document.Blocks.Add(paragraph);

            Console.WriteLine(sender + ": " + response);
        }

        private async void AskButton_Click(object sender, RoutedEventArgs e)
        {
            await AskQuestion();
        }

        public async Task AskQuestion()
        {
            //Clear out the response box
            ClearResponseBoxes();

            rawRunner.question = QueryBox.Text;
            agentRunner.question = QueryBox.Text;

            // Create tasks for both methods
            Task rawTask = rawRunner.AskSingletonAgent();
            Task agentTask = agentRunner.AskSingletonAgentWithFunctions();

            // Wait for both tasks to complete
            await Task.WhenAll(rawTask, agentTask);
        }
    }
}
