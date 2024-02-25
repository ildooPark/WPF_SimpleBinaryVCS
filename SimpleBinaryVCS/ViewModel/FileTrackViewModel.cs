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
        private ObservableCollection<ProjectFile>? changedFileList; 
        public ObservableCollection<ProjectFile> ChangedFileList 
        {
            get => changedFileList ??= new ObservableCollection<ProjectFile>();
            set
            {
                changedFileList = value;
                OnPropertyChanged("ChangedFileList");
            }
        }

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
        public ICommand ClearNewfiles => clearNewfiles ??= new RelayCommand(ClearFiles, CanClearFiles);

        private ICommand? checkProjectIntegrity; 
        public ICommand CheckProjectIntegrity => checkProjectIntegrity ??= new RelayCommand(MainProjectIntegrityTest, CanRunIntegrityTest);

        private ICommand? stageChanges;
        public ICommand StageChanges => stageChanges ??= new RelayCommand(StageNewChanges, CanStageChanges);

        private ICommand? addForRestore;
        public ICommand AddForRestore => addForRestore ??= new RelayCommand(RestoreFile, CanRestoreFile);

        private ICommand? getDeployedProjectInfo;
        public ICommand? GetDeployedProjectInfo => getDeployedProjectInfo ??= new RelayCommand(OpenDeployedProjectInfo, CanOpenDeployedProjectInfo);

        private MetaDataManager metaDataManager;
        private ProjectData? deployedProjectData;

        public FileTrackViewModel()
        {
            this.metaDataManager = App.MetaDataManager;
            this.metaDataManager.SrcProjectLoadedEventHandler += SrcProjectDataCallBack;
            this.metaDataManager.ProjectIntegrityCheckEventHandler += ProjectIntegrityCheckCallBack;
            this.metaDataManager.FileChangesEventHandler += FileChangeListUpdateCallBack;
        }

        private bool CanStageChanges(object obj) { return ChangedFileList.Count != 0; }
        private void StageNewChanges(object obj)
        {
            metaDataManager.RequestStageChanges();
        }

        private bool CanOpenDeployedProjectInfo(object obj)
        {
            return deployedProjectData != null;
        }

        public void OpenDeployedProjectInfo(object obj)
        {
            var mainWindow = obj as WPF.Window;
            IntegrityLogWindow logWindow = new IntegrityLogWindow(deployedProjectData);
            logWindow.Owner = mainWindow;
            logWindow.WindowStartupLocation = WPF.WindowStartupLocation.CenterOwner;
            logWindow.Show();
        }

        private bool CanClearFiles(object obj) { return ChangedFileList.Count != 0; }
        private void ClearFiles(object obj)
        {
            List<ProjectFile> clearList = new List<ProjectFile>();
            foreach (ProjectFile file in ChangedFileList)
            {
                if ((file.DataState & DataState.IntegrityChecked) == 0)
                {
                    clearList.Add(file);
                }
            }
            foreach (ProjectFile file in clearList)
            {
                ChangedFileList.Remove(file);
            }
        }

        
        private bool CanRestoreFile(object? obj)
        {
            if (obj is ProjectFile projFile &&
                !projFile.IsDstFile) return true; 
            else return false;
        }
        private void RestoreFile(object? obj)
        {
            if (obj is ProjectFile file)
            {
                metaDataManager.RequestFileRestore(file, DataState.Restored);
            }
        }

        private bool CanRunIntegrityTest(object Sender)
        {
            return true; 
        }
        private void MainProjectIntegrityTest(object sender)
        {
            metaDataManager.RequestProjectIntegrityTest(sender);
        }

        #region Receive Callback From Model 

        private void PreStagedFileOverlapCallBack(object overlappedFileObj)
        {
            if (overlappedFileObj is not ProjectFile file) return;
            MessageBox.Show($"PreStaged file {file.DataName} Already Exists");
            // User should be able to choose which to update.
            // Pop List ComboBox
        }

        private void FileChangeListUpdateCallBack(object changedFileListObj)
        {
            if (changedFileListObj is ObservableCollection<ProjectFile> changedFileList)
                this.ChangedFileList = changedFileList;
        }

        private void ProjectIntegrityCheckCallBack(object sender, string changeLog, ObservableCollection<ProjectFile> changedFileList)
        {
            if (changedFileList == null) { MessageBox.Show("Model Binding Issue: ChangedList is Empty"); return; }
            
            var mainWindow = sender as WPF.Window;
            IntegrityLogWindow logWindow = new IntegrityLogWindow(changeLog, changedFileList);
            logWindow.Owner = mainWindow;
            logWindow.WindowStartupLocation = WPF.WindowStartupLocation.CenterOwner;
            logWindow.Show();
        }

        private void SrcProjectDataCallBack(object srcProjectDataObj)
        {
            if (srcProjectDataObj is not ProjectData srcProjectData) return;
            this.deployedProjectData = srcProjectData;
        }
        #endregion
    }
}
#region Deprecated 
//private ICommand? getDeploySrcDir;
//public ICommand GetDeploySrcDir => getDeploySrcDir ??= new RelayCommand(SetDeploySrcDirectory, CanSetDeployDir);
//private bool CanSetDeployDir(object obj)
//{
//    return true;
//}
//private void SetDeploySrcDirectory(object obj)
//{
//    try
//    {
//        string? updateDirPath;
//        var openUpdateDir = new WinForms.FolderBrowserDialog();
//        if (openUpdateDir.ShowDialog() == DialogResult.OK)
//        {
//            deployedProjectData = null; 
//            updateDirPath = openUpdateDir.SelectedPath;
//            fileManager.RetrieveDataSrc(updateDirPath);
//        }
//        else
//        {
//            openUpdateDir.Dispose();
//            return;
//        }
//        openUpdateDir.Dispose();
//    }
//    catch (Exception ex)
//    {
//        MessageBox.Show(ex.Message);
//    }
//}

#endregion