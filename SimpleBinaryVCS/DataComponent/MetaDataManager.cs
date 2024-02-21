using MemoryPack;
using SimpleBinaryVCS.Interfaces;
using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.Utils;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SimpleBinaryVCS.DataComponent
{
    public class MetaDataManager : IManager
    {
        public string? CurrentProjectPath {  get; set; }
        public Action<object>? ResetAction;
        public Action<object>? UpdateAction;
        public Action<object>? PullAction;
        public Action<object>? ProjectLoaded;
        public Action<object>? ProjectInitialized;

        public Action? VersionCheckFinished;
        public ProjectMetaData? ProjectMetaData{ get; private set; }

        private ProjectData? mainProjectData; 
        public ProjectData? MainProjectData 
        {
            get => mainProjectData;
            private set
            {
                if (value == null) throw new ArgumentNullException(nameof(mainProjectData));
                else if (ProjectMetaData == null) throw new ArgumentNullException(nameof(ProjectMetaData));
                ProjectMetaData.ProjectMain = value;
                mainProjectData = value; 
                ProjectLoaded?.Invoke(value);
            }
        }

        public ProjectData? NewestProjectData
        {
            get
            {
                if (ProjectMetaData == null) return null;
                if (ProjectMetaData.ProjectDataList.First == null) return null;
                return ProjectMetaData.ProjectDataList.First.Value;
            }
        }

        private FileManager fileManager;
        private BackupManager backupManager;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public MetaDataManager()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
        }

        public void Awake()
        {
            backupManager = App.BackupManager;
            fileManager = App.FileManager;
        }
        private void OnReset(object obj)
        {

        }

        #region Setup Project 
        public bool TryRetrieveProject(string projectPath)
        {
            string projectRepoBin;

            CurrentProjectPath = projectPath;
            string[] binFiles = Directory.GetFiles(CurrentProjectPath, "ProjectMetaData.*", SearchOption.AllDirectories);

            if (binFiles.Length > 0)
            {
                projectRepoBin = binFiles[0];
                ProjectMetaData? loadedProjectMetaData;
                try
                {
                    var stream = File.ReadAllBytes(projectRepoBin);
                    loadedProjectMetaData = MemoryPackSerializer.Deserialize<ProjectMetaData>(stream);
                    if (loadedProjectMetaData != null)
                    {
                        ProjectMetaData = loadedProjectMetaData;
                        MainProjectData = loadedProjectMetaData.ProjectMain;
                        ProjectLoaded?.Invoke(MainProjectData);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                    return false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public void InitializeProject(string projectPath)
        {
            try
            {
                // Project Repository Setup 
                ProjectMetaData newProjectRepo = new ProjectMetaData(projectPath, Path.GetFileName(projectPath));
                StringBuilder changeLog = new StringBuilder();
                TryGetAllFiles(projectPath, out string[]? newProjectFiles);
                if (newProjectFiles == null)
                { MessageBox.Show("Couldn't Get Project Files"); return; }
                ProjectData newProjectData = new ProjectData(projectPath);
                newProjectData.ConductedPC = HashTool.GetUniqueComputerID(Environment.MachineName);
                newProjectData.UpdatedVersion = GetProjectVersionName(newProjectData);

                foreach (string filePath in newProjectFiles)
                {
                    var fileInfo = FileVersionInfo.GetVersionInfo(filePath);
                    ProjectFile newFile = new ProjectFile
                        (
                        new FileInfo(filePath).Length,
                        Path.GetFileName(filePath),
                        projectPath,
                        Path.GetRelativePath(projectPath, filePath),
                        fileInfo.FileVersion,
                        DataChangedState.Added
                        );

                    newFile.DataHash = HashTool.GetFileMD5CheckSum(projectPath, filePath);
                    newFile.DeployedProjectVersion = newProjectData.UpdatedVersion;
                    newProjectData.ProjectFiles.Add(newFile);
                    newProjectData.ChangedFiles.Add(new ChangedFile(new ProjectFile(newFile)));

                    changeLog.AppendLine($"Added {newFile.DataName}");
                }

                newProjectData.UpdatedTime = DateTime.Now;
                newProjectData.ChangeLog = changeLog.ToString();
                newProjectData.NumberOfChanges = newProjectData.ProjectFiles.Count;
                newProjectData.ProjectName = Path.GetFileName(projectPath);
                byte[] serializedFile = MemoryPackSerializer.Serialize(newProjectData);
                File.WriteAllBytes($"{newProjectData.ProjectPath}\\ProjectMetaData.bin", serializedFile);
                ProjectInitialized?.Invoke(MainProjectData);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }
        #endregion
        #region Version Management Tools

        private string GetProjectVersionName(ProjectData projData, bool isNewProject = false)
        {
            if (!isNewProject)
            {
                return $"{HashTool.GetUniqueComputerID(Environment.MachineName)}_{DateTime.Now.ToString("yyyy_MM_dd")}_{++projData.RevisionNumber + 1}";
            }
            return $"{HashTool.GetUniqueComputerID(Environment.MachineName)}_{DateTime.Now.ToString("yyyy_MM_dd")}_v{projData.RevisionNumber + 1}";
        }
        
        
        #endregion
        
        private void TryGetAllFiles(string directoryPath, out string[]? files)
        {
            try
            {
                files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                files = null;
            }
        }


        #region Planned
        #region Exports
        /// <summary>
        /// Input: Requested Project Data 
        /// Output: All the project files, including projectData meta file
        /// in a @.projectParentDir/Exports/ProjectVersion
        /// </summary>
        /// <param name="projectData"></param>
        public void ExportProject(ProjectData projectData)
        {
            // Requests for all the registerd project files, 
            // Copy paste to the 
        }
        public void ExportProjectRepo(ProjectMetaData projectRepository)
        {

        }

        public void Start(object obj)
        {
            throw new NotImplementedException();
        }
        #endregion
        #endregion
    }
}