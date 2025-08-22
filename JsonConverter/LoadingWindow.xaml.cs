using System.Windows;

namespace JsonConverter
{
    public partial class LoadingWindow : Window
    {
        public LoadingWindow()
        {
            InitializeComponent();
            this.Owner = Application.Current.MainWindow;
        }
    }
}
