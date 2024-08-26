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

namespace SQLMultiagent
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const string DefaultQuestion = "What was my best selling product?";

        private SQLMultiAgent multiAgent;
        public MainWindow()
        {
            multiAgent = new SQLMultiAgent();
            multiAgent.AgentResponded += SQLMultiAgent_AgentResponded;
            InitializeComponent();
        }
        private async void AskButton_Click(object sender, RoutedEventArgs e)
        {
            
            await multiAgent.askSingletonAgent(false);
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