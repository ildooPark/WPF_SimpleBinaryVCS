﻿using SimpleBinaryVCS.Interfaces;
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
        public event Action<List<ChangedFile>>? OverlappedFileSortEventHandler; 
        public event Action<object>? StagedChangesEventHandler;
        public event Action<object>? PreStagedChangesEventHandler;
        public event Action<object>? SrcProjectLoadedEventHandler;
        public event Action<object>? ProjectLoadedEventHandler;
        public event Action<object>? MetaDataLoadedEventHandler;
        public event Action<object>? FetchRequestEventHandler;
        public event Action<object, string, ObservableCollection<ProjectFile>>? IntegrityCheckCompleteEventHandler;
        public event Action<string> IssueEventHandler;

        private ProjectMetaData? _projectMetaData;
        public ProjectMetaData? ProjectMetaData
        {
            get => _projectMetaData;
            private set
            {
                if (value == null) throw new ArgumentNullException(nameof(ProjectMetaData));
                _projectMetaData = value;
                CurrentProjectPath = value.ProjectPath;
                MetaDataLoadedEventHandler?.Invoke(value);
            }
        }

        private ProjectData? _mainProjectData; 
        public ProjectData? MainProjectData 
        {
            get => _mainProjectData;
            private set
            {
                if (value == null || value is not ProjectData) throw new ArgumentNullException(nameof(_mainProjectData));
                else if (ProjectMetaData == null) throw new ArgumentNullException(nameof(ProjectMetaData));
                _mainProjectData = new ProjectData(value);
                ProjectMetaData.ProjectMain = _mainProjectData;
                ProjectLoadedEventHandler?.Invoke(_mainProjectData);
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

        private FileManager _fileManager;
        private BackupManager _backupManager;
        private UpdateManager _updateManager;
        private FileHandlerTool _fileHandlerTool;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public MetaDataManager()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
        }

        public void Awake()
        {
            _backupManager = App.BackupManager;
            _fileManager = App.FileManager;
            _updateManager = App.UpdateManager;
            _fileHandlerTool = new FileHandlerTool();

            MetaDataLoadedEventHandler += _backupManager.MetaDataLoadedCallBack;
            MetaDataLoadedEventHandler += _fileManager.MetaDataLoadedCallBack;
            MetaDataLoadedEventHandler += _updateManager.MetaDataLoadedCallBack;

            ProjectLoadedEventHandler += _backupManager.ProjectLoadedCallback;
            ProjectLoadedEventHandler += _fileManager.ProjectLoadedCallback;
            ProjectLoadedEventHandler += _updateManager.ProjectLoadedCallback;

            SrcProjectLoadedEventHandler += _updateManager.SrcProjectLoadedCallBack;
            StagedChangesEventHandler += _updateManager.DataStagedCallBack;

            _backupManager.ProjectRevertEventHandler += ProjectChangeCallBack;
            _backupManager.FetchCompleteEventHandler += FetchRequestCallBack;

            _updateManager.ProjectUpdateEventHandler += ProjectChangeCallBack;

            _fileManager.DataPreStagedEventHandler += DataPreStagedCallBack;
            _fileManager.DataStagedEventHandler += DataStagedCallBack;
            _fileManager.OverlappedFileFoundEventHandler += OverlapFileFoundCallBack; 
            _fileManager.IntegrityCheckEventHandler += ProjectIntegrityCheckCallBack;

            _fileManager.SrcProjectDataLoadedEventHandler += SrcProjectLoadedCallBack;
        }

        #region View Model Request Calls
        public bool RequestProjectRetrieval(string projectPath)
        {
            string projectMetaDataPath = $"{projectPath}\\ProjectMetaData.bin";

            try
            {
                _fileHandlerTool.TryDeserializeProjectMetaData(projectMetaDataPath, out ProjectMetaData? retrievedData);
                if (retrievedData != null)
                {
                    retrievedData.ProjectPath = projectPath;
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
        public void RequestSrcDataRetrieval(string deployedPath)
        {
             _fileManager.RetrieveDataSrc(deployedPath);
        }
        public void RequestStagedFileListRefresh(string deployedPath)
        {

        }
        /// <summary>
        /// Triggers FetchRequestEventHandler if True
        /// </summary>
        /// <returns></returns>
        public bool RequestFetchBackup()
        {
            bool result = _backupManager.FetchBackupProjectList();
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

            List<ChangedFile>? fileDifferences = _fileManager.FindVersionDifferences(targetProject, MainProjectData, true);
            _backupManager.RevertProject(targetProject, fileDifferences);
        }
        public void RequestRevertChange(ProjectFile file)
        {
            _fileManager.RevertChange(file);
        }
        public void RequestStageChanges()
        {
            _fileManager.StageNewFilesAsync();
        }

        public void RequestClearStagedFiles()
        {
            _fileManager.ClearDeployedFileChanges();
        }

        public void RequestOverlappedFileAllocation(List<ChangedFile> overlapSorted)
        {
            _fileManager.RegisterOverlapped(overlapSorted);
        }

        public void RequestProjectIntegrityTest(object requester)
        {
            _fileManager.MainProjectIntegrityCheck(requester);
        }

        public void RequestFileRestore(ProjectFile targetFile, DataState state)
        {
            _fileManager.RegisterNewfile(targetFile, state);
        }

        public void RequestUpdate(string? updaterName, string? updateLog, string? currentProjectPath)
        {
            if (currentProjectPath == null)
            {
                MessageBox.Show("Project Path must be set for Update Request");
                return;
            }
            _updateManager.UpdateProjectMain(updaterName, updateLog, currentProjectPath);
        }
        #endregion
        #region Version Management Tools
        private string GetProjectVersionName(ProjectData projData, bool isNewProject = false)
        {
            if (!isNewProject)
            {
                return $"{projData.ProjectName}_{Environment.MachineName}_{DateTime.Now.ToString("yyyy_MM_dd")}_v{projData.RevisionNumber + 1}";
            }
            return $"{projData.ProjectName}_{Environment.MachineName}_{DateTime.Now.ToString("yyyy_MM_dd")}_v{projData.RevisionNumber}";
        }

        #endregion
        #region Callbacks
        private void ProjectIntegrityCheckCallBack(object sender, string changeLog, List<ProjectFile> changedFileList)
        {
            IntegrityCheckCompleteEventHandler?.Invoke(sender, changeLog, new ObservableCollection<ProjectFile>(changedFileList));
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

        private void OverlapFileFoundCallBack(object overlapFileListObj)
        {
            if (overlapFileListObj is not List<ChangedFile> overlapFileList) return;
            OverlappedFileSortEventHandler?.Invoke(overlapFileList);
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
        public void RequestExportProjectFiles(ProjectData projectData)
        {
            // Requests for all the registerd project files, 
            // Copy paste to the 
        }
        public void ExportProjectRepo(ProjectMetaData projectRepository)
        {

        }
        public void GenerateProjectDataHash(object obj)
        {

        }
        #endregion
        #endregion
    }
}