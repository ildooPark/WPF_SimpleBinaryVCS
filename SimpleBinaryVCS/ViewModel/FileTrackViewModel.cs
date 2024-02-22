﻿using SimpleBinaryVCS.DataComponent;
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
        public bool DeployedSrcHasLog { get; set; }
        private ProjectData? deployedProjectData; 

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
            get => checkProjectIntegrity ??= new RelayCommand(MainProjectIntegrityTest, CanRunIntegrityTest);
        }

        private ICommand? stageChanges;
        public ICommand StageChanges
        {
            get => stageChanges ??= new RelayCommand(MainProjectIntegrityTest, CanRunIntegrityTest);
        }

        private ICommand? addForRestore;
        public ICommand AddForRestore
        {
            get => addForRestore ??= new RelayCommand(RestoreFile, CanRestoreFile);
        }

        private ICommand? getDeployedProjectInfo; 
        public ICommand? GetDeployedProjectInfo
        {
            get => getDeployedProjectInfo ??= new RelayCommand(OpenDeployedProjectInfo, CanOpenDeployedProjectInfo)
        }

        private FileManager fileManager;
        private UpdateManager updateManager; 
        private VMState currentState; 
        public FileTrackViewModel()
        {
            this.fileManager = App.FileManager;
            this.updateManager = App.UpdateManager;
            this.fileManager.DataPreStagedEventHandler += PreStagedCallBack;
            this.fileManager.IntegrityCheckEventHandler += IntegrityCheckCallBack;
            
            this.currentState = VMState.Idle; 
        }

        private bool CanStageChanges(object obj) { return ChangedFileList.Count != 0; }
        private void StageNewChanges(object obj)
        {
            if (deployedProjectData != null)
            {

            }
            else
            {

            }
        }
        private bool CanOpenDeployedProjectInfo(object obj)
        {
            return true;
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
                    updateDirPath = openUpdateDir.SelectedPath;
                    fileManager.RetrieveDataSrc(updateDirPath);
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
        
        private bool CanRestoreFile(object? obj)
        {
            if (obj is ProjectFile projFile &&
                projFile != null &&
                (projFile.DataState & DataChangedState.Added) != 0) return true; 
            else return false;
        }
        private void RestoreFile(object? obj)
        {
            if (obj is ProjectFile file)
            {
                fileManager.RegisterNewfile(file, DataChangedState.Restored);
            }
        }

        private bool CanRunIntegrityTest(object Sender)
        {
            return currentState == VMState.Idle;
        }
        private void MainProjectIntegrityTest(object sender)
        {
            currentState = VMState.Calculating;
            fileManager.PerformIntegrityCheck(sender);
        }

        #region Receive Callback From Model 
        private void PreStagedCallBack(object changedFileList)
        {
            if (changedFileList is ObservableCollection<ProjectFile> projectFileList)
            {
                ChangedFileList.Clear();
                ChangedFileList = projectFileList;
            }
        }

        private void IntegrityCheckCallBack(object sender, string changeLog, ObservableCollection<ProjectFile> changedFileList)
        {
            currentState = VMState.Idle;
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