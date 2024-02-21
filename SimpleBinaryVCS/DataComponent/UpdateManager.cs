using SimpleBinaryVCS.Interfaces;
using SimpleBinaryVCS.Model;
using System.Text;

namespace SimpleBinaryVCS.DataComponent
{
    public class UpdateManager : IManager
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
        public Action<object>? UpdateAction;
        private MetaDataManager metaDataManager;
        private FileManager fileManager;
        private BackupManager backupManager;
        private StringBuilder changeLog;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public UpdateManager() 
        { 
            changeLog = new StringBuilder();
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        
        public void Awake()
        {
            metaDataManager = App.MetaDataManager;
            backupManager = App.BackupManager;
            fileManager = App.FileManager;
            metaDataManager.ProjectLoaded += Start;
        }

        public void Start(object obj)
        {
            changeLog.Clear();
        }

        public void UpdateProjectMain()
        {
            if (ProjectMetaData == null)
            {
                MessageBox.Show("MetaData is Missing"); return;
            }
            // 0. Generate New Project
            ProjectData newProjectData = new ProjectData(ProjectMetaData.ProjectMain);
            // 1. Check for backup on the Current version, if none found, make one. 
            bool hasBackup = ProjectMetaData.ProjectDataList.Contains(newProjectData);
            if (!hasBackup)
            {
                backupManager.MakeProjectBackup(ProjectMetaData.ProjectMain);
            }
            // 2. Make Physical changes to the files 
            IList<ProjectFile> changedList = fileManager.ChangedFileList.ToList();

            // 3. Make Update, and backup for new version. 

            // 4. Call for new Fetch on BackupProject List
        }

        /// <summary>
        /// Preceded by the backup of the current Project
        /// </summary>
        /// <param name="obj"></param>
        private void UponUpdateRequest(object obj)
        {
            

        }


    }
}
