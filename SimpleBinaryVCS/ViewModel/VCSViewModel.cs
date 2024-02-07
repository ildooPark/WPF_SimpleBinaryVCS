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
        public VCSViewModel()
        {
            projectData = App.VcsManager.ProjectData; 
            projectFiles = App.VcsManager.ProjectData.ProjectFiles;
            vcsManager = App.VcsManager; 
            App.VcsManager.fetchAction += FetchResponse;
            App.VcsManager.revertAction += RevertResponse;
            App.FileManager.newLocalFileChange += OnNewLocalFileChange;
            detectedFileChange = 0; 
        }

        private void OnNewLocalFileChange(int numFile)
        {
            detectedFileChange = numFile;
        }

        private bool CanUpdateProject(object obj)
        {
            if (projectFiles == null || App.FileTrackManager.ChangedFileList.Count == 0) return false;
            if (projectData.projectPath == null || updaterName == null || updateLog == null) return false;
            if (detectedFileChange != 0) return false;
            return true;
        }

        private void UpdateProject(object obj)
        {
            if (updaterName == null || updateLog == null || updaterName == "" || updateLog == "")
            {
                var response = MessageBox.Show("Must Have both Deploy Version AND UpdaterName", "ok", MessageBoxButtons.OK);
                if (response == DialogResult.OK) return; 
            }
            if (App.FileTrackManager.ChangedFileList.Count == 0 || App.FileTrackManager == null) return;
            if (projectData.revisionNumber >= 1) App.VcsManager.updateAction?.Invoke(obj);
            projectData.numberOfChanges = 0;
            projectData.updatedVersion = GetprojectDataVersionName();
            projectData.DiffLog.Clear();

            foreach (ProjectFile changedFile in App.FileTrackManager.ChangedFileList)
            {
                if (projectFiles.Contains(changedFile))
                {
                    int srcIndex = projectFiles.IndexOf(changedFile);
                    if (srcIndex != -1)
                    {
                        string? fileHash = App.VcsManager.GetMD5CheckSum(changedFile.filePath);
                        if (fileHash == null) return; 
                        changedFile.fileHash = fileHash;
                        if (fileHash == ProjectFiles[srcIndex].fileHash) return; 
                        changedFile.deployedProjectVersion = projectData.updatedVersion;
                        projectFiles[srcIndex].isNew = false; 
                        projectData.DiffLog.Add(changedFile);
                        projectData.DiffLog.Add(projectFiles[srcIndex]);
                        if (!File.Exists(changedFile.filePath))
                        {
                            MessageBox.Show($"File Missing {changedFile.filePath}"); 
                            return;
                        }
                        try
                        {
                            File.Copy(changedFile.filePath, projectFiles[srcIndex].filePath, true);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Critical Error {ex.Message}");
                        }
                        changedFile.filePath = projectFiles[srcIndex].filePath;
                        projectFiles[srcIndex] = changedFile;
                        projectData.numberOfChanges++; 
                    }
                }
                else // Adding New Files, Should update UploadedFilePath to the RelativeProjectPath
                {
                    string? newFileHash = App.VcsManager.GetMD5CheckSum(changedFile.filePath);
                    if (newFileHash != null)
                    {
                        changedFile.fileHash = newFileHash;
                        changedFile.deployedProjectVersion = projectData.updatedVersion;
                        try
                        {
                            File.Copy(changedFile.filePath, Path.Combine(projectData.projectPath ?? "", Path.GetFileName(changedFile.filePath)), true);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show($" Error :{e.Message} \nCouldn't Copy File in a given path, From: {projectData.projectPath}, To: {changedFile.filePath}"); 
                        }
                        changedFile.filePath = Path.Combine(projectData.projectPath ?? "", Path.GetFileName(changedFile.filePath));
                        App.VcsManager.ProjectData.ProjectFiles.Add(changedFile);
                        App.VcsManager.ProjectData.DiffLog.Add(changedFile);
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
            //Transfer all the files in the current Directory to the backup folder. 
            //Serialize to .bin file, and then save the file to the projectFile Directory
            //Update Complete. 
            byte[] serializedFile = MemoryPackSerializer.Serialize(projectData);
            File.WriteAllBytes($"{App.VcsManager.ProjectData.projectPath}\\VersionLog.bin", serializedFile);
            //Fetch Action
            App.FileTrackManager.ChangedFileList.Clear();
            UpdaterName = null;
            UpdateLog = null; 
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
            string[]? newProjectFiles; TryGetAllFiles(projectFilePath, out newProjectFiles);
            if (newProjectFiles == null) return;
            projectData.updatedVersion = GetprojectDataVersionName(); 

            foreach (string filePath in newProjectFiles)
            {
                var fileInfo = FileVersionInfo.GetVersionInfo(filePath);
                ProjectFile newFile = new ProjectFile(
                    true,
                    new FileInfo(filePath).Length,
                    Path.GetFileName(filePath),
                    filePath,
                    fileInfo.FileVersion);
                newFile.fileHash = App.VcsManager.GetMD5CheckSum(filePath);
                newFile.deployedProjectVersion = projectData.updatedVersion;
                ProjectFiles.Add(newFile);
                App.VcsManager.ProjectData.DiffLog.Add(newFile);
            }
            projectData.numberOfChanges = ProjectFiles.Count;
            projectData.projectName = Path.GetFileName(projectFilePath);
            byte[] serializedFile = MemoryPackSerializer.Serialize(projectData);
            File.WriteAllBytes($"{projectData.projectPath}\\VersionLog.bin", serializedFile);
            ProjectName = projectData.projectName;
            CurrentVersion = projectData.updatedVersion; 
        }
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