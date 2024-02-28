using SimpleBinaryVCS.ViewModel;
using System.Windows;

namespace SimpleBinaryVCS.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainViewModel mainVM = new MainViewModel();
            this.DataContext = mainVM;
        }
    }
}