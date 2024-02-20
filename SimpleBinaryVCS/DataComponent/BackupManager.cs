using SimpleBinaryVCS.Interfaces;
using SimpleBinaryVCS.Model;
using System.IO;

namespace SimpleBinaryVCS.DataComponent
{
    public class BackupManager : IModel
    {
        // Keeps track of all the Project Files, 
        // First tracks the json file in a given Path Directory 
        // if Json file not found, then set the bool to null 

        private Dictionary<string, IProjectData> backupFiles;
        /// <summary>
        /// key : file Hash Value 
        /// Value : IFile, which may include TrackedFiles, or ProjectFiles 
        /// </summary>
        public Dictionary<string, IProjectData> BackupFiles { get => backupFiles; set => backupFiles = value; }
        private LinkedList<ProjectData> backupProjectDataList;
        public Action<object>? BackupAction;
        public Action<object>? RevertAction;
        private MetaDataManager metaDataManager; 
        private FileManager fileManager;
        private ProjectMetaData projectRepository;
        public ProjectMetaData ProjectRepository
        {
            get => projectRepository ?? throw new ArgumentNullException();
            private set
            {
                projectRepository = value;
                mainProjectData = value.ProjectMain;
            }
        }

        private ProjectData? mainProjectData;
        public ProjectData MainProjectData
        {
            get => mainProjectData ??= new ProjectData();
            set
            {
                if (projectRepository == null) throw new ArgumentNullException(nameof(projectRepository));
                projectRepository.ProjectMain = value;
                mainProjectData = projectRepository.ProjectMain;
            }
        }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public BackupManager()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            
        }
        public void Awake()
        {
            metaDataManager = App.MetaDataManager;
            fileManager = App.FileManager;
            metaDataManager.FetchAction += FetchBackupProjectList;
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
        private void FetchBackupProjectList(object obj)
        {
            if (metaDataManager.ProjectMetaData == null || metaDataManager.ProjectMetaData.ProjectDataList == null) return;
            backupProjectDataList = metaDataManager.ProjectMetaData.ProjectDataList;
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

        public void RevertProject(object projObj)
        {
            if (projObj is not ProjectData projectData) return;
            
        }
        
        #endregion
    }
}