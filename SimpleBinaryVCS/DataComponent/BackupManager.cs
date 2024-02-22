using MemoryPack;
using SimpleBinaryVCS.Interfaces;
using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.Utils;
using System.Collections.ObjectModel;
using System.IO;

namespace SimpleBinaryVCS.DataComponent
{
    public class BackupManager : IManager
    {
        private ProjectMetaData? projectMetaData;
        public ProjectMetaData? ProjectMetaData
        {
            get
            {
                if (projectMetaData == null)
                {
                    MessageBox.Show("Missing ProjectMetaData on BackupManager");
                    return null;
                }
                return projectMetaData;
            }
            private set
            {
                projectMetaData = value;
            }
        }

        /// <summary>
        /// key : file Hash Value 
        /// Value : IFile, which may include TrackedFiles, or ProjectFiles 
        /// </summary>
        public Dictionary<string, IProjectData>? BackupFiles => ProjectMetaData?.BackupFiles;

        private LinkedList<ProjectData>? backupProjectDataList => ProjectMetaData?.ProjectDataList;
        public ObservableCollection<ProjectData>? ProjectBackupListObservable
        {
            get
            {
                if (backupProjectDataList == null) return null; 
                ObservableCollection<ProjectData> dataList = new ObservableCollection<ProjectData>();
                foreach (ProjectData pd in backupProjectDataList)
                {
                    dataList.Add(pd);
                }
                return dataList;
            }
        }

        public Action<object>? BackupAction;
        public Action<object>? RevertAction;
        public Action<object>? FetchAction; 
        private FileManager fileManager;
        private FileHandlerTool fileHandlerTool;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public BackupManager()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            fileHandlerTool = new FileHandlerTool();
        }
        public void Awake()
        {
            fileManager = App.FileManager;
        }
        public void Start(object obj)
        {
        }

        // Save BackUp 

        /// <summary>
        /// By Default should Point to ProjectMain, Make backup of the ucrrent project main before applying new changes. 
        /// </summary>
        /// <param name="projectData">Should Point to ProjectMain</param>
        public void BackupProject (object? projectObj)
        {
            if (projectObj is not ProjectData newMainProject) return;
            if (projectMetaData == null || backupProjectDataList == null || BackupFiles == null) return;
            // Check if Backup already exists 
            bool hasBackup = backupProjectDataList.Contains(newMainProject);
            if (!hasBackup)
            {
                RegisterBackupFiles(newMainProject);
            }

            // Get changed File List 
            // IF Modified : Try Register new Backup File  
            // IF Added : Skip
            // IF Deleted : Erase from the projectFiles List 
            // IF Restored: 
            byte[] serializedFile = MemoryPackSerializer.Serialize(ProjectMetaData);
            //        File.WriteAllBytes($"{backUpData.ProjectPath}\\VersionLog.bin", serializedFile);
            newMainProject.ChangedFiles.Clear();
            FetchAction?.Invoke(ProjectBackupListObservable);
        }
        public void SetProjectMetaData(object metaDataObj)
        {
            if (metaDataObj is not ProjectMetaData projectMetaData) return;
            if (projectMetaData == null) return;
            this.projectMetaData = projectMetaData;
        }

        private void RegisterBackupFiles(ProjectData projectData)
        {
            if (BackupFiles == null) return;

            foreach (ProjectFile file in projectData.ChangedDstFileList)
            {
                if (!BackupFiles.TryAdd(file.DataHash, file))
                {

                }
            }
        }


        public string GetFileBackupPath(string parentPath, string projectName,  string projectVersion)
        {
            string backupPath = $"{parentPath}\\Backup_{Path.GetFileName(projectName)}\\Backup_{projectVersion}";
            return backupPath; 
        }

        public void BackupProjectData(ProjectData backupFile)
        {
            //Try Adding 
            //Else, Update Backup Path Info 
        }
        #region Link To View Model 
        public void FetchBackupProjectList(object obj)
        {
            if (projectMetaData == null || projectMetaData.ProjectDataList == null) return;
            FetchAction?.Invoke(ProjectBackupListObservable);
        }

        public void RevertProject(ProjectData revertingProjectData)
        {
            try
            {
                ProjectData revertedData = new ProjectData(revertingProjectData, true);
                List<ChangedFile>? FileDifferences = fileManager.FindVersionDifferences(revertingProjectData, ProjectMetaData?.ProjectMain);
                fileHandlerTool.ApplyFileChanges(FileDifferences);
                RevertAction?.Invoke(revertingProjectData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"BUVM RevertBackupToMain {ex.Message}");
            }
        }
        #endregion
    }
}