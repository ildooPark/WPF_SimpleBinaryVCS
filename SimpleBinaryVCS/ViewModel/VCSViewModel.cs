using MemoryPack;
using Microsoft.TeamFoundation.MVVM;
using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Model;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Input;
using WinForms = System.Windows.Forms;

namespace SimpleBinaryVCS.ViewModel
{
    public class VCSViewModel : ViewModelBase
    {
        private ProjectData projectData; 
        public ProjectData ProjectData
        {
            get { return projectData; }
            set
            {
                projectData = value;
                ProjectFiles = value.ProjectFiles;
                ProjectName = value.projectName ?? "Undefined";
                CurrentVersion = value.updatedVersion ?? "Undefined"; 
            }
        }

        private string? updaterName;
        public string? UpdaterName
        {
            get => updaterName ?? "";
            set
            {
                updaterName = value;
                OnPropertyChanged("UpdaterName");
            }
        }
        private string? updateLog;
        public string? UpdateLog
        {
            get => updateLog ?? "";
            set
            {
                updateLog = value;
                OnPropertyChanged("UpdateLog");
            }
        }
        private string? projectName;
        public string ProjectName
        {
            get => projectName ?? "";
            set
            {
                projectName = value ?? "Undefined";
                OnPropertyChanged("ProjectName");
            }
        }
        private string? currentVersion;
        public string CurrentVersion
        {
            get => currentVersion ?? "Undefined";
            set
            {
                currentVersion = value ?? "Undefined";
                OnPropertyChanged("CurrentVersion");
            }
        }
        private ObservableCollection<ProjectFile> projectFiles;
        public ObservableCollection<ProjectFile> ProjectFiles
        {
            get => projectFiles;
            set
            {
                projectFiles = value;
                OnPropertyChanged("ProjectFiles");
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

        private ICommand? conductUpdate;
        public ICommand ConductUpdate
        {
            get
            {
                if (conductUpdate == null) conductUpdate = new RelayCommand(UpdateProject, CanUpdateProject);
                return conductUpdate; 
            }
        }

        private ICommand? getProject;
        public ICommand GetProject
        {
            get
            {
                if (getProject == null) getProject = new RelayCommand(RetrieveProject, CanRetrieveProject);
                return getProject;
            }
        }
        private int detectedFileChange;
        private VersionControlManager vcsManager;
        private FileManager fileManager; 
        private BackupManager backupManager;
        public VCSViewModel()
        {
            vcsManager = App.VcsManager;
            projectData = App.VcsManager.ProjectData; 
            projectFiles = App.VcsManager.ProjectData.ProjectFiles;
            fileManager = App.FileManager;
            backupManager = App.BackupManager;
            vcsManager.fetchAction += FetchResponse;
            vcsManager.projectLoadAction += ProjectLoadResponse;
            backupManager.RevertAction += RevertResponse;
            fileManager.newLocalFileChange += OnNewLocalFileChange;
            detectedFileChange = 0; 
        }

        private void OnNewLocalFileChange(int numFile)
        {
            detectedFileChange = numFile;
        }
        #region Update Version 
        private bool CanUpdateProject(object obj)
        {
            if (projectFiles == null || fileManager.ChangedFileList.Count == 0) return false;
            if (ProjectData.projectPath == null || updaterName == null || updateLog == null) return false;
            if (detectedFileChange != 0) return false;
            return true;
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
                    fileManager.RegisterNewFiles(updateDirPath);
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
        private void UpdateProject(object obj)
        {
            if (updaterName == null || updateLog == null || updaterName == "" || updateLog == "")
            {
                var response = MessageBox.Show("Must Have both Deploy Version AND UpdaterName", "ok", MessageBoxButtons.OK);
                if (response == DialogResult.OK) return; 
            }
            if (fileManager.ChangedFileList.Count == 0 || fileManager == null) return;
            //if (projectData.revisionNumber >= 1) vcsManager.updateAction?.Invoke(obj);
            StringBuilder changeLog = new StringBuilder();
            ProjectData.numberOfChanges = 0;
            ProjectData.updatedVersion = GetprojectDataVersionName();
            ProjectData.DiffLog.Clear();

            for (int i = 0; i < fileManager.ChangedFileList.Count; i++)
            {
                
                int srcIndex = projectFiles.IndexOf(fileManager.ChangedFileList[i]);
                //Integrity Version Check
                if ((fileManager.ChangedFileList[i].fileChangedState & FileChangedState.IntegrityChecked) != 0)
                {
                    RegisterFileChange(fileManager.ChangedFileList[i], srcIndex, changeLog);
                    continue;
                }
                ReallocateFile(fileManager.ChangedFileList[i]);
                RegisterFileChange(fileManager.ChangedFileList[i], srcIndex, changeLog);
            }
            //foreach (ProjectFile changedFile in fileManager.ChangedFileList)
            if (ProjectData.numberOfChanges <= 0)
            {
                MessageBox.Show("No new changes were made.");
                fileManager.ChangedFileList.Clear();
                return; 
            }
            //Copy Paste Uploaded File to the Project File Directory 
            ProjectData.updatedTime = DateTime.Now;
            ProjectData.updaterName = updaterName;
            ProjectData.changeLog = changeLog.ToString(); 
            ProjectData.updateLog = updateLog;
            byte[] serializedFile = MemoryPackSerializer.Serialize(ProjectData);
            File.WriteAllBytes($"{vcsManager.ProjectData.projectPath}\\VersionLog.bin", serializedFile);
            ProjectData = ProjectData; 
            fileManager.ChangedFileList.Clear();
            UpdaterName = null;
            UpdateLog = null;
            vcsManager.updateAction?.Invoke(obj);
            return;
        }

        private void RegisterFileChange(ProjectFile file, int fileIndex, StringBuilder changeLog)
        {
            if (fileIndex == -1)
            {
                file.deployedProjectVersion = ProjectData.updatedVersion;
                file.fileSrcPath = ProjectData.projectPath;
                vcsManager.ProjectData.ProjectFiles.Add(file);
                vcsManager.ProjectData.DiffLog.Add(file);
                ProjectData.numberOfChanges++;
                changeLog.AppendLine($"File {file.fileName} on {file.fileRelPath} has been {file.fileChangedState.ToString()}");
            }
            else
            {
                file.deployedProjectVersion = ProjectData.updatedVersion;
                projectFiles[fileIndex].isNew = false;
                ProjectData.DiffLog.Add(file);
                ProjectData.DiffLog.Add(projectFiles[fileIndex]);
                file.fileSrcPath = projectFiles[fileIndex].fileSrcPath;
                changeLog.AppendLine($"File {file.fileName} on {file.fileRelPath} has been {file.fileChangedState.ToString()}");
                changeLog.AppendLine($"From : Build Version: {projectFiles[fileIndex].fileBuildVersion} Hash : {projectFiles[fileIndex].fileHash}");
                changeLog.AppendLine($"To : Build Version: {file.fileBuildVersion} Hash : {file.fileHash}");
                projectFiles[fileIndex] = file;
                ProjectData.numberOfChanges++;

            }
            //1. First set the new as false. 
        }

        private void ReallocateFile(ProjectFile file)
        {
            if (file.fileChangedState == FileChangedState.Deleted)
            {
                try
                {
                    if (File.Exists(file.fileFullPath()))
                    {
                        // Delete the file if it exists
                        File.Delete(file.fileFullPath());
                    }
                    else if (Directory.Exists(file.fileFullPath()))
                    {
                        // Delete the directory if it exists
                        Directory.Delete(file.fileFullPath(), true); // true for recursive deletion
                    }
                }
                catch (Exception Ex)
                {
                    MessageBox.Show($"Line VCS 267: {Ex.Message}");
                }
                return; 
            }
            try
            {
                string newFilePath = $"{ProjectData.projectPath}\\{file.fileRelPath}";

                if (!File.Exists(Path.GetDirectoryName(newFilePath)))
                {
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(newFilePath));
                    }
                    catch (Exception Ex)
                    {
                        MessageBox.Show($"{Ex.Message} \nNo Directory Needed for this new File {file.fileName}");
                    }
                }
                File.Copy(file.fileFullPath(), newFilePath, true);
            }
            catch (Exception Ex)
            {
                MessageBox.Show($"Line VCS 290: {Ex.Message}");
            }
        }
        private string? GetprojectDataVersionName()
        {
            if (vcsManager.NewestProjectData == null || vcsManager.NewestProjectData.updatedVersion == null || vcsManager.NewestProjectData.revisionNumber == 0)
            {
                ProjectData.revisionNumber = 1;
                ProjectData.updatedVersion = $"{DateTime.Now.ToString("yyyy_MM_dd")}_v{ProjectData.revisionNumber}"; 
                return ProjectData.updatedVersion;
            }
            ProjectData.revisionNumber = vcsManager.NewestProjectData.revisionNumber + 1;
            ProjectData.updatedVersion = $"{DateTime.Now.ToString("yyyy_MM_dd")}_v{ProjectData.revisionNumber}";
            return ProjectData.updatedVersion;
        }
        #endregion

        #region Retrieving VersionLogs 
        private bool CanRetrieveProject(object parameter)
        {
            return true;
        }

        private void RetrieveProject(object parameter)
        {
            if (projectFiles != null && projectFiles.Count != 0) projectFiles.Clear();
            var openFD = new WinForms.FolderBrowserDialog();
            string projectDataBin;
            if (openFD.ShowDialog() == DialogResult.OK)
            {
                vcsManager.mainProjectPath = openFD.SelectedPath;
                ProjectData.projectPath = openFD.SelectedPath;
                ProjectData.projectName = Path.GetFileName(openFD.SelectedPath);
            }
            else return; 
            openFD.Dispose(); 
            //Get .bin VersionLog File 
            string[] binFiles = Directory.GetFiles(openFD.SelectedPath, "VersionLog.*", SearchOption.AllDirectories);

            if (binFiles.Length > 0)
            {
                projectDataBin = binFiles[0];
                ProjectData? currentData;
                try
                {
                    var stream = File.ReadAllBytes(projectDataBin);
                    currentData = MemoryPackSerializer.Deserialize<ProjectData>(stream); 
                    if (currentData != null)
                    {
                        ProjectData = currentData;
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
                App.VcsManager.fetchAction?.Invoke(parameter); 
            }
            else
            {
                var result = MessageBox.Show("VersionLog file not found!\n Initialize A New Project?",
                    "Import Project", MessageBoxButtons.YesNo); 
                if (result == DialogResult.Yes)
                {
                    InitializeProject(openFD.SelectedPath);
                }
                else
                {
                    MessageBox.Show("Please Select Another Project Path"); 
                    return; 
                }
            }
            //App.VcsManager.projectLoadAction?.Invoke(openFD.SelectedPath);
        }

        private void InitializeProject(string projectFilePath)
        {
            StringBuilder changeLog = new StringBuilder();
            string[]? newProjectFiles; TryGetAllFiles(projectFilePath, out newProjectFiles);
            if (newProjectFiles == null) return;
            ProjectData.updatedVersion = GetprojectDataVersionName(); 

            foreach (string filePath in newProjectFiles)
            {
                var fileInfo = FileVersionInfo.GetVersionInfo(filePath);
                ProjectFile newFile = new ProjectFile
                    (
                    true,
                    new FileInfo(filePath).Length,
                    Path.GetFileName(filePath),
                    projectFilePath,
                    Path.GetRelativePath(projectFilePath, filePath),
                    fileInfo.FileVersion, 
                    FileChangedState.Added
                    );

                newFile.fileHash = vcsManager.GetFileMD5CheckSum(projectFilePath, filePath);
                newFile.deployedProjectVersion = ProjectData.updatedVersion;
                ProjectFiles.Add(newFile);
                vcsManager.ProjectData.DiffLog.Add(newFile);
                changeLog.AppendLine($"Added {newFile.fileName}");
            }
            ProjectData.updatedTime = DateTime.Now;
            ProjectData.changeLog = changeLog.ToString();
            ProjectData.numberOfChanges = ProjectFiles.Count;
            ProjectData.projectName = Path.GetFileName(projectFilePath);
            byte[] serializedFile = MemoryPackSerializer.Serialize(ProjectData);
            File.WriteAllBytes($"{ProjectData.projectPath}\\VersionLog.bin", serializedFile);
            ProjectName = ProjectData.projectName;
            CurrentVersion = ProjectData.updatedVersion;
            vcsManager.updateAction?.Invoke(projectFilePath); 
        }
        #endregion
        private void TryGetAllFiles(string directoryPath, out string[]? Files)
        {
            try
            {
                Files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Files = null; 
            }
        }

        private void VersionIntegrityCheck(object obj)
        {
            // After Revert Changes, 
            // Any Detected Changes should be enlisted to the FileManager.DetectedFileChanges for the Push
        }

        private void FetchResponse(object parameter)
        {
            ProjectName = ProjectData.projectName ?? "Undefined";
            CurrentVersion = ProjectData.updatedVersion ?? "Undefined";
        }

        private void RevertResponse()
        {
            ProjectFiles = App.VcsManager.ProjectData.ProjectFiles;
            ProjectData = ProjectData; 
        }

        private void ProjectLoadResponse(ProjectData projectData)
        {
            this.ProjectData = projectData;
        }
    }
}

#region Deprecated Updates 
//private void UpdateProject(object obj)
//{
//    if (updaterName == null || updateLog == null || updaterName == "" || updateLog == "")
//    {
//        var response = MessageBox.Show("Must Have both Deploy Version AND UpdaterName", "ok", MessageBoxButtons.OK);
//        if (response == DialogResult.OK) return;
//    }
//    if (fileManager.ChangedFileList.Count == 0 || fileManager == null) return;
//    if (projectData.revisionNumber >= 1) vcsManager.updateAction?.Invoke(obj);
//    projectData.numberOfChanges = 0;
//    projectData.updatedVersion = GetprojectDataVersionName();
//    projectData.DiffLog.Clear();

//    foreach (ProjectFile changedFile in fileManager.ChangedFileList)
//    {
//        if (projectFiles.Contains(changedFile))
//        {
//            int srcIndex = projectFiles.IndexOf(changedFile);
//            if (srcIndex != -1)
//            {
//                // This should be converted to Switch statements 
//                string? fileHash = vcsManager.GetFileMD5CheckSum(changedFile.projectPath, changedFile.fileRelPath);
//                if (fileHash == null) return;
//                changedFile.fileHash = fileHash;
//                if (fileHash == ProjectFiles[srcIndex].fileHash)
//                {
//                    MessageBox.Show($"UploadedFile {changedFile.fileName} " + $"is identical to existing File {ProjectFiles[srcIndex].fileName} \n" + $"UploadedFile FileHash : \n" + $"ProjectFile {ProjectFiles[srcIndex].fileHash}");
//                    return;

//                }
//                changedFile.deployedProjectVersion = projectData.updatedVersion;
//                projectFiles[srcIndex].isNew = false;
//                projectData.DiffLog.Add(changedFile);
//                projectData.DiffLog.Add(projectFiles[srcIndex]);
//                if (!File.Exists(changedFile.fileRelPath))
//                {
//                    MessageBox.Show($"File Missing {changedFile.fileRelPath}");
//                    return;
//                }
//                try
//                {
//                    File.Copy(changedFile.fileRelPath, projectFiles[srcIndex].fileRelPath, true);
//                }
//                catch (Exception ex)
//                {
//                    MessageBox.Show($"Critical Error {ex.Message}");
//                    MessageBox.Show($"Couldn't Copy File From: {changedFile.fileFullPath()} To: {projectFiles[srcIndex].fileFullPath()}");
//                }
//                changedFile.fileRelPath = projectFiles[srcIndex].fileRelPath;
//                projectFiles[srcIndex] = changedFile;
//                projectData.numberOfChanges++;
//            }
//        }
//        else // Adding New Files, Should update UploadedFilePath to the RelativeProjectPath
//        {
//            string? newFileHash = vcsManager.GetFileMD5CheckSum(changedFile.projectPath, changedFile.fileRelPath);
//            if (newFileHash != null)
//            {
//                changedFile.fileHash = newFileHash;
//                changedFile.deployedProjectVersion = projectData.updatedVersion;
//                try
//                {
//                    File.Copy(changedFile.fileFullPath(), Path.Combine(projectData.projectPath, changedFile.fileRelPath), true);
//                }
//                catch (Exception e)
//                {
//                    MessageBox.Show($" Error :{e.Message} \nCouldn't Copy File in a given path, From: {changedFile.fileFullPath()}, To: {Path.Combine(projectData.projectPath, changedFile.fileRelPath)}");
//                }
//                // ONLY Change the Project Path of the changedFile now. 

//                changedFile.projectPath = projectData.projectPath;
//                //Path.Combine(projectData.projectPath ?? "", Path.GetFileName(changedFile.filePath));
//                vcsManager.ProjectData.ProjectFiles.Add(changedFile);
//                vcsManager.ProjectData.DiffLog.Add(changedFile);
//                projectData.numberOfChanges++;
//            }
//        }
//    }
//    if (projectData.numberOfChanges <= 0)
//    {
//        MessageBox.Show("No new changes were made.");
//        App.FileTrackManager.ChangedFileList.Clear();
//        return;
//    }
//    //Copy Paste Uploaded File to the Project File Directory 
//    projectData.updatedTime = DateTime.Now;
//    projectData.updaterName = updaterName;
//    projectData.updateLog = updateLog;
//    byte[] serializedFile = MemoryPackSerializer.Serialize(projectData);
//    File.WriteAllBytes($"{vcsManager.ProjectData.projectPath}\\VersionLog.bin", serializedFile);
//    App.FileTrackManager.ChangedFileList.Clear();
//    UpdaterName = null;
//    UpdateLog = null;
//    vcsManager.updateAction?.Invoke(obj);
//    return;
//}
#endregion