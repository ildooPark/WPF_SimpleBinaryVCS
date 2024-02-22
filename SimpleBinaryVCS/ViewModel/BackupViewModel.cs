﻿using MemoryPack;
using SimpleBinaryVCS;
using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.Utils;
using SimpleBinaryVCS.View;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using WPF = System.Windows;

namespace SimpleBinaryVCS.ViewModel
{
    public class BackupViewModel : ViewModelBase
    {
        /// <summary>
        /// Aligns all the project in order, such that version with the highest revision number 
        /// is listed as the Newest Version. 
        /// </summary>
        private ObservableCollection<ProjectData>? backupProjectDataList;
        public ObservableCollection<ProjectData> BackupProjectDataList
        {
            get => backupProjectDataList ??= new ObservableCollection<ProjectData>();
            set
            {
                backupProjectDataList = value;
                OnPropertyChanged("BackupProjectDataList");
            }
        }
        private ProjectData? selectedItem;
        public ProjectData? SelectedItem
        {
            get { return selectedItem; }
            set
            {
                if (value == null) return;
                selectedItem = value;
                UpdaterName = value.UpdaterName;
                UpdateLog = value.UpdateLog;
                DiffLog = value.ChangedProjectFileObservable;
                OnPropertyChanged("SelectedItem");
            }
        }

        private ICommand? fetchBackup;
        public ICommand FetchBackup
        {
            get => fetchBackup ??= new RelayCommand(Fetch, CanFetch);
        }
        private ICommand? checkoutBackup;
        public ICommand CheckoutBackup
        {
            get => checkoutBackup ??= new RelayCommand(Revert, CanRevert);
        }
        private ICommand? viewFullLog;
        public ICommand ViewFullLog
        {
            get => viewFullLog ??= new RelayCommand(OnViewFullLog, CanRevert);
        }
        
        private string? updateLog;
        private string? updaterName;
        public string UpdaterName
        {
            get => updaterName ??= "";
            set
            {
                updaterName = value;
                OnPropertyChanged("UpdaterName");
            }
        }

        public string UpdateLog
        {
            get => updateLog ??= "";
            set
            {
                updateLog = value;
                OnPropertyChanged("UpdateLog");
            }
        }

        private ObservableCollection<ProjectFile>? diffLog;
        public ObservableCollection<ProjectFile> DiffLog
        {
            get => diffLog ??= new ObservableCollection<ProjectFile>();
            set
            {
                diffLog = value;
                OnPropertyChanged("DiffLog");
            }
        }

        private MetaDataManager metaDataManager;
        private BackupManager backupManager;
        public BackupViewModel()
        {
            metaDataManager = App.MetaDataManager;
            backupManager = App.BackupManager;

            backupManager.FetchAction += ReceiveFetch;
        }

        private bool CanFetch(object obj)
        {
            if (App.Current == null || metaDataManager.ProjectMetaData == null) return false;
            return true;
        }

        private void Fetch(object obj)
        {
            SelectedItem = null;
            //Set up Current Project at Main 
            if (metaDataManager.CurrentProjectPath == null || metaDataManager.ProjectMetaData == null) return;
            backupManager.FetchBackupProjectList(obj);
        }
        private void ReceiveFetch(object backupListObj)
        {
            if (backupListObj is not ObservableCollection<ProjectData> backupList) return;
            BackupProjectDataList = backupList;
        }

        private void OnViewFullLog(object obj)
        {
            if (SelectedItem == null)
            {
                MessageBox.Show("Couldn't Get the Log, Selected Item is Null");
                return;
            }
            var mainWindow = obj as WPF.Window;
            IntegrityLogWindow logWindow = new IntegrityLogWindow(SelectedItem);
            logWindow.Owner = mainWindow;
            logWindow.WindowStartupLocation = WPF.WindowStartupLocation.CenterOwner;
            logWindow.Show();
        }
        private bool CanRevert(object obj)
        {
            if (SelectedItem == null || metaDataManager.MainProjectData == null) return false;
            return true;
        }

        private void Revert(object obj)
        {
            if (selectedItem == null)
            {
                MessageBox.Show("BUVM 164: Selected BackupVersion is null");
                return;
            }
            var response = MessageBox.Show($"Do you want to Revert to {selectedItem.UpdatedVersion}", "Confirm Updates",
                MessageBoxButtons.YesNo); 
            if (response == DialogResult.Yes)
            {
                backupManager.RevertProject(selectedItem);
                return;
            }
            else
            {
                return;
            }
        }
    }
}