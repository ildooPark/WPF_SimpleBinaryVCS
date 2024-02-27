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
    /// Interaction logic for OverlapFileWindow.xaml
    /// </summary>
    public partial class OverlapFileWindow : Window
    {
        public OverlapFileWindow(List<ChangedFile> overlapFiles)
        {
            InitializeComponent();
            OverlapFileViewModel _overlapFileWindow = new OverlapFileViewModel(overlapFiles);
            this.DataContext = _overlapFileWindow;
        }
    }
}