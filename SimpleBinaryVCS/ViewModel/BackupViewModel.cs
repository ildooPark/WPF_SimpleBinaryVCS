﻿using MemoryPack;
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
        private PriorityQueue<ProjectData, ProjectData> importProjects;
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
                DiffLog = value.ChangedFiles;
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

        private VersionControlManager vcsManager;
        private BackupManager backupManager;
        private FileManager fileManager;
        public BackupViewModel()
        {
            importProjects = new PriorityQueue<ProjectData, ProjectData>();
            vcsManager = App.VcsManager;
            vcsManager.UpdateAction += MakeBackUp;
            vcsManager.FetchAction += Fetch;
            fileManager = App.FileManager;
            backupManager = App.BackupManager;
        }

        private bool CanFetch(object obj)
        {
            if (App.Current == null || vcsManager.ProjectRepository == null) return false;
            return true;
        }

        private void Fetch(object obj)
        {
            SelectedItem = null;
            BackupProjectDataList.Clear();
            //Set up Current Project at Main 
            if (vcsManager.CurrentProjectPath == null || vcsManager.ProjectRepository == null) return;
            try
            {
                BackupProjectDataList = vcsManager.ProjectRepository.ObservableProjectList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"BUVM 150 {ex.Message}");
            }
        }

        private bool CanRevert(object obj)
        {
            if (SelectedItem == null || vcsManager.MainProjectData.ProjectPath == null) return false;
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
                fileManager.RegisterNewfile(file, DataChangedState.Restored);
            }
        }

        private void Revert(object obj)
        {
            if (selectedItem == null)
            {
                MessageBox.Show("BUVM 186: Selected BackupVersion is null");
                return;
            }
            var response = MessageBox.Show($"Do you want to Revert to {selectedItem.UpdatedVersion}", "Confirm Updates",
                MessageBoxButtons.YesNo); 
            if (response == DialogResult.Yes)
            {
                //1. Backup Current Project Version 
                //1-1. Check for current project's backup
                MakeBackUp(obj);
                //1-2. Delete all the files in the current Directory 
                //1-2. Compare Main with Revision Version 
                DeleteAllInDirectory(vcsManager.CurrentProjectPath ?? "");
                //2. Transfer the ProjectData to Current
                RevertBackupToMain(selectedItem);
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
            if (string.IsNullOrEmpty(vcsManager.CurrentProjectPath))
            {
                MessageBox.Show("Main Project Path is Empty");
                return;
            }
            string newSrcPath = vcsManager.CurrentProjectPath;
            try
            {
                if (!File.Exists(newSrcPath))
                {
                    ProjectData revertedData = new ProjectData(revertData, true);

                    Directory.CreateDirectory(newSrcPath);

                    foreach (ProjectFile datum in revertData.ProjectFiles)
                    {
                        try
                        {
                            string newFilePath = $"{newSrcPath}\\{datum.DataRelPath}";
                            if (!File.Exists(Path.GetDirectoryName(newFilePath) ?? "")) Directory.CreateDirectory(Path.GetDirectoryName(newFilePath) ?? "");
                            File.Copy(datum.DataAbsPath, newFilePath, true);
                            ProjectFile newData = new ProjectFile(datum);
                            revertedData.ProjectFiles.Add(newData);
                            
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Line BU 256: {ex.Message}");
                        }
                    }
                    revertedData.ProjectPath = newSrcPath;
                    byte[] serializedFile = MemoryPackSerializer.Serialize(revertedData);
                    File.WriteAllBytes($"{revertedData.ProjectPath}\\VersionLog.bin", serializedFile);
                }
                else return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"BUVM RevertBackupToMain {ex.Message}");
            }
            
        }

        /// <summary>
        /// Backup is generated prior to and after update event. 
        /// </summary>
        /// <param name="e"></param>
        private void MakeBackUp(object e)
        {
            //Make new ProjectData for backup 
            if (App.VcsManager.MainProjectData.ProjectPath == null) return;
            DirectoryInfo? parentDirectory = Directory.GetParent(vcsManager.MainProjectData.ProjectPath);
            if (parentDirectory == null) return;
            string backupSrcPath = $"{parentDirectory.ToString()}\\Backup_{Path.GetFileName(vcsManager.MainProjectData.ProjectPath)}\\Backup_{App.VcsManager.MainProjectData.UpdatedVersion}";
            if (!File.Exists(backupSrcPath))
            {
                ProjectData backUpData = new ProjectData(vcsManager.MainProjectData);

                Directory.CreateDirectory(backupSrcPath);

                foreach (ProjectFile data in vcsManager.MainProjectData.ProjectFiles)
                {
                    try
                    {
                        string newBackupFullPath = $"{backupSrcPath}\\{data.DataRelPath}";
                        if (!File.Exists(Path.GetDirectoryName(newBackupFullPath) ?? "")) 
                            Directory.CreateDirectory(Path.GetDirectoryName(newBackupFullPath) ?? "");
                        File.Copy(data.DataAbsPath, newBackupFullPath, true);
                        ProjectFile newFile = new ProjectFile(data);
                        newFile.IsNew = false;
                        backUpData.ProjectFiles.Add(newFile);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Line BU 263: {ex.Message}");
                    }
                }
                foreach (ProjectFile file in vcsManager.MainProjectData.ChangedFiles)
                {
                    ProjectFile newFile = new ProjectFile(file);
                    string retrievablePath = backupManager.GetFileBackupPath(parentDirectory.ToString(), vcsManager.MainProjectData.ProjectName, file.DeployedProjectVersion);
                    backUpData.ChangedFiles.Add(newFile);
                }
                backUpData.ProjectPath = backupSrcPath;
                byte[] serializedFile = MemoryPackSerializer.Serialize(backUpData);
                File.WriteAllBytes($"{backUpData.ProjectPath}\\VersionLog.bin", serializedFile);
            }
            else return;
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
                VersionLogFiles = Directory.GetFiles($"{projectParentPath}\\Backup_{vcsManager.MainProjectData.ProjectName}", "VersionLog.*", SearchOption.AllDirectories);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                VersionLogFiles = null;
                return false;
            }
        }
    }
}
#region Deprecated 
//private void Fetch(object obj)
//{
//    SelectedItem = null;
//    importProjects.Clear();
//    BackupProjectDataList.Clear();
//    //Set up Current Project at Main 
//    if (vcsManager.CurrentProjectPath == null || vcsManager.ProjectRepository == null) return;
//    try
//    {
//        DirectoryInfo? parentPath = Directory.GetParent(vcsManager.CurrentProjectPath);
//        string[] mainVersionLog = Directory.GetFiles(vcsManager.CurrentProjectPath, "VersionLog.*", SearchOption.AllDirectories);
//        ProjectData? mainProjectData = MemoryPackSerializer.Deserialize<ProjectData>(File.ReadAllBytes(mainVersionLog[0]));
//        if (mainProjectData == null) return;
//        vcsManager.MainProjectData = mainProjectData;
//        string[]? backupVersionLogs;
//        if (parentPath == null) return;
//        TryGetBackupLogs(parentPath.ToString(), out backupVersionLogs);
//        if (backupVersionLogs == null) return;
//        foreach (string version in backupVersionLogs)
//        {
//            ProjectData? data = MemoryPackSerializer.Deserialize<ProjectData>(File.ReadAllBytes(version));
//            if (data == null) continue;
//            importProjects.Enqueue(data, data);
//        }
//        BackupProjectDataList.Add(vcsManager.NewestProjectData);
//        int importProjectCount = importProjects.Count;
//        for (int i = 0; i < importProjectCount; i++)
//        {
//            BackupProjectDataList.Add(importProjects.Dequeue());
//        }
//    }
//    catch (Exception ex)
//    {
//        MessageBox.Show($"BUVM 150 {ex.Message}");
//    }
//}
#endregion