using SimpleBinaryVCS.Interfaces;
using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.Utils;
using System.Collections.ObjectModel;
using System.IO;

namespace SimpleBinaryVCS.DataComponent
{
    public class BackupManager : IManager
    {
        private ProjectMetaData? _projectMetaData;
        public ProjectMetaData? ProjectMetaData
        {
            get
            {
                if (_projectMetaData == null)
                {
                    MessageBox.Show("Missing ProjectMetaData on BackupManager");
                    return null;
                }
                return _projectMetaData;
            }
            private set
            {
                _projectMetaData = value;
            }
        }

        public Dictionary<string, ProjectFile>? BackupFiles => ProjectMetaData?.BackupFiles;

        private LinkedList<ProjectData>? BackupProjectDataList => ProjectMetaData?.ProjectDataList;
        public ObservableCollection<ProjectData>? ProjectBackupListObservable
        {
            get
            {
                if (BackupProjectDataList == null) return null; 
                return new ObservableCollection<ProjectData>(BackupProjectDataList);
            }
        }
        //public event Action? ExportBackupEventHandler; 
        public event Action<object>? ProjectRevertEventHandler;
        public event Action<object>? FetchCompleteEventHandler; 
        private FileHandlerTool _fileHandlerTool;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public BackupManager()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            _fileHandlerTool = new FileHandlerTool();
        }
        public void Awake()
        {
        }

        private void BackupProject(ProjectData projectData)
        {
            if (BackupProjectDataList == null || _projectMetaData == null)
            { Console.WriteLine("Failed to Load ProjectMetaData: BackupProjectList is Null"); return; }
            bool hasBackup = BackupProjectDataList.Contains(projectData);
            if (!hasBackup)
            {
                RegisterBackupFiles(projectData);
                _projectMetaData.ProjectDataList.AddFirst(new ProjectData(projectData));
                _projectMetaData.UpdateCount++;
            }
            string projectMetaDataPath = $"{_projectMetaData.ProjectPath}\\ProjectMetaData.bin";
            bool serializeSuccess = _fileHandlerTool.TrySerializeProjectMetaData(_projectMetaData, projectMetaDataPath);
            if (serializeSuccess)
            {
                _projectMetaData.ProjectMain = projectData;
            }
            else
                _projectMetaData.ProjectMain = null; 
                FetchCompleteEventHandler?.Invoke(ProjectBackupListObservable);

        }
        #region Callbacks
        // Save BackUp 

        /// <summary>
        /// By Default should Point to ProjectMain, Make backup of the ucrrent project main before applying new changes. 
        /// </summary>
        /// <param name="projectData">Should Point to ProjectMain</param>
        public void ProjectLoadedCallback (object? projectObj)
        {
            if (projectObj is not ProjectData newMainProject) return;
            if (_projectMetaData == null || BackupProjectDataList == null || BackupFiles == null) return;
            BackupProject(newMainProject);
        }
        public void MetaDataLoadedCallBack(object metaDataObj)
        {
            if (metaDataObj is not ProjectMetaData projectMetaData) return;
            if (projectMetaData == null) return;
            this._projectMetaData = projectMetaData;
        }
        #endregion
        private void RegisterBackupFiles(ProjectData projectData)
        {
            if (BackupFiles == null) return;
            string backupSrcPath = GetFileBackupSrcPath(projectData);
            if (!Directory.Exists(backupSrcPath)) Directory.CreateDirectory(backupSrcPath);
            foreach (ProjectFile file in projectData.ChangedDstFileList)
            {
                if (file.DataType == ProjectDataType.Directory) continue;
                if (!BackupFiles.TryGetValue(file.DataHash, out ProjectFile? backupFile))
                {
                    ProjectFile newBackupFile = new ProjectFile(file, DataState.Backup, backupSrcPath);
                    BackupFiles.Add(newBackupFile.DataHash, newBackupFile);
                    _fileHandlerTool.HandleData(file.DataAbsPath, newBackupFile.DataAbsPath, ProjectDataType.File, DataState.Backup);
                }
            }
        }
        public string GetFileBackupSrcPath(ProjectData projectData)
        {
            string backupPath = $"{Directory.GetParent(projectData.ProjectPath)}\\Backup_{Path.GetFileName(projectData.ProjectName)}\\Backup_{projectData.UpdatedVersion}";
            return backupPath; 
        }
        #region Link To View Model 
        public bool FetchBackupProjectList()
        {
            if (_projectMetaData == null || _projectMetaData.ProjectDataList == null) return false;
            FetchCompleteEventHandler?.Invoke(ProjectBackupListObservable);
            return true;
        }
        public string GetBackupFilePath(ProjectFile projectFile)
        {
            if (BackupFiles == null)
            {
                MessageBox.Show("Backupfiles is null for BackupManager");
                return "";
            }
            BackupFiles.TryGetValue(projectFile.DataHash, out ProjectFile? backupFile);
            return backupFile != null ? backupFile.DataAbsPath : "";
        }
        public void RevertProject(ProjectData revertingProjectData, List<ChangedFile>? FileDifferences)
        {
            try
            {
                ProjectData revertedData = new ProjectData(revertingProjectData, true);
                _fileHandlerTool.ApplyFileChanges(FileDifferences);
                ProjectRevertEventHandler?.Invoke(revertingProjectData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"BUVM RevertProject {ex.Message}");
            }
        }
        #endregion
    }
}