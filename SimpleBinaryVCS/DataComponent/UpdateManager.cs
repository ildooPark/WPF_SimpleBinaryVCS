using SimpleBinaryVCS.Interfaces;
using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.Utils;
using System.Text;

namespace SimpleBinaryVCS.DataComponent
{
    public class UpdateManager : IManager
    {
        private ProjectMetaData? _projectMetaData;
        private ProjectData? _projectMain;
        private Dictionary<string, ProjectFile> _backupFiles;
        private ProjectData? _srcProjectData;
        private List<ChangedFile>? _currentProjectFileChanges;
        private FileHandlerTool _fileHandlerTool;

        public event Action<object>? ProjectUpdateEventHandler;
        public event Action<string>? IssueEventHandler;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public UpdateManager() 
        { 
            _fileHandlerTool = new FileHandlerTool();
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
            if (_projectMetaData == null) { MessageBox.Show("Project MetaData on Update Manager is Missing"); return; }
            if (_projectMain == null) { MessageBox.Show("Project Data on Update Manager is Missing"); return; }
            else if (_currentProjectFileChanges == null) { MessageBox.Show("File Changes does not exist"); return; }

            RegisterFileChanges(_projectMain, _currentProjectFileChanges, out StringBuilder? changeLog);
            _fileHandlerTool.ApplyFileChanges(_currentProjectFileChanges);

            string newVersionName = GetProjectVersionName(_projectMain, _projectMetaData.LocalUpdateCount);
            string conductedId = Environment.MachineName;
            _projectMain.ChangedFiles = _currentProjectFileChanges;
            ProjectData newProjectData = new ProjectData
                (
                _projectMain,
                currentProjectPath,
                updaterName,
                DateTime.Now,   
                newVersionName,
                conductedId,
                updateLog,
                changeLog?.ToString()
                );

            _projectMetaData.LocalUpdateCount++; 
            ProjectUpdateEventHandler?.Invoke(newProjectData);
        }

        public void MergeProjectMain(string updaterName, string updateLog, string currentProjectPath)
        {

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
                    currentProject.ProjectFiles.Add(changes.DstFile.DataRelPath, new ProjectFile(changes.DstFile, currentProject.UpdatedVersion));
                }
                else 
                    currentProject.ProjectFiles[changes.DstFile.DataRelPath] = new ProjectFile(changes.DstFile, currentProject.UpdatedVersion);
                LogTool.RegisterChange(newChangeLog, changes.DataState, changes.SrcFile, changes.DstFile);
                currentProject.NumberOfChanges++;
            }
            changeLog = newChangeLog; 
        }
        private string GetProjectVersionName(ProjectData projData, int currentUpdateCount)
        {
            return $"{Environment.MachineName}_{projData.ProjectName}_{DateTime.Now.ToString("yyyy_MM_dd")}_v{currentUpdateCount + 1}";
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
            _backupFiles = projectMetaData.BackupFiles;
        }
        public void ProjectLoadedCallback(object obj)
        {
            if (obj is not ProjectData loadedProject)
            {
                MessageBox.Show("Invalid Parameter has entered on Update Manager");
                return;
            }
            _projectMain = loadedProject;
            _currentProjectFileChanges = null; 
            _srcProjectData = null;
        }
        public void SrcProjectLoadedCallBack(object srcProjDataObj)
        {
            if (srcProjDataObj is not ProjectData srcProjectData)
            {
                return;
            }
            this._srcProjectData = srcProjectData;
        }

        #endregion
    }
}