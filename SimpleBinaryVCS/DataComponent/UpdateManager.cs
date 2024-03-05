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
            _fileHandlerTool = App.FileHandlerTool;
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
            if (_currentProjectFileChanges == null || _currentProjectFileChanges.Count == 0) { MessageBox.Show("File Changes does not exist"); return; }
            if (currentProjectPath != _projectMetaData.ProjectPath) { MessageBox.Show("Project Meta Data Path and Updated Path must match"); return; }
            
            string newVersionName = GetProjectVersionName(_projectMain, _projectMetaData.LocalUpdateCount);
            string conductedPC = Environment.MachineName;
            ProjectData updatedProjectData = new ProjectData(_projectMain);
            RegisterFileChanges(updatedProjectData, currentProjectPath, _currentProjectFileChanges, newVersionName, out StringBuilder ? changeLog);
            
            bool updateSuccess = false;
            while (!updateSuccess)
            {
                updateSuccess = _fileHandlerTool.TryApplyFileChanges(_currentProjectFileChanges);
                if (!updateSuccess)
                {
                    var response = MessageBox.Show("Update Failed, Would you like to Retry?", "Update Project", 

                        MessageBoxButtons.YesNo);
                    if (response == DialogResult.Yes)
                    {
                        continue; 
                    }
                    else
                    {
                        MessageBox.Show("Update Failed, Please Run Version Integrity Test"); 
                        return;
                    }
                }
            }
            ++_projectMetaData.LocalUpdateCount;
            updatedProjectData.ChangedFiles = _currentProjectFileChanges;
            updatedProjectData.ProjectPath = currentProjectPath;
            updatedProjectData.UpdaterName = updaterName;
            updatedProjectData.UpdatedTime = DateTime.Now;
            updatedProjectData.UpdatedVersion = newVersionName;
            updatedProjectData.ConductedPC = conductedPC;
            updatedProjectData.UpdateLog = updateLog;
            updatedProjectData.ChangeLog = changeLog?.ToString() ?? "";
            updatedProjectData.NumberOfChanges = _currentProjectFileChanges.Count;
            updatedProjectData.RevisionNumber = _projectMetaData.LocalUpdateCount;

            ProjectUpdateEventHandler?.Invoke(updatedProjectData);
        }

        public void MergeProjectMain(string updaterName, string updateLog, string currentProjectPath)
        {

        }
        private void RegisterFileChanges(ProjectData currentProject, string currentProjectPath, List<ChangedFile> fileChanges, string newProjectVersion, out StringBuilder? changeLog)
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
                    currentProject.ProjectFiles.Add(changes.DstFile.DataRelPath, new ProjectFile(changes.DstFile, newProjectVersion, currentProjectPath));
                    if (changes.SrcFile != null)
                        changes.SrcFile.DeployedProjectVersion = currentProject.UpdatedVersion;
                }
                else
                {
                    if ((changes.DataState & DataState.Deleted) != 0)
                    {
                        currentProject.ProjectFiles.Remove(changes.DstFile.DataRelPath);
                    }
                    else
                        currentProject.ProjectFiles[changes.DstFile.DataRelPath] = new ProjectFile(changes.DstFile, newProjectVersion, currentProjectPath);
                }
                    LogTool.RegisterChange(newChangeLog, changes.DataState, changes.SrcFile, changes.DstFile);
                currentProject.NumberOfChanges++;
            }
            changeLog = newChangeLog; 
        }
        private string GetProjectVersionName(ProjectData projData, int currentUpdateCount)
        {
            return $"{projData.ProjectName}_{Environment.MachineName}_{DateTime.Now.ToString("yyyy_MM_dd")}_v{currentUpdateCount + 1}";
        }
        #region MetaData CallBack 
        public void DataStagedCallBack(object fileChangeListObj)
        {
            if (fileChangeListObj is not List<ChangedFile> fileChangesList) return;
            _currentProjectFileChanges = fileChangesList;
        }
        public void MetaDataLoadedCallBack(object projMetaDataObj)
        {
            if (projMetaDataObj is not ProjectMetaData projectMetaData) return;
            _projectMetaData = projectMetaData;
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