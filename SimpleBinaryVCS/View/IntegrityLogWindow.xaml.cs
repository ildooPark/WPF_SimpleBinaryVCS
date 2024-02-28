using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.ViewModel;
using System.Collections.ObjectModel;
using System.Windows;

namespace SimpleBinaryVCS.View
{
    /// <summary>
    /// Interaction logic for IntegrityLogWindow.xaml
    /// </summary>
    public partial class IntegrityLogWindow : Window
    {
        public IntegrityLogWindow(string versionLog, ObservableCollection<ProjectFile> fileList)
        {
            InitializeComponent();
            VersionCheckViewModel versionCheckViewModel = new VersionCheckViewModel(versionLog, fileList);
            this.DataContext = versionCheckViewModel;
        }

        public IntegrityLogWindow(ProjectData projectData)
        {
            InitializeComponent();
            VersionCheckViewModel versionCheckViewModel = new VersionCheckViewModel(projectData);
            this.DataContext = versionCheckViewModel;
        }
    }
}
