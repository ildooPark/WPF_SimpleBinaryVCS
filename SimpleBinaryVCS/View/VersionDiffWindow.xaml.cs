using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.ViewModel;
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

namespace SimpleBinaryVCS.View
{
    /// <summary>
    /// Interaction logic for VersionDiffWindow.xaml
    /// </summary>
    public partial class VersionDiffWindow : Window
    {
        
        public VersionDiffWindow(ProjectData srcProject, ProjectData dstProject, List<ChangedFile> diff)
        {
            InitializeComponent();
            VersionDiffViewModel VersionDiffVM = new VersionDiffViewModel(srcProject, dstProject, diff);
            this.DataContext = VersionDiffVM;
        }
    }
}
