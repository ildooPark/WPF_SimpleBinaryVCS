using SimpleBinaryVCS.Interfaces;
using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.Utils;
using DeployAssistant.Model;
using System.Collections.ObjectModel;
using System.IO;

namespace SimpleBinaryVCS.DataComponent
{
    public class BackupManager : IManager
    {
        public ProjectMetaData? ProjectMetaData { get; set; }
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
        public event Action<MetaDataState> ManagerStateEventHandler;

        private FileHandlerTool _fileHandlerTool;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public BackupManager()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            _fileHandlerTool = new FileHandlerTool();
        }
        public void Awake(){}
        private void BackupProject(ProjectData projectData)
        {
            if (BackupProjectDataList == null || ProjectMetaData == null)
            { 
                MessageBox.Show("Failed to Load ProjectMetaData: BackupProjectList is Null"); 
                return; 
            }
            bool hasBackup = BackupProjectDataList.Contains(projectData);
            if (!hasBackup)
            {
                RegisterBackupFiles(projectData);
                ProjectMetaData.ProjectDataList.AddFirst(new ProjectData(projectData));
            }

            string projectMetaDataPath = $"{ProjectMetaData.ProjectPath}\\ProjectMetaData.bin";
            bool serializeSuccess = _fileHandlerTool.TrySerializeProjectMetaData(ProjectMetaData, projectMetaDataPath);
            if (serializeSuccess)
            {
                ProjectMetaData.SetProjectMain(projectData);
            }
            else
            {

            }
            FetchCompleteEventHandler?.Invoke(ProjectBackupListObservable);
        }
        #region Callbacks
        /// <summary>
        /// By Default should Point to ProjectMain, Make backup of the ucrrent project main before applying new changes. 
        /// </summary>
        public void MetaDataManager_ProjLoadedCallback (object? projectObj)
        {
            if (projectObj is not ProjectData newMainProject) return;
            if (ProjectMetaData == null || BackupProjectDataList == null || BackupFiles == null) return;
            BackupProject(newMainProject);
        }
        public void MetaDataManager_MetaDataLoadedCallBack(object metaDataObj)
        {
            if (metaDataObj is not ProjectMetaData projectMetaData) return;
            if (projectMetaData == null) return;
            this.ProjectMetaData = projectMetaData;
        }

        #endregion
        private void RegisterBackupFiles(ProjectData projectData)
        {
            try
            {
                if (BackupFiles == null) return;
                string backupSrcPath = GetFileBackupSrcPath(projectData);
                int backupCount = 0;
                if (!Directory.Exists(backupSrcPath)) Directory.CreateDirectory(backupSrcPath);
                foreach (ChangedFile changes in projectData.ChangedFiles)
                {
                    if ((changes.DataState & DataState.Integrate) != 0)
                    {
                        if ((changes.DataState & (DataState.Modified | DataState.Added)) == 0)
                        {
                            continue;
                        }
                        BackupIntegratedFile(changes.DstFile, projectData.ProjectPath, backupSrcPath);
                        backupCount++;
                        continue; 
                    }
                    if (changes.DstFile == null) continue;
                    if (changes.DstFile.DataType == ProjectDataType.Directory) continue;
                    if (!BackupFiles.TryGetValue(changes.DstFile.DataHash, out ProjectFile? backupFile))
                    {
                        ProjectFile newBackupFile = new ProjectFile(changes.DstFile, DataState.Backup, backupSrcPath);
                        BackupFiles.Add(newBackupFile.DataHash, newBackupFile);
                        _fileHandlerTool.HandleData(changes.DstFile.DataAbsPath, newBackupFile.DataAbsPath, ProjectDataType.File, DataState.Backup);
                        newBackupFile.DataSrcPath = backupSrcPath;
                        if (changes.SrcFile != null)
                            changes.SrcFile.DataSrcPath = backupSrcPath;
                        changes.DstFile.DeployedProjectVersion = projectData.UpdatedVersion;
                        backupCount++;
                    }
                    else
                    {
                        if (changes.SrcFile != null)
                            changes.SrcFile.DataSrcPath = backupFile.DataSrcPath;
                        changes.DstFile.DataSrcPath = projectData.ProjectPath;
                        changes.DstFile.DeployedProjectVersion = projectData.UpdatedVersion;
                    }
                }
                if (backupCount <= 0) Directory.Delete(backupSrcPath, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message); 
            }
        }

