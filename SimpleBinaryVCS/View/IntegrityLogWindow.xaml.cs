using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
