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

namespace Ringkes
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();

            LoadLogo();
        }

        private void LoadLogo()
        {
            BitmapImage bitmap =
                new BitmapImage(
                    new Uri(
                        "Assets/logo.png",
                        UriKind.Relative));

            LogoImage.Source = bitmap;
        }
    }
}