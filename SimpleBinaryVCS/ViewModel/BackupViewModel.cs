using MemoryPack;
using Microsoft.TeamFoundation.MVVM;
using Microsoft.TeamFoundation.VersionControl.Client;
using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static Microsoft.VisualStudio.Shell.ThreadedWaitDialogHelper;

namespace SimpleBinaryVCS.ViewModel
{
    public class BackupViewModel : ViewModelBase
    {
        private ObservableCollection<ProjectData> projectDataList;
        public ObservableCollection<ProjectData> ProjectDataList
        {
            get => projectDataList;
            set
            {
                projectDataList = value;
                OnPropertyChanged("ProjectDataList"); 
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
                UpdaterName = value.updaterName;
                UpdateLog = value.updateLog;
                DiffLog = value.DiffLog;
                OnPropertyChanged("SelectedItem");
            }
        }
        private ICommand? fetchLogs; 
        public ICommand FetchLogs
        {
            get
            {
                if (fetchLogs == null) fetchLogs = new RelayCommand(Fetch, CanFetch);
                return fetchLogs;
            }
        }
        private ICommand? revertBackup;
        public ICommand RevertBackup
        {
            get
            {
                if (revertBackup == null) revertBackup = new RelayCommand(Revert, CanRevert);
                return revertBackup;
            }
        }
        private string? updaterName;
        private string? updateLog;

        public string UpdaterName
        {
            get => updaterName;
            set
            {
                updaterName= value;
                OnPropertyChanged("UpdaterName"); 
            }
        }
        public string UpdateLog
        {
            get => updateLog;
            set
            {
                updateLog= value;
                OnPropertyChanged("UpdateLog"); 
            }
        }
        private ObservableCollection<ProjectFile>? diffLog;
        public ObservableCollection<ProjectFile> DiffLog
        {
            get => diffLog;
            set
            {
                diffLog= value;
                OnPropertyChanged("DiffLog"); 
            }
        }

        public BackupViewModel()
        {
            if (App.VcsManager != null)
            {
                App.VcsManager.updateAction += MakeBackUp;
                App.VcsManager.fetchAction += Fetch; 
            }
            projectDataList = new ObservableCollection<ProjectData>();
        }

        private bool CanFetch(object obj)
        {
            if (App.Current == null || App.VcsManager == null || App.VcsManager.ProjectData.projectPath == null) return false;
            return true;
        }

        private void Fetch(object obj)
        {
            //string?[] backUps = 
            SelectedItem = null; 
            ProjectDataList.Clear();
            if (App.VcsManager.ProjectData.projectPath == null) return;
            string[] mainVersionLog = Directory.GetFiles(App.VcsManager.ProjectData.projectPath, "VersionLog.*", SearchOption.AllDirectories);
            ProjectData? mainProjectData = MemoryPackSerializer.Deserialize<ProjectData>(File.ReadAllBytes(mainVersionLog[0]));
            if (mainProjectData == null) return;
            ProjectDataList.Add(mainProjectData); 
            string[]? backupVersionLogs;
            TryGetBackupLogs(Directory.GetParent(App.VcsManager.ProjectData.projectPath).ToString(), out backupVersionLogs);
            if (backupVersionLogs == null) return;
            foreach (string version in backupVersionLogs)
            {
                ProjectData? data = MemoryPackSerializer.Deserialize<ProjectData>(File.ReadAllBytes(version));
                if (data == null) continue;
                ProjectDataList.Add(data);
            }
        }

        private bool CanRevert(object obj)
        {
            if (SelectedItem == null || App.VcsManager.ProjectData.projectPath == null) return false;
            return true;
        }

        private void Revert(object obj)
        {
            
        }

        private void MakeBackUp(object e)
        {
            //Make new ProjectData for backup 
            ProjectData backUpData = new ProjectData(App.VcsManager.ProjectData); 
            if (App.VcsManager.ProjectData.projectPath == null) return;
            DirectoryInfo? parentDirectory = Directory.GetParent(App.VcsManager.ProjectData.projectPath);
            string backupPath = $"{parentDirectory?.ToString()}\\Backup_{Path.GetFileName(App.VcsManager.ProjectData.projectPath)}\\Backup_{App.VcsManager.ProjectData.updatedVersion}";
            if (!File.Exists(backupPath)) Directory.CreateDirectory(backupPath);
            foreach (ProjectFile file in App.VcsManager.ProjectData.ProjectFiles)
            {
                string newBackupPath = $"{backupPath}\\{Path.GetRelativePath(App.VcsManager.ProjectData.projectPath, file.filePath)}";
                if (!File.Exists(Path.GetDirectoryName(newBackupPath))) Directory.CreateDirectory(Path.GetDirectoryName(newBackupPath));
                File.Copy(file.filePath, newBackupPath, true);
                ProjectFile newFile = new ProjectFile(file);
                newFile.filePath = backupPath;
                newFile.isNew = false;
                backUpData.ProjectFiles.Add(newFile); 
            }

            backUpData.projectPath = backupPath;
            byte[] serializedFile = MemoryPackSerializer.Serialize(App.VcsManager.ProjectData);
            File.WriteAllBytes($"{backUpData.projectPath}\\VersionLog.bin", serializedFile);
            // parentDirectory
            // Take all the file in this directory, copy to the created directory 
            // Take current ProjectData, for all the files in the observablecollection, paste new back up data to the string. 
        }
        private void CloneDirectory(string root, string dest)
        {
            string newFilePath; 

            foreach (var directory in Directory.GetDirectories(root))
            {
                //Get the path of the new directory
                var newDirectory = Path.Combine(dest, Path.GetFileName(directory));
                //Create the directory if it doesn't already exist
                Directory.CreateDirectory(newDirectory);
                //Recursively clone the directory
                CloneDirectory(directory, newDirectory);
            }

            foreach (var file in Directory.GetFiles(root))
            {
                newFilePath = Path.Combine(dest, Path.GetFileName(file)); 

                File.Copy(file, newFilePath);
            }
        }
        private bool TryGetBackupLogs(string projectParentPath, out string[]? VersionLogFiles)
        {
            try
            {
                VersionLogFiles = Directory.GetFiles($"{projectParentPath}\\Backup_{App.VcsManager.ProjectData.projectName}", "VersionLog.*", SearchOption.AllDirectories);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                VersionLogFiles = null;
                return false;
            }
        }
    }
}
