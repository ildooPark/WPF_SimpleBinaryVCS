﻿using SimpleBinaryVCS.DataComponent;
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
        private ObservableCollection<ProjectFile>? _changedFileList; 
        public ObservableCollection<ProjectFile> ChangedFileList 
        {
            get => _changedFileList ??= new ObservableCollection<ProjectFile>();
            set
            {
                _changedFileList = value;
                OnPropertyChanged("ChangedFileList");
            }
        }
        private ProjectFile? _srcProjectData;
        public ProjectFile? SrcProjectData
        {
            get => _srcProjectData ??= null;
            set
            {
                _srcProjectData = value;
                OnPropertyChanged("SelectedItem");
            }
        }

        private ProjectFile? _selectedItem; 
        public ProjectFile? SelectedItem
        {
            get => _selectedItem ??= null; 
            set
            {
                _selectedItem = value;
                OnPropertyChanged("SelectedItem"); 
            }
        }

        private ICommand? clearNewfiles;
        public ICommand ClearNewfiles => clearNewfiles ??= new RelayCommand(ClearFiles, CanClearFiles);

        private ICommand? refreshDeployFileList;
        public ICommand RefreshDeployFileList => refreshDeployFileList ??= new RelayCommand(RefreshFilesList);


        private ICommand? revertChange;
        public ICommand RevertChange => revertChange ??= new RelayCommand(RevertIntegrityCheckFile);

        private ICommand? checkProjectIntegrity; 
        public ICommand CheckProjectIntegrity => checkProjectIntegrity ??= new RelayCommand(MainProjectIntegrityTest, CanRunIntegrityTest);

        private ICommand? stageChanges;
        public ICommand StageChanges => stageChanges ??= new RelayCommand(StageNewChanges, CanStageChanges);

        private ICommand? addForRestore;
        public ICommand AddForRestore => addForRestore ??= new RelayCommand(RestoreFile, CanRestoreFile);

        private ICommand? getDeployedProjectInfo;
        public ICommand? GetDeployedProjectInfo => getDeployedProjectInfo ??= new RelayCommand(OpenDeployedProjectInfo, CanOpenDeployedProjectInfo);
        
        private ICommand? getDeploySrcDir;
        public ICommand GetDeploySrcDir => getDeploySrcDir ??= new RelayCommand(SetDeploySrcDirectory, CanSetDeployDir);
        
        private MetaDataManager _metaDataManager;
        private MetaDataState _metaDataState = MetaDataState.Idle;
        private ProjectData? _deployedProjectData;
        string? _deploySrcPath;
        public FileTrackViewModel()
        {
            this._metaDataManager = App.MetaDataManager;
            this._metaDataManager.OverlappedFileSortEventHandler += OverlapFileSortCallBack; 
            this._metaDataManager.SrcProjectLoadedEventHandler += SrcProjectDataCallBack;
            this._metaDataManager.PreStagedChangesEventHandler += PreStagedChangesCallBack;
            this._metaDataManager.IntegrityCheckCompleteEventHandler += ProjectIntegrityCheckCallBack;
            this._metaDataManager.FileChangesEventHandler += FileChangeListUpdateCallBack;
        }

        private bool CanSetDeployDir(object obj)
        {
            return true;
        }
        private void SetDeploySrcDirectory(object obj)
        {
            try
            {
                var openUpdateDir = new WinForms.FolderBrowserDialog();
                if (openUpdateDir.ShowDialog() == DialogResult.OK)
                {
                    _deployedProjectData = null;
                    _deploySrcPath = openUpdateDir.SelectedPath;
                    _metaDataManager.RequestSrcDataRetrieval(_deploySrcPath);
                }
                else
                {
                    _deploySrcPath = null; 
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
        
        private bool CanStageChanges(object obj) 
        {
            if (_metaDataState != MetaDataState.Idle) return false; 
            return ChangedFileList.Count != 0; 
        }
        private void StageNewChanges(object obj)
        {
            _metaDataManager.RequestStageChanges();
        }

        private bool CanOpenDeployedProjectInfo(object obj)
        {
            return _deployedProjectData != null;
        }
        public void OpenDeployedProjectInfo(object obj)
        {
            var mainWindow = obj as WPF.Window;
            IntegrityLogWindow logWindow = new IntegrityLogWindow(_deployedProjectData);
            logWindow.Owner = mainWindow;
            logWindow.WindowStartupLocation = WPF.WindowStartupLocation.CenterOwner;
            logWindow.Show();
        }

        private bool CanClearFiles(object obj) { return ChangedFileList.Count != 0; }
        private void ClearFiles(object obj)
        {
            _metaDataManager.RequestClearStagedFiles();
        }
        
        private bool CanRestoreFile(object? obj)
        {
            if (_metaDataState != MetaDataState.Idle) return false;
            if (obj is ProjectFile projFile &&
                (projFile.DataState == DataState.Deleted ||
                !projFile.IsDstFile)) return true; 
            else return false;
        }
        private void RestoreFile(object? obj)
        {
            if (obj is ProjectFile file)
            {
                _metaDataManager.RequestFileRestore(file, DataState.Restored);
            }
        }
        
        private void RefreshFilesList(object? obj)
        {
            if (_deploySrcPath == null)
            {
                MessageBox.Show("Please Set Src Deploy Path");
            }
            _metaDataManager.RequestClearStagedFiles();
            _metaDataManager.RequestSrcDataRetrieval(_deploySrcPath);
        }

        private void RevertIntegrityCheckFile(object? obj)
        {
            if (_selectedItem is ProjectFile file)
            {
                if ((file.DataState & DataState.IntegrityChecked) == 0)
                {
                    MessageBox.Show("Only Applicable for Integrity Check Failed Files");
                    return;
                }
                _metaDataManager.RequestRevertChange(file);
            }
        }

        private bool CanRunIntegrityTest(object Sender)
        {
            return _metaDataState == MetaDataState.Idle; 
        }
        private void MainProjectIntegrityTest(object sender)
        {
            Task.Run(() => _metaDataManager.RequestProjectIntegrityTest());
        }

        #region Receive Callback From Model 
        private void StageRequestCallBack(ObservableCollection<ProjectFile> stagedChanges)
        {
            _changedFileList = stagedChanges;
        }
        private void OverlapFileSortCallBack(List<ChangedFile> overlappedFileObj)
        {
            OverlapFileWindow overlapFileWindow = new OverlapFileWindow(overlappedFileObj);
            overlapFileWindow.Owner = App.Current.MainWindow;
            overlapFileWindow.WindowStartupLocation = WPF.WindowStartupLocation.CenterOwner;
            overlapFileWindow.Show();
        }

        private void PreStagedChangesCallBack(object changedFileList)
        {
            if (changedFileList is ObservableCollection<ProjectFile> projectFileList)
            {
                ChangedFileList = projectFileList;
            }
        }

        private void FileChangeListUpdateCallBack(ObservableCollection<ProjectFile> changedFileList)
        {
            this.ChangedFileList = changedFileList;
        }

        private void ProjectIntegrityCheckCallBack(string changeLog, ObservableCollection<ProjectFile> changedFileList)
        {
            if (changedFileList == null) { MessageBox.Show("Model Binding Issue: ChangedList is Empty"); return; }

            App.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = App.Current.MainWindow;
                IntegrityLogWindow logWindow = new IntegrityLogWindow(changeLog, changedFileList);
                logWindow.Owner = mainWindow;
                logWindow.WindowStartupLocation = WPF.WindowStartupLocation.CenterOwner;
                logWindow.Show();
            });
        }
        private void MetaDataStateChangeCallBack(MetaDataState state)
        {
            _metaDataState = state;
        }
        private void SrcProjectDataCallBack(object srcProjectDataObj)
        {
            if (srcProjectDataObj is not ProjectData srcProjectData) return;
            this._deployedProjectData = srcProjectData;
        }
        #endregion
    }
}
#region Deprecated 


#endregion