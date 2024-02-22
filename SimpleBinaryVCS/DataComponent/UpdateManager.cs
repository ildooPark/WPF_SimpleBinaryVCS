using SimpleBinaryVCS.Interfaces;
using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.Utils;
using System.Text;

namespace SimpleBinaryVCS.DataComponent
{
    public class UpdateManager : IManager
    {
        private ProjectData? projectMain; 
        public ProjectData? ProjectMain
        {
            get
            {
                if (projectMain == null)
                {
                    MessageBox.Show("Project Main Not Set for Update Manager");
                    return null; 
                }
                return projectMain;
            }
            private set
            {
                projectMain = value; 
            }
        }
        private ProjectData? SrcProjectData { get; set; }
        private List<ChangedFile>? projectFileChanges;
        public List<ChangedFile>? ProjectFileChanges
        {
            get
            {
                if (projectFileChanges == null)
                {
                    MessageBox.Show("FileChangesList Not Set for Update Manager");
                    return null;
                }
                return projectFileChanges;
            }
            private set 
            { 
                projectFileChanges = value; 
            }
        }

        public Action<object>? UpdateAction;
        private FileManager fileManager;
        private StringBuilder changeLog;
        private FileHandlerTool fileHandlerTool;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public UpdateManager() 
        { 
            changeLog = new StringBuilder();
            fileHandlerTool = new FileHandlerTool();
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        
        public void Awake()
        {
            fileManager = App.FileManager;
            fileManager.SrcProjectDeployed += 
            fileManager.UpdateChanges += ProjectChangesForUpdate;
        }

        public void Start(object obj)
        {
            if (obj is not ProjectData loadedProject)
            {
                MessageBox.Show("Invalid Parameter has entered on Update Manager");
                return;
            }
            ProjectMain = loadedProject;
            SrcProjectData = null; 
            projectFileChanges?.Clear();
            changeLog.Clear();
        }
        private void RegisterSrcProject(object srcProjDataObj)
        {
            if (srcProjDataObj is not ProjectData srcProjectData) 
            {
                return;
            }
            this.SrcProjectData = srcProjectData;
        }
        private void ProjectChangesForUpdate(object fileChangeListObj)
        {
            if (fileChangeListObj is not List<ChangedFile> fileChangesList) return;
        }

        public void UpdateProjectMain()
        {
            if (ProjectMain == null || ProjectFileChanges == null)
            {
                MessageBox.Show("Project Data on Update Manager is Missing"); return;
            }
            // 0. Generate New Project
            ProjectData newProjectData = new ProjectData(ProjectMain);
            newProjectData.ChangedFiles = ProjectFileChanges;
            // 2. Make Physical changes to the files 
            fileHandlerTool.ApplyFileChanges(ProjectFileChanges);
            // 3. Make Update, and backup for new version. 
            UpdateAction?.Invoke(newProjectData);
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