        public void BackupIntegratedFile(ProjectFile integratedFile, string projSrcPath, string backupSrcPath)
        {
            if (BackupFiles == null) return;
            try
            {
                if (!BackupFiles.TryGetValue(integratedFile.DataHash, out ProjectFile? backupFile))
                {
                    integratedFile.DataSrcPath = projSrcPath;
                    ProjectFile newBackupFile = new ProjectFile(integratedFile, DataState.Backup, backupSrcPath);
                    BackupFiles.Add(newBackupFile.DataHash, newBackupFile);
                    _fileHandlerTool.HandleData(integratedFile.DataAbsPath, newBackupFile.DataAbsPath, ProjectDataType.File, DataState.Backup);
                }
                else
                {
                    backupFile.DataSrcPath = backupSrcPath; 
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public string GetFileBackupSrcPath(ProjectData projectData)
        {
            string backupPath = $"{projectData.ProjectPath}\\Backup_{Path.GetFileName(projectData.ProjectName)}\\Backup_{projectData.UpdatedVersion}";
            return backupPath; 
        }

        #region Link To View Model
        public bool FetchBackupProjectList()
        {
            if (ProjectMetaData == null || ProjectMetaData.ProjectDataList == null) return false;
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
        public void RevertProject(ProjectData revertingProjectData, List<ChangedFile>? fileDifferences)
        {

            try
            {
                if (fileDifferences == null)
                {
                    ManagerStateEventHandler?.Invoke(MetaDataState.Idle);
                    MessageBox.Show($"BUVM RevertProject File changes is null");
                    return;
                }
                ManagerStateEventHandler?.Invoke(MetaDataState.Reverting);
                bool revertSuccess = false; 
                ProjectData revertedData = new ProjectData(revertingProjectData, true);
                while (!revertSuccess)
                {
                    List<ChangedFile> changedDirs = [];
                    List<ChangedFile> changedFiles = [];
                    
                    foreach (ChangedFile changes in fileDifferences)
                    {
                        if (changes.DstFile == null) continue; 
                        if (changes.DstFile.DataType == ProjectDataType.File)
                        {
                            changedFiles.Add(changes);
                        }
                        else
                        {
                            changedDirs.Add(changes);
                        }
                    }
                    bool revertSuccessDirs = _fileHandlerTool.TryApplyFileChanges(changedDirs);
                    bool revertSuccessFiles = _fileHandlerTool.TryApplyFileChanges(changedFiles);
                    revertSuccess = revertSuccessDirs && revertSuccessFiles; 
                    if (!revertSuccess)
                    {
                        var response = MessageBox.Show("Reverting Project Failed, Would you like to Retry?", "Checkout Project",
                            MessageBoxButtons.YesNo);
                        if (response == DialogResult.Yes)
                        {
                            continue;
                        }
                        else
                        {
                            MessageBox.Show("Revert Failed");
                            ManagerStateEventHandler?.Invoke(MetaDataState.Idle); 
                            return;
                        }
                    }
                }
                ProjectRevertEventHandler?.Invoke(revertingProjectData);
                ManagerStateEventHandler?.Invoke(MetaDataState.Idle);
            }
            catch (Exception ex)
            {
                ManagerStateEventHandler?.Invoke(MetaDataState.Idle);
                MessageBox.Show($"BUVM RevertProject {ex.Message}");
            }
        }
        #endregion
    }
}