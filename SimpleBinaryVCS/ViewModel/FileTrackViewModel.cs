using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.View;
using System.Collections.ObjectModel;
using System.Windows.Input;
using WPF = System.Windows;
using SimpleBinaryVCS.Utils;
namespace SimpleBinaryVCS.ViewModel
{
    public enum VMState
    {
        Idle,
        Calculating
    }
    public class FileTrackViewModel : ViewModelBase
    {
        public ObservableCollection<ProjectFile> ChangedFileList { get; set; }

        private ProjectFile? selectedItem; 
        public ProjectFile? SelectedItem
        {
            get => selectedItem ??= null; 
            set
            {
                selectedItem = value;
                OnPropertyChanged("SelectedItem"); 
            }
        }

        private ICommand? clearNewfiles;
        public ICommand ClearNewfiles
        {
            get => clearNewfiles ??= new RelayCommand(ClearFiles, CanClearFiles);
        }

        private ICommand? checkProjectIntegrity; 
        public ICommand CheckProjectIntegrity
        {
            get => checkProjectIntegrity ??= new RelayCommand(RunProjectVersionIntegrity, CanRunIntegrityTest);
        }

        private int detectedFileChange;
        public int DetectedFileChange
        {
            get { return detectedFileChange; }
            set
            {
                detectedFileChange = value;
                OnPropertyChanged("DetectedFileChange");
            }
        }

        private FileManager fileManager;
        private VMState currentState; 
        public FileTrackViewModel()
        {
            this.fileManager = App.FileManager;
            this.ChangedFileList = fileManager.ChangedFileList;
            this.fileManager.newLocalFileChange += OnNewLocalFileChange;
            this.fileManager.IntegrityCheckFinished += OpenIntegrityLogWindow;
            this.currentState = VMState.Idle; 
        }

        private void OnNewLocalFileChange(int numFile)
        {
            DetectedFileChange = numFile;
        }

        private bool CanClearFiles(object obj) { return ChangedFileList.Count != 0; }

        private void ClearFiles(object obj)
        {
            List<ProjectFile> clearList = new List<ProjectFile>();
            foreach (ProjectFile file in ChangedFileList)
            {
                if ((file.dataState & DataChangedState.IntegrityChecked) == 0)
                {
                    clearList.Add(file);
                }
            }
            foreach (ProjectFile file in clearList)
            {
                ChangedFileList.Remove(file);
            }
            
        }

        private bool CanRunIntegrityTest(object parameter)
        {
            return currentState == VMState.Idle;
        }

        private void RunProjectVersionIntegrity(object parameter)
        {
            currentState = VMState.Calculating;
            fileManager.PerformIntegrityCheck(parameter);
            currentState = VMState.Idle;
        }

        private void OpenIntegrityLogWindow(object sender, string changeLog, ObservableCollection<ProjectFile> changedFileList)
        {
            if (changedFileList == null) { MessageBox.Show("Model Binding Issue: ChangedList is Empty"); return; }
            var mainWindow = sender as WPF.Window;
            IntegrityLogWindow logWindow = new IntegrityLogWindow(changeLog, changedFileList);
            logWindow.Owner = mainWindow;
            logWindow.WindowStartupLocation = WPF.WindowStartupLocation.CenterOwner;
            logWindow.Show();
        }
    }
}