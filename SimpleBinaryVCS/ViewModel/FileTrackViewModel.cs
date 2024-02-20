using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Interfaces;
using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.Utils;
using SimpleBinaryVCS.View;
using System.Collections.ObjectModel;
using System.Windows.Input;
using WinForms = System.Windows.Forms;
using WPF = System.Windows;
namespace SimpleBinaryVCS.ViewModel
{
    public enum VMState
    {
        Idle,
        Calculating
    }
    public class FileTrackViewModel : ViewModelBase
    {
        public ObservableCollection<IProjectData> ChangedFileList { get; set; } = new ObservableCollection<IProjectData>();

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
        private ICommand? getDeploySrcDir;
        public ICommand GetDeploySrcDir
        {
            get
            {
                if (getDeploySrcDir == null) getDeploySrcDir = new RelayCommand(SetDeploySrcDirectory, CanSetDeployDir);
                return getDeploySrcDir;
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
        private ICommand? stageChanges;
        public ICommand StageChanges
        {
            get => stageChanges ??= new RelayCommand(RunProjectVersionIntegrity, CanRunIntegrityTest);
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
            this.fileManager.newLocalFileChange += OnNewLocalFileChange;
            this.fileManager.IntegrityCheckFinished += OpenIntegrityLogWindow;
            this.currentState = VMState.Idle; 
        }

        private void OnNewLocalFileChange(int numFile)
        {
            DetectedFileChange = numFile;
        }
        private bool CanStageChanges(object obj) { return ChangedFileList.Count != 0; }
        private void StageNewChanges(object obj)
        {
            
        }

        private bool CanClearFiles(object obj) { return ChangedFileList.Count != 0; }

        private void ClearFiles(object obj)
        {
            List<ProjectFile> clearList = new List<ProjectFile>();
            foreach (ProjectFile file in ChangedFileList)
            {
                if ((file.DataState & DataChangedState.IntegrityChecked) == 0)
                {
                    clearList.Add(file);
                }
            }
            foreach (ProjectFile file in clearList)
            {
                ChangedFileList.Remove(file);
            }
            
        }

        private bool CanSetDeployDir(object obj)
        {
            return true;
        }

        private void SetDeploySrcDirectory(object obj)
        {
            try
            {
                string? updateDirPath;
                var openUpdateDir = new WinForms.FolderBrowserDialog();
                if (openUpdateDir.ShowDialog() == DialogResult.OK)
                {
                    //Delete Project Repo and all the related subject. 
                    updateDirPath = openUpdateDir.SelectedPath;
                    fileManager.RegisterNewData(updateDirPath);
                }
                else
                {
                    openUpdateDir.Dispose();
                    return;
                }
                openUpdateDir.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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

        private void GetFileChanges(ObservableCollection<ProjectFile> changedFileList)
        {
            foreach (ProjectFile projectFile in changedFileList)
            {
                IProjectData data = projectFile as IProjectData;
                this.ChangedFileList.Add(data);
            }
        }

        private void GetFileChanges(ObservableCollection<TrackedData> changedFileList)
        {
            foreach (TrackedData trackedData in changedFileList)
            {
                IProjectData data = trackedData as IProjectData;
                this.ChangedFileList.Add(data);
            }
        }
    }
}