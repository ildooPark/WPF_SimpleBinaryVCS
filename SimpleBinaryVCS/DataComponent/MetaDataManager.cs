using SimpleBinaryVCS.Interfaces;
using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.Utils;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SimpleBinaryVCS.DataComponent
{
    public class MetaDataManager : IManager
    {
        public string? CurrentProjectPath {  get; set; }

        public event Action<ObservableCollection<ProjectFile>>? FileChangesEventHandler;
        public event Action<object>? StagedChangesEventHandler;
        public event Action<object>? PreStagedChangesEventHandler;
        public event Action<object>? SrcProjectLoadedEventHandler;
        public event Action<object>? ProjectLoadedEventHandler;
        public event Action<object>? MetaDataLoadedEventHandler;
        public event Action<object>? FetchRequestEventHandler;
        public event Action<object, string, ObservableCollection<ProjectFile>>? ProjectIntegrityCheckEventHandler;

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
        private FileHandlerTool fileHandlerTool;

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
            fileHandlerTool = new FileHandlerTool();

            MetaDataLoadedEventHandler += backupManager.MetaDataLoadedCallBack;
            MetaDataLoadedEventHandler += fileManager.MetaDataLoadedCallBack;
            MetaDataLoadedEventHandler += updateManager.MetaDataLoadedCallBack;

            ProjectLoadedEventHandler += backupManager.ProjectLoadedCallback;
            ProjectLoadedEventHandler += fileManager.ProjectLoadedCallback;
            ProjectLoadedEventHandler += updateManager.ProjectLoadedCallback;

            SrcProjectLoadedEventHandler += updateManager.SrcProjectLoadedCallBack;
            StagedChangesEventHandler += updateManager.DataStagedCallBack;

            backupManager.ProjectRevertEventHandler += ProjectChangeCallBack;
            backupManager.FetchCompleteEventHandler += FetchRequestCallBack;

            updateManager.ProjectUpdateEventHandler += ProjectChangeCallBack;

            fileManager.IntegrityCheckEventHandler += ProjectIntegrityCheckCallBack;
            fileManager.DataPreStagedEventHandler += DataPreStagedCallBack;
            fileManager.DataStagedEventHandler += DataStagedCallBack;
            fileManager.SrcProjectDataEventHandler += SrcProjectLoadedCallBack;
        }

        #region View Model Request Calls
        public bool RequestProjectRetrieval(string projectPath)
        {
            string projectMetaDataPath = $"{projectPath}\\ProjectMetaData.bin";

            
            CurrentProjectPath = projectPath;

            try
            {
                fileHandlerTool.TryDeserializeProjectMetaData(projectMetaDataPath, out ProjectMetaData? retrievedData);
                if (retrievedData != null)
                {
                    ProjectMetaData = retrievedData;
                    MainProjectData = retrievedData.ProjectMain;
                }
                else 
                    return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"MetaDataManager TryRetrieveProject Error {ex.Message}");
                return false;
            }
            return true;
        }
        public void RequestProjectInitialization(string projectPath)
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
                newProjectData.UpdatedVersion = GetProjectVersionName(newProjectData, true);

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
        public bool RequestSrcDataRetrieval(string deployedPath)
        {
            bool result = fileManager.RetrieveDataSrc(deployedPath);
            if (!result) return false;
            return true; 
        }
        public bool RequestFetchBackup()
        {
            bool result = backupManager.FetchBackupProjectList();
            if (!result) return false;
            return true;
        }
        public void RequestRevertProject(ProjectData? targetProject)
        {
            if (targetProject == null)
            {
                MessageBox.Show("Invalid Request For Backup: Targeting Project is Null");
                return;
            }
            List<ChangedFile>? fileDifferences = fileManager.FindVersionDifferences(targetProject, MainProjectData, true);
            
            backupManager.RevertProject(targetProject, fileDifferences);
        }
        public void RequestStageChanges()
        {
            fileManager.StageNewFilesAsync();
        }

        public void RequestClearStagedFiles()
        {
            fileManager.ClearDeployedFileChanges();
        }

        public void RequestProjectIntegrityTest(object requester)
        {
            fileManager.MainProjectIntegrityCheck(requester);
        }

        public void RequestFileRestore(ProjectFile targetFile, DataState state)
        {
            fileManager.RegisterNewfile(targetFile, state);
        }

        public void RequestUpdate(string? updaterName, string? updateLog, string? currentProjectPath)
        {
            if (currentProjectPath == null)
            {
                MessageBox.Show("Project Path must be set for Update Request");
                return;
            }
            updateManager.UpdateProjectMain(updaterName, updateLog, currentProjectPath);
        }
        #endregion
        #region Version Management Tools
        private string GetProjectVersionName(ProjectData projData, bool isNewProject = false)
        {
            if (!isNewProject)
            {
                return $"{HashTool.GetUniqueComputerID(Environment.MachineName)}_{DateTime.Now.ToString("yyyy_MM_dd")}_v{projData.RevisionNumber + 1}";
            }
            return $"{HashTool.GetUniqueComputerID(Environment.MachineName)}_{DateTime.Now.ToString("yyyy_MM_dd")}_v{projData.RevisionNumber}";
        }

        #endregion
        #region Callbacks
        private void ProjectIntegrityCheckCallBack(object sender, string changeLog, List<ProjectFile> changedFileList)
        {
            ProjectIntegrityCheckEventHandler?.Invoke(sender, changeLog, new ObservableCollection<ProjectFile>(changedFileList));
        }

        private void DataPreStagedCallBack(object preStagedFileListObj)
        {
            if (preStagedFileListObj is not List<ProjectFile> preStagedFileList) return;
            ObservableCollection<ProjectFile> preStagedChangesObs = new ObservableCollection<ProjectFile>(preStagedFileList);
            FileChangesEventHandler?.Invoke(preStagedChangesObs);
        }

        private void DataStagedCallBack(object stagedFileListObj)
        {
            if (stagedFileListObj is not List<ChangedFile> stagedFiles)
            {
                MessageBox.Show("Improper stagedFile parameter value returned");
                return;
            }
            ObservableCollection<ProjectFile> stagedChangesObs = new ObservableCollection<ProjectFile>();
            foreach (ChangedFile file in stagedFiles)
            {
                if (file.DstFile != null) stagedChangesObs.Add(file.DstFile);
            }
            FileChangesEventHandler?.Invoke(stagedChangesObs);
            StagedChangesEventHandler?.Invoke(stagedFiles);
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

        private void FetchRequestCallBack(object backupListObj)
        {
            if (backupListObj is not ObservableCollection<ProjectData> backupList) return;
            FetchRequestEventHandler?.Invoke(backupListObj);
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
        public void RequestProjectExport(ProjectData projectData)
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