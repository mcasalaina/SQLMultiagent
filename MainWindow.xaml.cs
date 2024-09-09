using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SQLMultiAgent
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const string DefaultQuestion = "What was my best selling product?";

        private SQLMultiAgentRunner multiAgent;
        public MainWindow()
        {
            InitializeComponent();
            multiAgent = new SQLMultiAgentRunner();
            this.DataContext = multiAgent;
            multiAgent.AgentResponded += SQLMultiAgent_AgentResponded;
        }
        private async void AskButton_Click(object sender, RoutedEventArgs e)
        {
            await AskQuestion();
        }

        private void SQLMultiAgent_AgentResponded(object? sender, EventArgs e)
        {
            if (e is AgentRespondedEventArgs args)
            {
                UpdateResponseBox(args.AgentName, args.Response, Colors.Black);
            }
        }
        private void QuestionBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AskButton_Click(sender, e);
            }
        }

        public async Task AskQuestion()
        {
            //Clear out the response box
            ClearResponseBox();

            //Case statement on AgentType.Text
      
            if (AgentType.Text == "Single Agent")
            {
                //Ask the question
                await multiAgent.AskSingletonAgent();
            } else if (AgentType.Text == "Single Agent with Functions")
            {
                //Ask the question
                await multiAgent.AskSingletonAgentWithFunctions();
            }
            else if (AgentType.Text == "Multiagent")
            {
                //Ask the question
                await multiAgent.AskMultiAgent();
            }
            else if (AgentType.Text == "Multiagent with Functions")
            {
                //Ask the question
                await multiAgent.AskMultiAgent();
            }
        }

        public void ClearResponseBox()
        {
            //Clear out the response box
            ResponseBox.Document.Blocks.Clear();
        }

        public void UpdateResponseBox(string sender, string response, Color color)
        {
            //Update mainWindow.ResponseBox to add the sender in bold, a colon, a space, and the response in normal text
            Paragraph paragraph = new Paragraph();
            Bold bold = new Bold(new Run(sender + ": "));

            bold.Foreground = new SolidColorBrush(color);

            paragraph.Inlines.Add(bold);
            Run run = new Run(response);
            paragraph.Inlines.Add(run);
            ResponseBox.Document.Blocks.Add(paragraph);

            Console.WriteLine(sender + ": " + response);
        }
    }
}