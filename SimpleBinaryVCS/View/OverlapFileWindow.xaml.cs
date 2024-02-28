using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.ViewModel;
using System.Windows;

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
            _overlapFileWindow.TaskFinishedEventHandler += TaskFinishedCallBack;
        }

        private void TaskFinishedCallBack()
        {
            this.Close(); 
        }
    }
}