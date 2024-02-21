using MemoryPack;
using SimpleBinaryVCS.Interfaces;
using SimpleBinaryVCS.Model;
using System.Collections.ObjectModel;
using System.IO;

namespace SimpleBinaryVCS.DataComponent
{
    public class BackupManager : IManager
    {

        private ProjectMetaData? ProjectMetaData
        {
            get
            {
                if (metaDataManager.ProjectMetaData == null)
                {
                    MessageBox.Show("Missing ProjectMetaData");
                    return null;
                }
                return metaDataManager.ProjectMetaData;
            }
        }

        /// <summary>
        /// key : file Hash Value 
        /// Value : IFile, which may include TrackedFiles, or ProjectFiles 
        /// </summary>
        public Dictionary<string, IProjectData>? BackupFiles
        {
            get
            {
                if (metaDataManager.ProjectMetaData == null)
                {
                    MessageBox.Show("Missing ProjectMetaData");
                    return null;
                }
                return metaDataManager.ProjectMetaData.BackupFiles;
            }
        }

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
        private MetaDataManager metaDataManager; 
        private FileManager fileManager;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public BackupManager()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            
        }
        public void Awake()
        {
            metaDataManager = App.MetaDataManager;
            fileManager = App.FileManager;
            metaDataManager.ProjectInitialized += MakeProjectBackup;
        }
        // Save BackUp 

        /// <summary>
        /// By Default should Point to ProjectMain, Make backup of the ucrrent project main before applying new changes. 
        /// </summary>
        /// <param name="projectData">Should Point to ProjectMain</param>
        public void MakeProjectBackup (object? projectObj)
        {
            if (projectObj is not ProjectData projectData) return; 
            
            // Check if Backup already exists 
            bool hasBackup = backupProjectDataList.Contains(projectData);
            // Get changed File List 
            // IF Modified : Try Register new Backup File  
            // IF Added : Skip
            // IF Deleted : Erase from the projectFiles List 
            // IF Restored: 

        }

        private void MakeBackupFile(ProjectData projectData, ProjectFile file)
        {

        }
        public void FetchBackupProjectList(object obj)
        {
            if (metaDataManager.ProjectMetaData == null || metaDataManager.ProjectMetaData.ProjectDataList == null) return;
            FetchAction?.Invoke(ProjectBackupListObservable);
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
        #region Planned 
        public void MakeProjectFullBackup(ProjectData projectData)
        {

        }

        public void FetchBackup()
        {
            
        }

        public void RevertProject(ProjectData revertingProjectData)
        {
            try
            {
                List<ChangedFile>? diff = fileManager.FindVersionDifferences(revertingProjectData, ProjectMetaData?.ProjectMain);
                ProjectData revertedData = new ProjectData(revertingProjectData, true);

                foreach (ProjectFile file in revertingProjectData.ProjectFiles)
                {
                    try
                    {
                        string newFilePath = $"{newSrcPath}\\{file.DataRelPath}";
                        if (!File.Exists(Path.GetDirectoryName(newFilePath) ?? "")) Directory.CreateDirectory(Path.GetDirectoryName(newFilePath) ?? "");
                        File.Copy(file.DataAbsPath, newFilePath, true);
                        ProjectFile newData = new ProjectFile(file);
                        revertedData.ProjectFiles.Add(newData);

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Line BU 256: {ex.Message}");
                    }
                }
                revertedData.ProjectPath = newSrcPath;
                byte[] serializedFile = MemoryPackSerializer.Serialize(revertedData);
                File.WriteAllBytes($"{revertedData.ProjectPath}\\VersionLog.bin", serializedFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"BUVM RevertBackupToMain {ex.Message}");
            }
        }

        private void AmmendFileDifferences(List<ProjectFile> fileDifferences)
        {

        }
        public void Start(object obj)
        {
            
        }

        #endregion
    }
}