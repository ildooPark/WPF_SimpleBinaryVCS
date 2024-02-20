using MemoryPack;
using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.Utils;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using WinForms = System.Windows.Forms;

namespace SimpleBinaryVCS.DataComponent
{
    public class VersionControlManager
    {
        public string? CurrentProjectPath {  get; set; }
        public Action<object>? UpdateAction;
        public Action<object>? PullAction;
        public Action<object>? FetchAction;
        public Action<object>? ProjectLoaded;
        public Action<object>? ProjectInitialized;

        public Action? VersionCheckFinished;
        private ProjectRepository projectRepository; 
        public ProjectRepository ProjectRepository
        {
            get => projectRepository; 
            private set
            {
                projectRepository = value;
                NewestProjectData = value.ProjectDataList.First();
                CurrentProjectData = value.ProjectMain; 
            }
        }

        private ProjectData? currentProjectData; 
        public ProjectData CurrentProjectData 
        { 
            get => currentProjectData ??= new ProjectData();
            set
            {
                if (projectRepository == null) throw new ArgumentNullException(nameof(projectRepository));
                projectRepository.ProjectMain = value;
                currentProjectData = projectRepository.ProjectMain;
                ProjectLoaded?.Invoke(currentProjectData);
            }
        }

        public ProjectData? NewestProjectData { get; private set; }
        public ObservableCollection<ProjectData> ProjectDataList { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public VersionControlManager()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            ProjectDataList = new ObservableCollection<ProjectData>();
        }

