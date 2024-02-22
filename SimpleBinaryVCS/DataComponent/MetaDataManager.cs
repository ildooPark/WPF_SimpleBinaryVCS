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
        public Action<object>? ResetEventHandler;
        public Action<object>? UpdateEventHandler;
        public Action<object>? ProjectLoadedEventHandler;
        public Action<object>? MetaDataLoadedEventHandler;

        public Action? VersionCheckFinished;
        private ProjectMetaData? projectMetaData;
        public ProjectMetaData? ProjectMetaData
        {
            get => projectMetaData;
            private set
            {
                if (value == null) throw new ArgumentNullException(nameof(ProjectMetaData));
                projectMetaData = value;
                MetaDataLoadedEventHandler?.Invoke(value);
            }
        }

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
                ProjectLoadedEventHandler?.Invoke(value);
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
        private UpdateManager updateManager;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public MetaDataManager()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
        }

        public void Awake()
        {
            backupManager = App.BackupManager;
            fileManager = App.FileManager;
            updateManager = App.UpdateManager;

            MetaDataLoadedEventHandler += backupManager.MetaDataLoadedCallBack;
            MetaDataLoadedEventHandler += fileManager.MetaDataLoadedCallBack;
            MetaDataLoadedEventHandler += updateManager.MetaDataLoadedCallBack;

            ProjectLoadedEventHandler += backupManager.ProjectLoadedCallback;
            ProjectLoadedEventHandler += fileManager.ProjectLoadedCallback;
            ProjectLoadedEventHandler += updateManager.ProjectLoadedCallback;

            backupManager.ProjectRevertEventHandler += ProjectChangeCallBack;
            updateManager.ProjectUpdateEventHandler += ProjectChangeCallBack;
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
                try
                {
                    var stream = File.ReadAllBytes(projectRepoBin);
                    ProjectMetaData? loadedProjectMetaData = MemoryPackSerializer.Deserialize<ProjectMetaData>(stream);
                    if (loadedProjectMetaData != null)
                    {
                        ProjectMetaData = loadedProjectMetaData;
                        MainProjectData = loadedProjectMetaData.ProjectMain;
                        ProjectLoadedEventHandler?.Invoke(MainProjectData);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"MetaDataManager TryRetrieveProject Error {ex.Message}");
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
                StringBuilder changeLog = new StringBuilder();
                ProjectMetaData newProjectRepo = new ProjectMetaData(Path.GetFileName(projectPath), projectPath);
                ProjectMetaData = newProjectRepo; 

                string[]? newProjectFiles = Directory.GetFiles(projectPath, "*", SearchOption.AllDirectories);
                if (newProjectFiles == null)
                { 
                    MessageBox.Show("Couldn't Get Project Files"); 
                    return; 
                }

                ProjectData newProjectData = new ProjectData(projectPath);
                newProjectData.ProjectName = Path.GetFileName(projectPath);
                newProjectData.ConductedPC = HashTool.GetUniqueComputerID(Environment.MachineName);
                newProjectData.UpdatedVersion = GetProjectVersionName(newProjectData);

                foreach (string filePath in newProjectFiles)
                {
                    ProjectFile newFile = new ProjectFile
                        (
                        ProjectDataType.File,
                        new FileInfo(filePath).Length,
                        FileVersionInfo.GetVersionInfo(filePath).FileVersion,
                        newProjectData.UpdatedVersion,
                        DateTime.Now,
                        DataChangedState.PreStaged,
                        Path.GetFileName(filePath),
                        projectPath,
                        Path.GetRelativePath(projectPath, filePath),
                        "",
                        true
                        );
                    newFile.DataHash = HashTool.GetFileMD5CheckSum(projectPath, Path.GetRelativePath(projectPath, filePath));
                    
                    newProjectData.ProjectFiles.Add(newFile);
                    newProjectData.ChangedFiles.Add(new ChangedFile(new ProjectFile(newFile), DataChangedState.Added));

                    changeLog.AppendLine($"Added {newFile.DataName}");
                }

                newProjectData.UpdatedTime = DateTime.Now;
                newProjectData.ChangeLog = changeLog.ToString();
                newProjectData.NumberOfChanges = newProjectData.ProjectFiles.Count;
                MainProjectData = newProjectData;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"MetaDataManager Error InitializeProject {ex.Message}");
                return;
            }
        }
        #endregion
        #region Version Management Tools

        private string GetProjectVersionName(ProjectData projData, bool isNewProject = false)
        {
            if (!isNewProject)
            {
                return $"{HashTool.GetUniqueComputerID(Environment.MachineName)}_{DateTime.Now.ToString("yyyy_MM_dd")}_{projData.RevisionNumber + 1}";
            }
            return $"{HashTool.GetUniqueComputerID(Environment.MachineName)}_{DateTime.Now.ToString("yyyy_MM_dd")}_v{projData.RevisionNumber + 1}";
        }
        
        
        #endregion
        private void ProjectChangeCallBack(object projObj)
        {
            if (projObj is not ProjectData projData) return;
            this.MainProjectData = projData;
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