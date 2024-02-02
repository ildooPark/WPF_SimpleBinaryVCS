using Microsoft.TeamFoundation.MVVM;
using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SimpleBinaryVCS.ViewModel
{
    public class BackupViewModel : ViewModelBase
    {
        private ObservableCollection<ProjectData> projectDataList;
        public ObservableCollection<ProjectData> ProjectDataList => projectDataList;

        private ProjectData selectedItem; 
        public ProjectData SelectedItem
        {
            get { return selectedItem; }
            set
            {
                selectedItem = value;
                OnPropertyChanged("SelectedItem");
            }
        }
        private ICommand getBackupData; 
        public ICommand GetBackupData
        {
            get
            {
                if (getBackupData == null) getBackupData = new RelayCommand(RetrieveBackUp, CanRetrieveBackUp);
                return getBackupData;
            }
        }
        private ICommand makebackup;
        public ICommand MakeBackup
        {
            get
            {
                if (makebackup == null) makebackup = new RelayCommand(LoadBackUps, CanLoadBackUps);
                return makebackup;
            }
        }
        //private BackupManager backupManager;
        //public BackupManager BackupManager => backupManager;

        public BackupViewModel()
        {
            projectDataList = new ObservableCollection<ProjectData>();
        }

        private bool CanRetrieveBackUp(object obj)
        {
            if (App.Current == null || App.VcsManager == null || App.VcsManager.ProjectPath == null) return false;
            return true;
        }

        private void RetrieveBackUp(object obj)
        {

        }
        private bool CanLoadBackUps(object obj)
        {
            if (App.VcsManager.ProjectPath == null) return false;
            return true;
        }

        private void LoadBackUps(object obj)
        {
            //string?[] backUps = 
        }

        private void TryGetAllLogs(string directoryPath, out string[]? VersionLogFiles)
        {
            try
            {
                VersionLogFiles = Directory.GetFiles(directoryPath, "BinaryVersionLog.*", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                VersionLogFiles = null;
            }
        }
    }
}