        #region Setup Project 
        public bool TryRetrieveProject(string projectPath)
        {
            var openFD = new WinForms.FolderBrowserDialog();
            string projectDataBin;
            if (openFD.ShowDialog() == DialogResult.OK)
            {
                CurrentProjectPath = openFD.SelectedPath;
            }
            else return false;
            openFD.Dispose();
            //Get .bin VersionLog File 
            string[] binFiles = Directory.GetFiles(ProjectRepository.ProjectPath, "VersionLog.*", SearchOption.AllDirectories);

            if (binFiles.Length > 0)
            {
                projectDataBin = binFiles[0];
                ProjectRepository? projectRepo;
                try
                {
                    var stream = File.ReadAllBytes(projectDataBin);
                    projectRepo = MemoryPackSerializer.Deserialize<ProjectRepository>(stream);
                    if (projectRepo != null)
                    {
                        CurrentProjectData = projectRepo.ProjectMain;
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
                FetchAction?.Invoke(projectPath);
            }
            else
            {
                var result = MessageBox.Show("VersionLog file not found!\n Initialize A New Project?",
                    "Import Project", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    InitializeProject(openFD.SelectedPath);
                    ProjectInitialized?.Invoke(CurrentProjectData);
                }
                else
                {
                    MessageBox.Show("Please Select Another Project Path");
                    return false;
                }
            }
            return true; 
        }
        private void InitializeProject(string projectFilePath)
        {
            // Project Repository Setup 
            ProjectRepository newProjectRepo = new ProjectRepository(projectFilePath, Path.GetFileName(projectFilePath)); 

            StringBuilder changeLog = new StringBuilder();
            TryGetAllFiles(projectFilePath, out string[]? newProjectFiles);
            if (newProjectFiles == null)
            { MessageBox.Show("Couldn't Get Project Files");  return; }
            ProjectData newProjectData = new ProjectData();
            newProjectData.UpdatedVersion = GetprojectDataVersionName();

            foreach (string filePath in newProjectFiles)
            {
                var fileInfo = FileVersionInfo.GetVersionInfo(filePath);
                ProjectFile newFile = new ProjectFile
                    (
                    true,
                    new FileInfo(filePath).Length,
                    Path.GetFileName(filePath),
                    projectFilePath,
                    Path.GetRelativePath(projectFilePath, filePath),
                    fileInfo.FileVersion,
                    DataChangedState.Added
                    );

                newFile.DataHash = HashTool.GetFileMD5CheckSum(projectFilePath, filePath);
                newFile.DeployedProjectVersion = newProjectData.UpdatedVersion;
                newProjectData.ProjectFiles.Add(newFile);
                newProjectData.ChangedFiles.Add(newFile);
                changeLog.AppendLine($"Added {newFile.DataName}");
            }

            newProjectData.UpdatedTime = DateTime.Now;
            newProjectData.ChangeLog = changeLog.ToString();
            newProjectData.NumberOfChanges = newProjectData.ProjectFiles.Count;
            newProjectData.ProjectName = Path.GetFileName(projectFilePath);
            byte[] serializedFile = MemoryPackSerializer.Serialize(CurrentProjectData);
            File.WriteAllBytes($"{CurrentProjectData.ProjectPath}\\VersionLog.bin", serializedFile);
            UpdateAction?.Invoke(newProjectData);
        }
        #endregion
        #region Version Management Tools

        private string GetprojectDataVersionName()
        {
            if (NewestProjectData == null || 
                NewestProjectData.UpdatedVersion == null || 
                ProjectRepository.UpdateCount == 0)
            {
                return $"{Environment.MachineName}_{DateTime.Now.ToString("yyyy_MM_dd")}_{++ProjectRepository.UpdateCount}";
            }
            return $"{Environment.MachineName}_{DateTime.Now.ToString("yyyy_MM_dd")}_v{++ProjectRepository.UpdateCount}";
        }
        /// <summary>
        /// Merging Src => Dst, Occurs in version Reversion or Merging from outer source. 
        /// </summary>
        /// <param name="srcData"></param>
        /// <param name="dstData"></param>
        /// <param name="isRevert"> True if Reverting, else (Merge) false.</param>
        public List<ProjectFile> FindVersionDifferences(ProjectData srcData, ProjectData dstData, bool isRevert = true)
        {
            try
            {
                StringBuilder fileIntegrityLog = new StringBuilder();
                fileIntegrityLog.AppendLine($"Conducting Version Integrity Check on {CurrentProjectData.UpdatedVersion}");
                List<string> recordedFiles = new List<string>();
                List<string> directoryFiles = new List<string>();
                Dictionary<string, ProjectFile> srcDict = srcData.ProjectFilesDict;
                Dictionary<string, ProjectFile> dstDict = dstData.ProjectFilesDict;
                
                // Files which is not on the Dst 
                IEnumerable<string> filesToAdd = srcData.ProjectRelFilePathsList.Except(dstData.ProjectRelFilePathsList);
                // Files which is not on the Src
                IEnumerable<string> filesToDelete = dstData.ProjectRelFilePathsList.Except(srcData.ProjectRelFilePathsList);
                // Directories which is not on the Src
                IEnumerable<string> dirsToAdd = srcData.ProjectRelDirsList.Except(dstData.ProjectRelDirsList);
                // Directories which is not on the Dst
                IEnumerable<string> dirsToDelete = dstData.ProjectRelDirsList.Except(srcData.ProjectRelDirsList);
                // Files to Overwrite
                IEnumerable<string> intersectFiles = srcData.ProjectRelFilePathsList.Intersect(dstData.ProjectRelFilePathsList);

                //1. Directories 
                foreach (string dirRelPath in dirsToAdd)
                {
                    if (dirRelPath == "VersionLog.bin") continue;
                    fileIntegrityLog.AppendLine($"{dirRelPath} has been Added");
                    string? fileHash = HashTool.GetFileMD5CheckSum(CurrentProjectData.ProjectPath, dirRelPath);
                    ProjectFile file = new ProjectFile(CurrentProjectData.ProjectPath, dirRelPath, fileHash, DataChangedState.Added | DataChangedState.IntegrityChecked);
                    dstData.ChangedFiles.Add(file);
                }

                foreach (string dirRelPath in dirsToDelete)
                {
                    fileIntegrityLog.AppendLine($"{dirRelPath} has been Deleted");
                    ProjectFile file = dstDict[dirRelPath];
                    file.DataState = DataChangedState.Deleted | DataChangedState.IntegrityChecked;
                    dstData.ChangedFiles.Add(file);
                }
                //2. Files 
                foreach (string fileRelPath in filesToAdd)
                {
                    if (fileRelPath == "VersionLog.bin") continue;
                    fileIntegrityLog.AppendLine($"{fileRelPath} has been Added");
                    string? fileHash = HashTool.GetFileMD5CheckSum(CurrentProjectData.ProjectPath, fileRelPath);
                    ProjectFile file = new ProjectFile(CurrentProjectData.ProjectPath, fileRelPath, fileHash, DataChangedState.Added | DataChangedState.IntegrityChecked);
                    dstData.ChangedFiles.Add(file);
                }

                foreach (string fileRelPath in filesToDelete)
                {
                    fileIntegrityLog.AppendLine($"{fileRelPath} has been Deleted");
                    ProjectFile file = projectFilesDict[fileRelPath];
                    file.DataState = DataChangedState.Deleted | DataChangedState.IntegrityChecked;
                    dstData.ChangedFiles.Add(file);
                }
                //3. File Overwrite
                Dictionary<string, ProjectFile> srcDict = srcData.ProjectFilesDict;
                Dictionary<string, ProjectFile> dstDict = dstData.ProjectFilesDict;
                foreach (string fileRelPath in intersectFiles)
                {
                    if (srcDict[fileRelPath].DataHash != dstDict[fileRelPath].DataHash)
                    {
                        fileIntegrityLog.AppendLine($"File {dstDict[fileRelPath].DataName} on {fileRelPath} has been modified");
                        var fileVersionInfo = FileVersionInfo.GetVersionInfo(Path.Combine(CurrentProjectData.ProjectPath, fileRelPath));

                        ProjectFile file = new ProjectFile(dstDict[fileRelPath]);
                        file.DataState = DataChangedState.Modified | DataChangedState.IntegrityChecked;
                        file.DataHash = srcDict[fileRelPath].DataHash;
                        file.IsNew = true;
                        file.UpdatedTime = new FileInfo(file.DataAbsPath).LastAccessTime;
                        dstData.ChangedFiles.Add(file);
                    }
                }
                fileIntegrityLog.AppendLine("Integrity Check Complete");
                VersionCheckFinished?.Invoke();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"{ex.Message}. Couldn't Run File Integrity Check");
            }
        }
        public void FindVersionDifferences(ProjectData srcData, ProjectData dstData, ObservableCollection<ProjectFile> changeList)
        {
            if (srcData == null  || dstData == null)
            {
                MessageBox.Show($"One or more project is set to null");
                return;
            }
            try
            {
                StringBuilder fileIntegrityLog = new StringBuilder();
                fileIntegrityLog.AppendLine($"Conducting Version Integrity Check on {CurrentProjectData.UpdatedVersion}");
                Dictionary<string, ProjectFile> srcDict = srcData.ProjectFilesDict;
                Dictionary<string, ProjectFile> dstDict = dstData.ProjectFilesDict;

                List<string> recordedFiles = new List<string>();
                List<string> directoryFiles = new List<string>();
                // Fitting to the SrcFiles, 
                // Files which is not on the Dst 
                IEnumerable<string> filesToAdd = srcData.ProjectFileRelPathsList().Except(dstData.ProjectFileRelPathsList());
                // Files which is not on the Src
                IEnumerable<string> filesToDelete = dstData.ProjectFileRelPathsList().Except(srcData.ProjectFileRelPathsList());
                // Directories which is not on the Src
                IEnumerable<string> dirsToAdd = srcData.ProjectRelDirsList().Except(dstData.ProjectRelDirsList());
                // Directories which is not on the Dst
                IEnumerable<string> dirsToDelete = dstData.ProjectRelDirsList().Except(srcData.ProjectRelDirsList());
                // Files to Overwrite
                IEnumerable<string> intersectFiles = srcData.ProjectFileRelPathsList().Intersect(dstData.ProjectFileRelPathsList());

                //1. Directories 
                foreach (string dirRelPath in dirsToAdd)
                {
                    if (dirRelPath == "VersionLog.bin") continue;
                    fileIntegrityLog.AppendLine($"{dirRelPath} has been Added");
                    string? fileHash = HashTool.GetFileMD5CheckSum(CurrentProjectData.ProjectPath, dirRelPath);
                    ProjectFile file = new ProjectFile(CurrentProjectData.ProjectPath, dirRelPath, fileHash, DataChangedState.Added | DataChangedState.IntegrityChecked);
                    dstData.ChangedFiles.Add(file);
                }

                foreach (string dirRelPath in dirsToDelete)
                {
                    fileIntegrityLog.AppendLine($"{dirRelPath} has been Deleted");
                    ProjectFile file = new ProjectFile(dstDict[dirRelPath]);
                    file.DataState = DataChangedState.Deleted | DataChangedState.IntegrityChecked;
                    changeList.Add(file);
                }

                foreach (string fileRelPath in filesToAdd)
                {
                    if (fileRelPath == "VersionLog.bin") continue;
                    fileIntegrityLog.AppendLine($"{fileRelPath} has been Added");
                    string? fileHash = HashTool.GetFileMD5CheckSum(CurrentProjectData.ProjectPath, fileRelPath);
                    ProjectFile file = new ProjectFile(CurrentProjectData.ProjectPath, fileRelPath, fileHash, DataChangedState.Added | DataChangedState.IntegrityChecked);
                    dstData.ChangedFiles.Add(file);
                }

                foreach (string fileRelPath in filesToDelete)
                {
                    fileIntegrityLog.AppendLine($"{fileRelPath} has been Deleted");
                    ProjectFile file = new ProjectFile(dstDict[fileRelPath]);
                    file.DataState = DataChangedState.Deleted | DataChangedState.IntegrityChecked;
                    dstData.ChangedFiles.Add(file);
                }
                
                foreach (string fileRelPath in intersectFiles)
                {
                    if (srcDict[fileRelPath].DataHash != dstDict[fileRelPath].DataHash)
                    {
                        fileIntegrityLog.AppendLine($"File {dstDict[fileRelPath].DataName} on {fileRelPath} has been modified");
                        var fileVersionInfo = FileVersionInfo.GetVersionInfo(Path.Combine(CurrentProjectData.ProjectPath, fileRelPath));

                        ProjectFile file = new ProjectFile(dstDict[fileRelPath]);
                        file.DataState = DataChangedState.Modified | DataChangedState.IntegrityChecked;
                        file.DataHash = srcDict[fileRelPath].DataHash;
                        file.IsNew = true;
                        file.UpdatedTime = new FileInfo(file.DataAbsPath).LastAccessTime;
                        dstData.ChangedFiles.Add(file);
                    }
                }

                fileIntegrityLog.AppendLine("Integrity Check Complete");
                VersionCheckFinished?.Invoke();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"{ex.Message}. Couldn't Run File Integrity Check");
            }
        }
        #endregion
        /// <summary>
        /// Preceded by the backup of the current Project
        /// </summary>
        /// <param name="obj"></param>
        private void UponUpdateRequest(object obj)
        {
            // 0. Generate New Project
            ProjectData newProjectData = new ProjectData(ProjectRepository.ProjectMain);

            // 1. Make Physical changes to the files 
            // 2. Make 
            // 3. Call for new Fetch Action 
        }
        private void TryGetAllFiles(string directoryPath, out string[]? files)
        {
            try
            {
                files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                files = null;
            }
        }

        #region Planned
        #region Exports
        /// <summary>
        /// Input: Requested Project Data 
        /// Output: All the project files, including projectData meta file
        /// in a @.projectParentDir/Exports/ProjectVersion
        /// </summary>
        /// <param name="projectData"></param>
        public void ExportProject(ProjectData projectData)
        {
            // Requests for all the registerd project files, 
            // Copy paste to the 
        }
        public void ExportProjectRepo(ProjectRepository projectRepository)
        {

        }
        #endregion
        #endregion
    }
}