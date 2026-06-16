using System.IO;
using System.Windows;

namespace Ringkes.Views
{
    public partial class MergeProgressWindow : Window
    {
        public MergeProgressWindow(
            string outputFile)
        {
            InitializeComponent();

            OutputText.Text =
                Path.GetFileName(outputFile);
        }
    }
}