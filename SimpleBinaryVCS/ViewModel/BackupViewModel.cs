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
        private PriorityQueue<ProjectData, ProjectData> importProjects;
        private ObservableCollection<ProjectData> projectDataList;
        public ObservableCollection<ProjectData> BackupProjectDataList
        {
            get => projectDataList;
            set
            {
                projectDataList = value;
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
        private ICommand? checkoutBackup;
        public ICommand CheckoutBackup
        {
            get
            {
                if (checkoutBackup == null) checkoutBackup = new RelayCommand(Revert, CanRevert);
                return checkoutBackup;
            }
        }
        private ICommand? viewFullLog;
        public ICommand ViewFullLog
        {
            get
            {
                if (viewFullLog == null) viewFullLog = new RelayCommand(Revert, CanRevert);
                return viewFullLog;
            }
        }
        private ICommand? addForRevert;
        public ICommand AddForRevert
        {
            get
            {
                if (addForRevert == null) addForRevert = new RelayCommand(RevertAFile, CanRevert);
                return addForRevert;
            }
        }
        private string? updateLog;
        private string? updaterName;
        public string UpdaterName
        {
            get => updaterName ??= (updaterName = "");
            set
            {
                updaterName= value;
                OnPropertyChanged("UpdaterName"); 
            }
        }
        public string UpdateLog
        {
            get => updateLog ??= (updateLog = "");
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

        private VersionControlManager vcsManager;
        private FileManager fileManager; 
        public BackupViewModel()
        {
            importProjects = new PriorityQueue<ProjectData, ProjectData>(); 
            vcsManager = App.VcsManager; 
            fileManager = App.FileManager;
            if (vcsManager != null)
            {
                vcsManager.updateAction += MakeBackUp;
                vcsManager.fetchAction += Fetch;
                projectDataList = vcsManager.projectDataList;
            }
        }

        private bool CanFetch(object obj)
        {
            if (App.Current == null || vcsManager == null || vcsManager.ProjectData.projectPath == null) return false;
            return true;
        }

        private void Fetch(object obj)
        {
            SelectedItem = null;
            vcsManager.NewestProjectData = null; 
            importProjects.Clear(); 
            BackupProjectDataList.Clear();
            if (App.VcsManager.ProjectData.projectPath == null) return;
            string[] mainVersionLog = Directory.GetFiles(App.VcsManager.ProjectData.projectPath, "VersionLog.*", SearchOption.AllDirectories);
            ProjectData? mainProjectData = MemoryPackSerializer.Deserialize<ProjectData>(File.ReadAllBytes(mainVersionLog[0]));
            if (mainProjectData == null) return;
            importProjects.Enqueue(mainProjectData, mainProjectData);
            string[]? backupVersionLogs;
            TryGetBackupLogs(Directory.GetParent(vcsManager.ProjectData.projectPath).ToString(), out backupVersionLogs);
            if (backupVersionLogs == null) return;
            foreach (string version in backupVersionLogs)
            {
                ProjectData? data = MemoryPackSerializer.Deserialize<ProjectData>(File.ReadAllBytes(version));
                if (data == null) continue;
                importProjects.Enqueue(data, data);
            }
            vcsManager.NewestProjectData = importProjects.Dequeue();
            BackupProjectDataList.Add(vcsManager.NewestProjectData);
            int importProjectCount = importProjects.Count;
            for (int i = 0; i < importProjectCount; i++)
            {
                if (importProjects.Peek().updatedVersion == vcsManager.ProjectData.updatedVersion)
                {
                    importProjects.Dequeue();
                    continue;
                }
                BackupProjectDataList.Add(importProjects.Dequeue());
            }
        }

        private bool CanRevert(object obj)
        {
            if (SelectedItem == null ||
                vcsManager.ProjectData.projectPath == null ||
                SelectedItem == vcsManager.ProjectData) return false;
            return true;
        }
        private void RevertAFile(object obj)
        {
            if (obj is ProjectFile file)
            {
                fileManager.RegisterNewfile(file);
            }
        }
        private void Revert(object obj)
        {
            var response = MessageBox.Show($"Do you want to Revert to {SelectedItem.updatedVersion}", "Confirm Updates",
                MessageBoxButtons.YesNo); 
            if (response == DialogResult.OK)
            {

                //1. Backup Current Project Version 
                //1-1. Check for current project's backup
                MakeBackUp(obj);
                //1-2. Delete all the files in the current Directory 
                DeleteAllInDirectory(vcsManager.ProjectData.projectPath);
                //2. Transfer the ProjectData to Current

                //3. Set Selected ProjectData as Current Project Data 
            }
            else
            {
                return;
            }
        }
        private void RevertBackupProject(string projectMainPath)
        {

        }

        private void DeleteAllInDirectory(string directoryPath)
        {
            //1-1. 
            try
            {
                string[] files = Directory.GetFiles(directoryPath);
                foreach (string file in files)
                {
                    File.Delete(file);
                }
                foreach (string subdirectory in Directory.GetDirectories(directoryPath))
                {
                    Directory.Delete(subdirectory, true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n Couldn't Delete files in a Given Directory");
            }
        }

        private void MakeBackUp(object e)
        {
            //Make new ProjectData for backup 
            if (App.VcsManager.ProjectData.projectPath == null) return;
            DirectoryInfo? parentDirectory = Directory.GetParent(vcsManager.ProjectData.projectPath);
            string backupPath = $"{parentDirectory?.ToString()}\\Backup_{Path.GetFileName(vcsManager.ProjectData.projectPath)}\\Backup_{App.VcsManager.ProjectData.updatedVersion}";
            if (!File.Exists(backupPath))
            {
                ProjectData backUpData = new ProjectData(vcsManager.ProjectData);

                Directory.CreateDirectory(backupPath);

                foreach (ProjectFile file in vcsManager.ProjectData.ProjectFiles)
                {
                    try
                    {
                        string newBackupPath = $"{backupPath}\\{file.fileRelPath}";
                        if (!File.Exists(Path.GetDirectoryName(newBackupPath))) Directory.CreateDirectory(Path.GetDirectoryName(newBackupPath));
                        File.Copy(file.fileFullPath(), newBackupPath, true);
                        ProjectFile newFile = new ProjectFile(file);
                        newFile.fileRelPath = backupPath;
                        newFile.isNew = false;
                        backUpData.ProjectFiles.Add(newFile);
                    }
                    catch (Exception Ex)
                    {
                        MessageBox.Show(Ex.Message);
                    }
                }
                backUpData.projectPath = backupPath;
                byte[] serializedFile = MemoryPackSerializer.Serialize(vcsManager.ProjectData);
                File.WriteAllBytes($"{backUpData.projectPath}\\VersionLog.bin", serializedFile);
            }
            else return;
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
                VersionLogFiles = Directory.GetFiles($"{projectParentPath}\\Backup_{vcsManager.ProjectData.projectName}", "VersionLog.*", SearchOption.AllDirectories);
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
