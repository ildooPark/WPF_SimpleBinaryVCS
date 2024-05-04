using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.ViewModel;
using System.Windows;

namespace DeployAssistant.View
{
    /// <summary>
    /// Interaction logic for SrcProjectdataWindow.xaml
    /// </summary>
    public partial class SrcProjectdataWindow : Window
    {
        public SrcProjectdataWindow(ProjectData srcProject)
        {
            InitializeComponent();
            VersionCheckViewModel versionCheckViewModel = new VersionCheckViewModel(srcProject);
            this.DataContext = versionCheckViewModel;
        }

        private void FileFilterKeyword_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }
    }
}
