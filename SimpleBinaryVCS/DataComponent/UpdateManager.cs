using SimpleBinaryVCS.Interfaces;
using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.Utils;
using System.Collections.ObjectModel;
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
        private Dictionary<string, ProjectFile> BackupFiles { get; set; }
        private ProjectData? SrcProjectData { get; set; }
        private List<ChangedFile>? currentProjectFileChanges;
        public List<ChangedFile>? CurrentProjectFileChanges
        {
            get => currentProjectFileChanges;
            private set 
            { 
                currentProjectFileChanges = value; 
            }
        }

        public Action<object>? ProjectUpdateEventHandler;

        private FileHandlerTool fileHandlerTool;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public UpdateManager() 
        { 
            fileHandlerTool = new FileHandlerTool();
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public void Awake()
        {
        }
        
        /// <summary>
        /// Requires Updater Name, Log, and Current Project Path
        /// </summary>
        /// <param name="updaterName"></param>
        /// <param name="updateLog"></param>
        public void UpdateProjectMain(string updaterName, string updateLog, string currentProjectPath)
        {
            if (ProjectMain == null || CurrentProjectFileChanges == null)
            {
                MessageBox.Show("Project Data on Update Manager is Missing"); return;
            }

            RegisterFileChanges(ProjectMain, CurrentProjectFileChanges, out StringBuilder? changeLog);
            fileHandlerTool.ApplyFileChanges(CurrentProjectFileChanges);
            string newVersionName = GetProjectVersionName(ProjectMain);
            string conductedId = HashTool.GetUniqueComputerID(Environment.MachineName);
            ProjectData newProjectData = new ProjectData
                (
                ProjectMain,
                currentProjectPath,
                updaterName,
                DateTime.Now,
                newVersionName,
                conductedId,
                updateLog,
                changeLog?.ToString(),
                ProjectMain.ProjectFiles,
                CurrentProjectFileChanges
                );

            ProjectUpdateEventHandler?.Invoke(newProjectData);
        }

        private void RegisterFileChanges(ProjectData currentProject, List<ChangedFile> fileChanges, out StringBuilder? changeLog)
        {
            if (fileChanges.Count <= 0)
            {
                changeLog = null;
                return;
            }
            StringBuilder newChangeLog = new StringBuilder();

                foreach (ChangedFile changes in fileChanges)
            {
                if (changes.DstFile == null) continue; 
                if (!currentProject.ProjectFiles.TryGetValue(changes.DstFile.DataRelPath, out ProjectFile? existingFile))
                {
                    currentProject.ProjectFiles.Add(changes.DstFile.DataRelPath, new ProjectFile(changes.DstFile));
                }
                else 
                    currentProject.ProjectFiles[changes.DstFile.DataRelPath] = new ProjectFile(changes.DstFile);
                LogTool.RegisterChange(newChangeLog, changes.DataState, changes.SrcFile, changes.DstFile);
                currentProject.NumberOfChanges++;
            }
            changeLog = newChangeLog; 
        }
        
        private string GetProjectVersionName(ProjectData projData, bool isNewProject = false)
        {
            if (!isNewProject)
            {
                return $"{HashTool.GetUniqueComputerID(Environment.MachineName)}_{DateTime.Now.ToString("yyyy_MM_dd")}_{projData.RevisionNumber + 1}";
            }
            return $"{HashTool.GetUniqueComputerID(Environment.MachineName)}_{DateTime.Now.ToString("yyyy_MM_dd")}_v{projData.RevisionNumber + 1}";
        }
        #region MetaData CallBack 
        public void DataStagedCallBack(object fileChangeListObj)
        {
            if (fileChangeListObj is not List<ChangedFile> fileChangesList) return;
            currentProjectFileChanges = fileChangesList;
        }

        public void MetaDataLoadedCallBack(object projMetaDataObj)
        {
            if (projMetaDataObj is not ProjectMetaData projectMetaData) return;
            BackupFiles = projectMetaData.BackupFiles;
        }
        public void ProjectLoadedCallback(object obj)
        {
            if (obj is not ProjectData loadedProject)
            {
                MessageBox.Show("Invalid Parameter has entered on Update Manager");
                return;
            }
            ProjectMain = loadedProject;
            CurrentProjectFileChanges = null; 
            SrcProjectData = null;
        }
        public void SrcProjectLoadedCallBack(object srcProjDataObj)
        {
            if (srcProjDataObj is not ProjectData srcProjectData)
            {
                return;
            }
            this.SrcProjectData = srcProjectData;
        }
        #endregion
    }
}
