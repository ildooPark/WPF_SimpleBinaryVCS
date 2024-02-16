using MemoryPack;
using Microsoft.TeamFoundation.MVVM;
using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Model;
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
        private PriorityQueue<ProjectData, ProjectData> importProjects;
        private ObservableCollection<ProjectData> backupProjectDataList;
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
                UpdaterName = value.updaterName;
                UpdateLog = value.updateLog;
                DiffLog = value.DiffLog;
                OnPropertyChanged("SelectedItem");
            }
        }
        private ICommand? fetchLogs; 
        public ICommand FetchLogs
        {
            get => fetchLogs ??= new RelayCommand(Fetch, CanFetch);
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
        private ICommand? addForRestore;
        public ICommand AddForRestore
        {
            get => addForRestore ??= new RelayCommand(RestoreAFile, CanRevert);
        }
        private string? updateLog;
        private string? updaterName;
        public string UpdaterName
        {
            get => updaterName ??= "";
            set
            {
                updaterName= value;
                OnPropertyChanged("UpdaterName"); 
            }
        }
        public string UpdateLog
        {
            get => updateLog ??= "";
            set
            {
                updateLog= value;
                OnPropertyChanged("UpdateLog"); 
            }
        }
        private ObservableCollection<ProjectFile>? diffLog;
        public ObservableCollection<ProjectFile> DiffLog
        {
            get => diffLog ??= new ObservableCollection<ProjectFile>();
            set
            {
                diffLog= value;
                OnPropertyChanged("DiffLog"); 
            }
        }

        private VersionControlManager vcsManager;
        private BackupManager backupManager; 
        private FileManager fileManager; 
        public BackupViewModel()
        {
            importProjects = new PriorityQueue<ProjectData, ProjectData>(); 
            vcsManager = App.VcsManager; 
            fileManager = App.FileManager;
            backupManager = App.BackupManager;
            if (vcsManager != null)
            {
                vcsManager.updateAction += MakeBackUp;
                vcsManager.fetchAction += Fetch;
                backupProjectDataList = vcsManager.projectDataList;
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
            //Set up Current Project at Main 
            if (vcsManager.mainProjectPath == null) return;
            DirectoryInfo parentPath = Directory.GetParent(vcsManager.mainProjectPath);

            string[] mainVersionLog = Directory.GetFiles(vcsManager.mainProjectPath, "VersionLog.*", SearchOption.AllDirectories);
            ProjectData? mainProjectData = MemoryPackSerializer.Deserialize<ProjectData>(File.ReadAllBytes(mainVersionLog[0]));
            if (mainProjectData == null) return;
            vcsManager.ProjectData = mainProjectData;
            string[]? backupVersionLogs;
            if (parentPath == null) return;
            TryGetBackupLogs(parentPath.ToString(), out backupVersionLogs);
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
        private void RestoreAFile(object obj)
        {
            if (obj is ProjectFile file)
            {
                fileManager.RegisterNewfile(file, FileChangedState.Restored);
            }
        }
        private void Revert(object obj)
        {
            var response = MessageBox.Show($"Do you want to Revert to {SelectedItem.updatedVersion}", "Confirm Updates",
                MessageBoxButtons.YesNo); 
            if (response == DialogResult.Yes)
            {
                //1. Backup Current Project Version 
                //1-1. Check for current project's backup
                MakeBackUp(obj);
                //1-2. Delete all the files in the current Directory 
                DeleteAllInDirectory(vcsManager.mainProjectPath ?? "");
                //2. Transfer the ProjectData to Current
                RevertBackupToMain(SelectedItem);
                //3. Set Selected ProjectData as Current Project Data 
                Fetch(obj);
            }
            else
            {
                return;
            }
        }

        private void DeleteAllInDirectory(string directoryPath)
        {
            try
            {
                Directory.Delete(directoryPath, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n Couldn't Delete files in a Given Directory");
            }
        }

        private void RevertBackupToMain(ProjectData revertData)
        {

            string newSrcPath = vcsManager.mainProjectPath;
            if (string.IsNullOrEmpty(newSrcPath))
            {
                return;
            }
            try
            {
                if (!File.Exists(newSrcPath))
                {
                    ProjectData revertedData = new ProjectData(revertData, true);

                    Directory.CreateDirectory(newSrcPath);

                    foreach (ProjectFile file in revertData.ProjectFiles)
                    {
                        try
                        {
                            string newFilePath = $"{newSrcPath}\\{file.fileRelPath}";
                            if (!File.Exists(Path.GetDirectoryName(newFilePath))) Directory.CreateDirectory(Path.GetDirectoryName(newFilePath));
                            File.Copy(file.fileFullPath(), newFilePath, true);
                            ProjectFile newFile = new ProjectFile(file);
                            newFile.fileSrcPath = newSrcPath;
                            revertedData.ProjectFiles.Add(newFile);
                            
                        }
                        catch (Exception Ex)
                        {
                            MessageBox.Show($"Line BU 256: {Ex.Message}");
                        }
                    }
                    revertedData.projectPath = newSrcPath;
                    byte[] serializedFile = MemoryPackSerializer.Serialize(revertedData);
                    File.WriteAllBytes($"{revertedData.projectPath}\\VersionLog.bin", serializedFile);
                }
                else return;
            }
            catch (Exception Ex)
            {
                MessageBox.Show($"BUVM RevertBackupToMain {Ex.Message}");
            }
            
        }

        /// <summary>
        /// Backup is generated prior to and after update event. 
        /// </summary>
        /// <param name="e"></param>
        private void MakeBackUp(object e)
        {
            //Make new ProjectData for backup 
            if (App.VcsManager.ProjectData.projectPath == null) return;
            DirectoryInfo? parentDirectory = Directory.GetParent(vcsManager.ProjectData.projectPath);
            if (parentDirectory != null) return;
            string backupSrcPath = $"{parentDirectory?.ToString()}\\Backup_{Path.GetFileName(vcsManager.ProjectData.projectPath)}\\Backup_{App.VcsManager.ProjectData.updatedVersion}";
            if (!File.Exists(backupSrcPath))
            {
                ProjectData backUpData = new ProjectData(vcsManager.ProjectData);

                Directory.CreateDirectory(backupSrcPath);

                foreach (ProjectFile file in vcsManager.ProjectData.ProjectFiles)
                {
                    try
                    {
                        string newBackupFullPath = $"{backupSrcPath}\\{file.fileRelPath}";
                        if (!File.Exists(Path.GetDirectoryName(newBackupFullPath))) Directory.CreateDirectory(Path.GetDirectoryName(newBackupFullPath));
                        File.Copy(file.fileFullPath(), newBackupFullPath, true);
                        ProjectFile newFile = new ProjectFile(file);
                        newFile.fileSrcPath = backupSrcPath;
                        newFile.isNew = false;
                        backUpData.ProjectFiles.Add(newFile);
                    }
                    catch (Exception Ex)
                    {
                        MessageBox.Show($"Line BU 263: {Ex.Message}");
                    }
                }
                foreach (ProjectFile file in vcsManager.ProjectData.DiffLog)
                {
                    //if (file.fileChangedState == FileChangedState.Restored) continue;
                    ProjectFile newFile = new ProjectFile(file);
                    string retrievablePath = backupManager.GetFileBackupPath(parentDirectory?.ToString(), vcsManager.ProjectData.projectName, file.deployedProjectVersion);
                    newFile.fileSrcPath = retrievablePath;
                    backUpData.DiffLog.Add(newFile);
                }
                backUpData.projectPath = backupSrcPath;
                byte[] serializedFile = MemoryPackSerializer.Serialize(backUpData);
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
