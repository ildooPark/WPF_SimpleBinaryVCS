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
        public event Action<object>? ResetEventHandler;
        public event Action<object>? UpdateEventHandler;
        public event Action<object>? DataStagedEventHandler;
        public event Action<object>? SrcProjectLoadedEventHandler;
        public event Action<object>? ProjectLoadedEventHandler;
        public event Action<object>? MetaDataLoadedEventHandler;

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
                if (value == null || value is not ProjectData) throw new ArgumentNullException(nameof(mainProjectData));
                else if (ProjectMetaData == null) throw new ArgumentNullException(nameof(ProjectMetaData));
                mainProjectData = new ProjectData(value);
                ProjectMetaData.ProjectMain = mainProjectData;
                ProjectLoadedEventHandler?.Invoke(mainProjectData);
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

            SrcProjectLoadedEventHandler += updateManager.SrcProjectLoadedCallBack;

            DataStagedEventHandler += updateManager.NewDataStagedCallBack;
            //DataStagedEventHandler += backupManager.
            backupManager.ProjectRevertEventHandler += ProjectChangeCallBack;
            updateManager.ProjectUpdateEventHandler += ProjectChangeCallBack;
            fileManager.SrcProjectDataEventHandler += SrcProjectLoadedCallBack;
            fileManager.DataStagedEventHandler += DataStagedCallBack;
        }

        #region Project Load
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
                string[]? newProjectDirs = Directory.GetDirectories(projectPath, "*", SearchOption.AllDirectories);
                if (newProjectFiles == null || newProjectDirs == null)
                { 
                    MessageBox.Show("Couldn't Get Project Files (And Or) Directories on MetaDataManager"); 
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
                        DataState.None,
                        Path.GetFileName(filePath),
                        projectPath,
                        Path.GetRelativePath(projectPath, filePath),
                        "",
                        true
                        );
                    newFile.DataHash = HashTool.GetFileMD5CheckSum(projectPath, Path.GetRelativePath(projectPath, filePath));
                    newProjectData.ProjectFiles.Add(newFile.DataRelPath, newFile);
                    newProjectData.ChangedFiles.Add(new ChangedFile(new ProjectFile(newFile), DataState.Added));
                    changeLog.AppendLine($"Added {newFile.DataName}");
                }
                foreach (string dirPath in newProjectDirs)
                {
                    ProjectFile newFile = new ProjectFile
                        (
                        ProjectDataType.Directory,
                        0,
                        "",
                        newProjectData.UpdatedVersion,
                        DateTime.Now,
                        DataState.None,
                        Path.GetFileName(dirPath),
                        projectPath,
                        Path.GetRelativePath(projectPath, dirPath),
                        "",
                        true
                        );
                    newProjectData.ProjectFiles.Add(newFile.DataRelPath, newFile);
                    newProjectData.ChangedFiles.Add(new ChangedFile(new ProjectFile(newFile), DataState.Added));
                    changeLog.AppendLine($"Added {newFile.DataName}");
                }
                newProjectData.UpdatedTime = DateTime.Now;
                newProjectData.ChangeLog = changeLog.ToString();
                newProjectData.NumberOfChanges = newProjectData.ProjectFilesObs.Count;
                MainProjectData = newProjectData;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"MetaDataManager Error InitializeProject {ex.Message}");
                return;
            }
        }
        private string GetProjectVersionName(ProjectData projData, bool isNewProject = false)
        {
            if (!isNewProject)
            {
                return $"{HashTool.GetUniqueComputerID(Environment.MachineName)}_{DateTime.Now.ToString("yyyy_MM_dd")}_{projData.RevisionNumber + 1}";
            }
            return $"{HashTool.GetUniqueComputerID(Environment.MachineName)}_{DateTime.Now.ToString("yyyy_MM_dd")}_v{projData.RevisionNumber + 1}";
        }
        #endregion
        #region Version Management Tools
        #endregion
        #region Callbacks
        private void DataStagedCallBack(object stagedFileListObj)
        {
            if (stagedFileListObj is not List<ChangedFile> stagedFiles)
            {
                MessageBox.Show("Improper stagedFile parameter value called");
                return;
            }
            DataStagedEventHandler?.Invoke(stagedFileListObj);
        }

        private void ProjectChangeCallBack(object projObj)
        {
            if (projObj is not ProjectData projData) return;
            this.MainProjectData = projData;
        }

        private void SrcProjectLoadedCallBack(object srcProjectObj)
        {
            if (srcProjectObj is not ProjectData projData) return;
            SrcProjectLoadedEventHandler?.Invoke(projData);
        }
        #endregion
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