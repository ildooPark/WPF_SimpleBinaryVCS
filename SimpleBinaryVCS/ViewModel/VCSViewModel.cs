using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Model;
using System;
using WinForms = System.Windows.Forms; 
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.TeamFoundation.MVVM;
using Microsoft.TeamFoundation.Build.Client;
using System.IO;
using System.Diagnostics;
using MemoryPack;
using System.Runtime.CompilerServices;

namespace SimpleBinaryVCS.ViewModel
{
    public class VCSViewModel : ViewModelBase
    {
        public ProjectData projectData
        {
            get { return App.VcsManager.ProjectData; }
            set
            {
                App.VcsManager.ProjectData = value;
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
        public VCSViewModel()
        {
            projectData = App.VcsManager.ProjectData; 
            projectFiles = App.VcsManager.ProjectData.ProjectFiles;
            vcsManager = App.VcsManager;
            fileManager = App.FileManager; 
            vcsManager.fetchAction += FetchResponse;
            vcsManager.revertAction += RevertResponse;
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
            if (projectData.projectPath == null || updaterName == null || updateLog == null) return false;
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
            if (projectData.revisionNumber >= 1) vcsManager.updateAction?.Invoke(obj);
            projectData.numberOfChanges = 0;
            projectData.updatedVersion = GetprojectDataVersionName();
            projectData.DiffLog.Clear();

            foreach (ProjectFile changedFile in fileManager.ChangedFileList)
            {
                if (projectFiles.Contains(changedFile))
                {
                    int srcIndex = projectFiles.IndexOf(changedFile);
                    if (srcIndex != -1)
                    {
                        // This should be converted to Switch statements 
                        string? fileHash = vcsManager.GetFileMD5CheckSum(changedFile.projectPath, changedFile.fileRelPath);
                        if (fileHash == null) return; 
                        changedFile.fileHash = fileHash;
                        if (fileHash == ProjectFiles[srcIndex].fileHash)
                        {
                            MessageBox.Show($"UploadedFile {changedFile.fileName} " + $"is identical to existing File {ProjectFiles[srcIndex].fileName} \n" + $"UploadedFile FileHash : \n" + $"ProjectFile {ProjectFiles[srcIndex].fileHash}"); 
                            return;

                        }
                        changedFile.deployedProjectVersion = projectData.updatedVersion;
                        projectFiles[srcIndex].isNew = false; 
                        projectData.DiffLog.Add(changedFile);
                        projectData.DiffLog.Add(projectFiles[srcIndex]);
                        if (!File.Exists(changedFile.fileRelPath))
                        {
                            MessageBox.Show($"File Missing {changedFile.fileRelPath}"); 
                            return;
                        }
                        try
                        {
                            File.Copy(changedFile.fileRelPath, projectFiles[srcIndex].fileRelPath, true);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Critical Error {ex.Message}");
                            MessageBox.Show($"Couldn't Copy File From: {changedFile.fileFullPath()} To: {projectFiles[srcIndex].fileFullPath()}");
                        }
                        changedFile.fileRelPath = projectFiles[srcIndex].fileRelPath;
                        projectFiles[srcIndex] = changedFile;
                        projectData.numberOfChanges++; 
                    }
                }
                else // Adding New Files, Should update UploadedFilePath to the RelativeProjectPath
                {
                    string? newFileHash = vcsManager.GetFileMD5CheckSum(changedFile.projectPath, changedFile.fileRelPath);
                    if (newFileHash != null)
                    {
                        changedFile.fileHash = newFileHash;
                        changedFile.deployedProjectVersion = projectData.updatedVersion;
                        try
                        {
                            File.Copy(changedFile.fileFullPath(), Path.Combine(projectData.projectPath, changedFile.fileRelPath), true);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show($" Error :{e.Message} \nCouldn't Copy File in a given path, From: {changedFile.fileFullPath()}, To: {Path.Combine(projectData.projectPath, changedFile.fileRelPath)}"); 
                        }
                        // ONLY Change the Project Path of the changedFile now. 

                        changedFile.projectPath = projectData.projectPath;  
                            //Path.Combine(projectData.projectPath ?? "", Path.GetFileName(changedFile.filePath));
                        vcsManager.ProjectData.ProjectFiles.Add(changedFile);
                        vcsManager.ProjectData.DiffLog.Add(changedFile);
                        projectData.numberOfChanges++;
                    }
                }
            }
            if (projectData.numberOfChanges <= 0)
            {
                MessageBox.Show("No new changes were made.");
                App.FileTrackManager.ChangedFileList.Clear();
                return; 
            }
            //Copy Paste Uploaded File to the Project File Directory 
            projectData.updatedTime = DateTime.Now;
            projectData.updaterName = updaterName;
            projectData.updateLog = updateLog;
            byte[] serializedFile = MemoryPackSerializer.Serialize(projectData);
            File.WriteAllBytes($"{vcsManager.ProjectData.projectPath}\\VersionLog.bin", serializedFile);
            App.FileTrackManager.ChangedFileList.Clear();
            UpdaterName = null;
            UpdateLog = null;
            vcsManager.updateAction?.Invoke(obj);
            return;
        }

        private string? GetprojectDataVersionName()
        {
            if (vcsManager.NewestProjectData == null || vcsManager.NewestProjectData.updatedVersion == null || vcsManager.NewestProjectData.revisionNumber == 0)
            {
                projectData.revisionNumber = 1;
                projectData.updatedVersion = $"{DateTime.Now.ToString("yyyy_MM_dd")}_v{projectData.revisionNumber}"; 
                return projectData.updatedVersion;
            }
            projectData.revisionNumber = vcsManager.NewestProjectData.revisionNumber + 1;
            projectData.updatedVersion = $"{DateTime.Now.ToString("yyyy_MM_dd")}_v{projectData.revisionNumber}";
            return projectData.updatedVersion;
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
                App.VcsManager.mainProjectPath = openFD.SelectedPath;
                projectData.projectPath = openFD.SelectedPath;
                projectData.projectName = Path.GetFileName(openFD.SelectedPath);
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
                        projectData = currentData;
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
                ProjectFiles = projectData.ProjectFiles;
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
            App.VcsManager.projectLoadAction?.Invoke(openFD.SelectedPath);
        }

        private void InitializeProject(string projectFilePath)
        {
            StringBuilder changeLog = new StringBuilder();
            string[]? newProjectFiles; TryGetAllFiles(projectFilePath, out newProjectFiles);
            if (newProjectFiles == null) return;
            projectData.updatedVersion = GetprojectDataVersionName(); 

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
                newFile.deployedProjectVersion = projectData.updatedVersion;
                ProjectFiles.Add(newFile);
                vcsManager.ProjectData.DiffLog.Add(newFile);
                changeLog.AppendLine($"Added {newFile.fileName}");
            }
            projectData.updatedTime = DateTime.Now;
            projectData.changeLog = changeLog.ToString();
            projectData.numberOfChanges = ProjectFiles.Count;
            projectData.projectName = Path.GetFileName(projectFilePath);
            byte[] serializedFile = MemoryPackSerializer.Serialize(projectData);
            File.WriteAllBytes($"{projectData.projectPath}\\VersionLog.bin", serializedFile);
            ProjectName = projectData.projectName;
            CurrentVersion = projectData.updatedVersion;
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
                Console.WriteLine(ex.Message);
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
            ProjectName = projectData.projectName;
            CurrentVersion = projectData.updatedVersion;
        }

        private void RevertResponse(object obj)
        {
            ProjectFiles = App.VcsManager.ProjectData.ProjectFiles; 
        }
    }
}